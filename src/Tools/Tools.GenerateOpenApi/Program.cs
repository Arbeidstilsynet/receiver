using Arbeidstilsynet.MeldingerReceiver.App.Extensions;

var builder = WebApplication.CreateBuilder(args);

var appAssembly = typeof(Arbeidstilsynet.MeldingerReceiver.App.IAssemblyInfo).Assembly;

builder.Services.ConfigureApi().AddApplicationPart(appAssembly);

var app = builder.Build();

await app.RunAsync();
