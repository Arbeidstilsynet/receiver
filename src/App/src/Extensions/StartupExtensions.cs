using System.Net;
using Arbeidstilsynet.Common.Altinn.Model.Exceptions;
using Arbeidstilsynet.Common.AspNetCore.Extensions.CrossCutting;
using Arbeidstilsynet.Common.AspNetCore.Extensions.Extensions;
using Arbeidstilsynet.MeldingerReceiver.App.Jobs;
using Arbeidstilsynet.MeldingerReceiver.App.WebApi;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data.Exceptions;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using OpenTelemetry.Trace;
using Quartz;
using Quartz.Diagnostics;
using Quartz.Impl.AdoJobStore;

namespace Arbeidstilsynet.MeldingerReceiver.App.Extensions;

internal static class StartupExtensions
{
    public static IMvcBuilder ConfigureApi(this IServiceCollection services)
    {
        services.AddOpenApi(openApiOptions =>
            openApiOptions.ConfigureBasicOpenApiSpec(IAssemblyInfo.AppName)
        );

        return services.ConfigureStandardMvc();
    }

    public static IServiceCollection ConfigureApp(
        this IServiceCollection services,
        string appName,
        ApiConfiguration apiConfiguration,
        IWebHostEnvironment env,
        IConfiguration configurationRoot
    )
    {
        services.AddLogging(configure =>
        {
            configure.AddConfiguration(configurationRoot.GetSection("Logging"));
        });

        services.ConfigureApi();

        services.AddHealthChecks().AddInfrastructureHealthChecks();

        services.ConfigureOpenTelemetry(appName);

        //add custom instrumentation
        services
            .AddOpenTelemetry()
            .WithMetrics(options =>
            {
                options.AddMeter(ApiMeters.MeterName);
                options.AddMeter(QuartzInstrumentation.MeterName);
            })
            .WithTracing(options =>
            {
                options.AddSource(QuartzInstrumentation.ActivitySourceName);
                options.AddRedisInstrumentation();
            });

        services.ConfigureCors(apiConfiguration.Cors, env.IsDevelopment());

        return services;
    }

    public static WebApplication AddApi(this WebApplication app, ApiConfiguration apiConfiguration)
    {
        app.AddStandardApi(
            apiConfiguration.AuthenticationConfiguration,
            options =>
                options
                    .AddExceptionMapping<AltinnEventSourceParseException>(
                        HttpStatusCode.InternalServerError
                    )
                    .AddExceptionMapping<DocumentNotSafeToUseException>(HttpStatusCode.NotFound)
        );

        return app;
    }

    internal static IServiceCollection AddQuartz(
        this IServiceCollection services,
        string serviceConnection
    )
    {
        services.AddQuartz(q =>
        {
            // Unique cluster node id per pod (hostname + start time), like instanceId = AUTO in 3.x.
            q.ConfigureScheduler(o => o.GenerateInstanceId = true);
            q.UsePersistentStore(c =>
            {
                // Every pod joins the cluster; each firing runs on exactly one pod.
                c.UseClustering();
                c.ConfigureStore(store =>
                {
                    store.DbRetryInterval = TimeSpan.FromMinutes(2);
                    store.StoreJobDataAsStrings = true;
                    store.SchemaProvisioning = SchemaProvisioning.Validate;
                    store.TablePrefix = "quartz.qrtz_";
                });
                c.UseSystemTextJsonSerializer();
                c.UsePostgres(serviceConnection);
                c.UseDriverDelegate<PostgreSQLDelegate>();
            });
            // A failing scheduler should be visible, but must not take the API out of rotation.
            q.AddQuartzHealthChecks(o => o.FailureStatus = HealthStatus.Degraded);

            var jobKey = new JobKey("RecoveryJob");
            q.AddJob<RecoveryJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts =>
                opts.ForJob(jobKey)
                    .WithIdentity("RecoveryJob-trigger")
                    // Hourly on weekdays, 08:00-15:00 Norwegian time (8 firings)
                    .WithDailyTimeIntervalSchedule(s =>
                        s.WithInterval(1, IntervalUnit.Hour)
                            .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo"))
                            .OnMondayThroughFriday()
                            .StartingDailyAt(new TimeOnly(8, 0))
                            .EndingDailyAfterCount(8)
                    )
            );
        });
        services.AddQuartzHostedService(q =>
        {
            q.WaitForJobsToComplete = true;
            q.AwaitApplicationStarted = true;
        });

        return services;
    }
}
