using System.Net;
using Arbeidstilsynet.Common.Altinn.Model.Exceptions;
using Arbeidstilsynet.Common.AspNetCore.Extensions.CrossCutting;
using Arbeidstilsynet.Common.AspNetCore.Extensions.Extensions;
using Arbeidstilsynet.MeldingerReceiver.API.Adapters.Jobs;
using Arbeidstilsynet.MeldingerReceiver.API.Adapters.WebApi;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data.Exceptions;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.DependencyInjection;
using OpenTelemetry.Trace;
using Quartz;
using Quartz.Impl.AdoJobStore;

namespace Arbeidstilsynet.MeldingerReceiver.API.Adapters.Extensions;

internal static class StartupExtensions
{
    public static IServiceCollection ConfigureStandardApi(
        this IServiceCollection services,
        string appName,
        ApiConfiguration apiConfiguration,
        IWebHostEnvironment env,
        StartupChecks? StartupChecks = null
    )
    {
        services.ConfigureApi(
            startupChecks: StartupChecks,
            buildHealthChecksAction: builder => builder.AddInfrastructureHealthChecks()
        );

        services.ConfigureOpenTelemetry(appName);

        //add custom instrumentation
        services
            .AddOpenTelemetry()
            .WithMetrics(options => options.AddMeter(ApiMeters.MeterName))
            .WithTracing(options =>
            {
                options.AddQuartzInstrumentation();
                options.AddRedisInstrumentation();
            });
        services.AddOpenApi();

        services.ConfigureCors(
            apiConfiguration.Cors.AllowedOrigins,
            apiConfiguration.Cors.AllowCredentials,
            env.IsDevelopment()
        );

        return services;
    }

    public static WebApplication AddStandardApi(this WebApplication app)
    {
        app.AddApi(options =>
            options
                .AddExceptionMapping<AltinnEventSourceParseException>(
                    HttpStatusCode.InternalServerError
                )
                .AddExceptionMapping<DocumentNotSafeToUseException>(HttpStatusCode.NotFound)
        );
        app.UseCors();
        app.AddScalar();

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
