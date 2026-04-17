using System.Net;
using Arbeidstilsynet.Common.Altinn.Model.Exceptions;
using Arbeidstilsynet.Common.AspNetCore.Extensions.CrossCutting;
using Arbeidstilsynet.Common.AspNetCore.Extensions.Extensions;
using Arbeidstilsynet.MeldingerReceiver.App.Jobs;
using Arbeidstilsynet.MeldingerReceiver.App.WebApi;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data.Exceptions;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.DependencyInjection;
using Microsoft.OpenApi;
using OpenTelemetry.Trace;
using Quartz;
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
            configure.AddConfiguration(configurationRoot);
        });

        services.ConfigureApi();

        services.AddHealthChecks().AddInfrastructureHealthChecks();

        services.ConfigureOpenTelemetry(appName);

        services.AddOpenApi(openApiOptions =>
            openApiOptions.ConfigureBasicOpenApiSpec(IAssemblyInfo.AppName)
        );

        //add custom instrumentation
        services
            .AddOpenTelemetry()
            .WithMetrics(options => options.AddMeter(ApiMeters.MeterName))
            .WithTracing(options =>
            {
                options.AddQuartzInstrumentation();
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
        string serviceConnection,
        IWebHostEnvironment webHostEnvironment
    )
    {
        services.AddQuartz(q =>
        {
            q.UsePersistentStore(c =>
            {
                c.RetryInterval = TimeSpan.FromMinutes(2);
                c.UseProperties = true;
                c.PerformSchemaValidation = true;
                c.UseNewtonsoftJsonSerializer();
                c.UsePostgres(postgres =>
                {
                    postgres.ConnectionString = serviceConnection;
                    postgres.UseDriverDelegate<PostgreSQLDelegate>();
                    postgres.TablePrefix = "quartz.qrtz_";
                });
            });
            // Just use the name of your job that you created in the Jobs folder.
            var jobKey = new JobKey("RecoveryJob");
            q.AddJob<RecoveryJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts =>
                opts.ForJob(jobKey)
                    .WithIdentity("RecoveryJob-trigger")
                    // run every weekday from 8-16
                    .WithDailyTimeIntervalSchedule(
                        1,
                        IntervalUnit.Hour,
                        s =>
                            s.OnMondayThroughFriday()
                                .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(8, 0))
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
