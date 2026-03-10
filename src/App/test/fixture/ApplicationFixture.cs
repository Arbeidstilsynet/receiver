using System.Text;
using Arbeidstilsynet.Common.Altinn.Model.Api.Response;
using Arbeidstilsynet.Common.Altinn.Ports.Adapter;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.DependencyInjection;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Storage;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Test.fixtures;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Quartz;
using FileMetadata = Arbeidstilsynet.MeldingerReceiver.Domain.Data.FileMetadata;

namespace Arbeidstilsynet.MeldingerReceiver.App.Test.fixture;

public class ApplicationFixture : WebApplicationFactory<IAssemblyInfo>, IAsyncLifetime
{
    private readonly IAltinnAdapter _altinnAdapterMock = Substitute.For<IAltinnAdapter>();
    private readonly IVirusScanService _virusScanServiceMock = Substitute.For<IVirusScanService>();

    private readonly PostgresDbDemoFixture _postgresDbDemoFixture = new();
    private readonly StorageFixture _storageFixture = new();
    private volatile bool _isSeeded;

    public ApplicationFixture()
    {
        var fakeAltinnSubscription = TestData.CreateSubscriptionFaker().Generate();
        _altinnAdapterMock
            .SubscribeForCompletedProcessEvents(default!)
            .ReturnsForAnyArgs(fakeAltinnSubscription);
        _altinnAdapterMock.UnsubscribeForCompletedProcessEvents(default!).ReturnsForAnyArgs(true);
        _altinnAdapterMock.GetAltinnSubscription(Arg.Any<int>()).Returns((AltinnSubscription?)null);
        _altinnAdapterMock
            .GetAltinnSubscription(fakeAltinnSubscription.Id)
            .Returns(new AltinnSubscription { Id = fakeAltinnSubscription.Id });
        _altinnAdapterMock
            .GetSummary(default!)
            .ReturnsForAnyArgs(TestData.CreateAltinnInstanceSummaryFaker().Generate());

        _virusScanServiceMock
            .ScanForVirus(default!, default!)
            .ReturnsForAnyArgs(DocumentScanResult.Clean);
    }

    public IMeldingNotificationService NotificationServiceMock { get; } =
        Substitute.For<IMeldingNotificationService>();

    public static string[] KnownApplicationIds =>
        [KnownApplicationId, SafeToDeleteApplicationId, "applikasjon-2", "applikasjon-3"];

    public const string KnownApplicationId = "ulykkesvarsel";
    public const string SafeToDeleteApplicationId = "flip-flop-varsel";
    public static Guid KnownMeldingId { get; } = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static Guid KnownAttachmentDocumentId { get; } =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static Guid KnownStructuredDataId { get; } =
        Guid.Parse("33333333-3333-3333-3333-333333333333");

    public const string KnownDocumentContent = "Hello World.";

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        await _storageFixture.InitializeAsync();
        await _postgresDbDemoFixture.InitializeAsync();
        await EnsureSeededAsync();
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await _postgresDbDemoFixture.DisposeAsync();
        await _storageFixture.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.Replace<ISchedulerFactory>();
            services.Replace<IVirusScanService>(_ => _virusScanServiceMock);
            services.Replace<IMeldingNotificationService>(_ => NotificationServiceMock);
            services.Replace<IAltinnAdapter>(_ => _altinnAdapterMock);

            services.RemoveAll<IHostedService>();

            // replacing the current db context
            services.RemoveAll<InfrastructureConfiguration>();
            var infraConfig = Substitute.For<InfrastructureConfiguration>();
            infraConfig.PostgresConfiguration.Returns(
                new PostgresConfiguration
                {
                    ConnectionString = _postgresDbDemoFixture.ConnectionString,
                }
            );
            infraConfig.DocumentStorageConfiguration.Returns(
                new DocumentStorageConfiguration
                {
                    AuthRequired = false,
                    BaseUrl = _storageFixture.StorageBaseUrl,
                    BucketName = "test-bucket",
                }
            );
            services.AddSingleton(infraConfig);
            services.RemoveAll<ReceiverDbContext>();
            services.RemoveAll<DbContextOptions<ReceiverDbContext>>();

