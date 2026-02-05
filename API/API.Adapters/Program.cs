using Arbeidstilsynet.Common.Altinn.DependencyInjection;
using Arbeidstilsynet.Common.AspNetCore.Extensions.Extensions;
using Arbeidstilsynet.MeldingerReceiver.API.Adapters;
using Arbeidstilsynet.MeldingerReceiver.API.Adapters.Extensions;
using Arbeidstilsynet.MeldingerReceiver.API.Adapters.WebApi;
using Arbeidstilsynet.MeldingerReceiver.Domain.Logic.DependencyInjection;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.DependencyInjection;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using Quartz;
using IAssemblyInfo = Arbeidstilsynet.MeldingerReceiver.API.Adapters.IAssemblyInfo;

var builder = WebApplication.CreateBuilder(args);

var appSettings = builder.Configuration.GetRequired<AppSettings>();
var services = builder.Services;
var env = builder.Environment;

var appNameFromConfig = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME");
services.ConfigureStandardApi(
    string.IsNullOrEmpty(appNameFromConfig) ? IAssemblyInfo.AppName : appNameFromConfig,
    appSettings.ApiConfig,
    env
);

services.AddSingleton<ApiMeters>();

services.AddDomain(appSettings.DomainConfig);

//overwrite settings with constants
appSettings.InfrastructureConfig.ValkeyConfiguration.StreamName = Arbeidstilsynet
    .Receiver
    .Ports
    .IConstants
    .Stream
    .StreamName;
appSettings.InfrastructureConfig.ValkeyConfiguration.MessageKey = Arbeidstilsynet
    .Receiver
    .Ports
    .IConstants
    .Stream
    .MessageKey;
services.AddInfrastructure(appSettings.InfrastructureConfig);

services.AddAltinnAdapter(env, appSettings.InfrastructureConfig.MaskinportenConfiguration, appSettings.InfrastructureConfig.AltinnConfiguration);
services.AddQuartz(appSettings.InfrastructureConfig.PostgresConfiguration.ConnectionString, env);
var app = builder.Build();

if (env.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
app.AddStandardApi();

using (var scope = app.Services.CreateScope())
{
    var migrationService = scope.ServiceProvider.GetRequiredService<IDatabaseMigrationService>();
    await migrationService.RunMigrations();
}

await app.RunAsync();