            services.AddDbContext<ReceiverDbContext>(opt =>
            {
                opt.UseNpgsql(_postgresDbDemoFixture.ConnectionString);
            });

            services.RemoveAll<DocumentStorage>();
            services.AddSingleton(
                new StorageClientBuilder
                {
                    BaseUri = infraConfig.DocumentStorageConfiguration.BaseUrl,
                    UnauthenticatedAccess = !infraConfig.DocumentStorageConfiguration.AuthRequired,
                }.Build()
            );
        });
    }

    public async Task EnsureSeededAsync()
    {
        if (_isSeeded)
            return;

        using var scope = Services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<IDatabaseMigrationService>().RunMigrations();

        var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

        // subscribe all before sending in messages
        foreach (var applicationReference in KnownApplicationIds)
        {
            await subscriptionService.CreateSubscription(
                new ConsumerManifest
                {
                    ConsumerName = applicationReference,
                    AppRegistrations =
                    [
                        new AppRegistration
                        {
                            AppId = applicationReference,
                            MessageSource = MessageSource.Api,
                        },
                        new AppRegistration
                        {
                            AppId = applicationReference,
                            MessageSource = MessageSource.Altinn,
                        },
                    ],
                }
            );
        }

        var postMeldingRequestFaker = TestData
            .CreatePostMeldingRequestFaker()
            .RuleFor(r => r.ApplicationReference, f => f.PickRandom(KnownApplicationIds));

        var meldingService = scope.ServiceProvider.GetRequiredService<IMeldingService>();

        var knownRequest = postMeldingRequestFaker.Generate() with
        {
            MeldingId = KnownMeldingId,
            ApplicationReference = KnownApplicationId,
            MainContent = new UploadDocumentRequest
            {
                DocumentId = KnownMeldingId, // Using MeldingId as DocumentId for simplicity
                FileMetadata = new FileMetadata
                {
                    FileName = "main-content.txt",
                    ContentType = "text/plain",
                },
                InputStream = new MemoryStream(Encoding.UTF8.GetBytes(KnownDocumentContent)),
                ScanResult = DocumentScanResult.Clean,
            },
            StructuredData = new UploadDocumentRequest()
            {
                DocumentId = KnownStructuredDataId,
                FileMetadata = new FileMetadata
                {
                    FileName = "structured-data.json",
                    ContentType = "application/json",
                },
                InputStream = new MemoryStream(Encoding.UTF8.GetBytes("{ \"key\": \"value\" }")),
                ScanResult = DocumentScanResult.Clean,
            },
            Attachments =
            [
                new UploadDocumentRequest
                {
                    DocumentId = KnownAttachmentDocumentId,
                    FileMetadata = new FileMetadata
                    {
                        FileName = "hello-world.txt",
                        ContentType = "text/plain",
                    },
                    InputStream = new MemoryStream(Encoding.UTF8.GetBytes(KnownDocumentContent)),
                    ScanResult = DocumentScanResult.Clean,
                },
                new UploadDocumentRequest
                {
                    DocumentId = Guid.NewGuid(),
                    FileMetadata = new FileMetadata
                    {
                        FileName = "hello-underworld.txt",
                        ContentType = "text/plain",
                    },
                    InputStream = new MemoryStream("Malware content"u8.ToArray()),
                    ScanResult = DocumentScanResult.Infected,
                },
            ],
        };

        await meldingService.ProcessMelding(knownRequest, TestContext.Current.CancellationToken);

        foreach (var request in postMeldingRequestFaker.Generate(3))
        {
            await meldingService.ProcessMelding(request, TestContext.Current.CancellationToken);
        }

        _isSeeded = true;
    }
}

file static class Extensions
{
    public static IServiceCollection Replace<T>(
        this IServiceCollection services,
        Func<IServiceProvider, T>? factory = null
    )
        where T : class
    {
        factory ??= _ => Substitute.For<T>();

        services.RemoveAll<T>();
        services.AddSingleton(factory);

        return services;
    }
}
