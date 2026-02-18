using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Db;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Db.Model;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Test.fixtures;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shouldly;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Test.Db
{
    public class Migration_AddStructuredData_Tests
        : IClassFixture<PostgresDbDemoFixture>,
            IAsyncLifetime
    {
        private readonly PostgresDbDemoFixture _fixture;
        private readonly string _connStr;

        public Migration_AddStructuredData_Tests(PostgresDbDemoFixture fixture)
        {
            _fixture = fixture;
            _connStr = fixture.ConnectionString;
        }

        public async ValueTask InitializeAsync()
        {
            await _fixture.InitializeAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _fixture.DisposeAsync();
        }

        [Fact]
        public async Task Migration_Sets_DocumentType_Correctly()
        {
            // Migrate up to just before AddStructuredData
            var options = new DbContextOptionsBuilder<ReceiverDbContext>()
                .UseNpgsql(_connStr)
                .Options;
            await using (var db = new ReceiverDbContext(options))
            {
                await db.Database.MigrateAsync(
                    "20260126065135_RefactorSubscriptionModel",
                    cancellationToken: TestContext.Current.CancellationToken
                );
            }

            // Insert test data using pre-migration schema (with IsAttachment)
            var meldingId1 = Guid.NewGuid();
            var meldingId2 = Guid.NewGuid();
            var docId1 = Guid.NewGuid(); // IsAttachment = false
            var docId2 = Guid.NewGuid(); // IsAttachment = true, normal attachment
            var docId3 = Guid.NewGuid(); // IsAttachment = true, special case
            var docId4 = Guid.NewGuid(); // IsAttachment = true, normal attachment
            var docId5 = Guid.NewGuid(); // IsAttachment = true, non-migratable case (different ApplicationId)

            await using (var conn = new NpgsqlConnection(_connStr))
            {
                await conn.OpenAsync(TestContext.Current.CancellationToken);
                var insertMeldinger =
                    $@"
INSERT INTO public.meldinger
    (""Id"", ""ApplicationId"", ""CreatedAt"", ""InternalTags"", ""Source"", ""Tags"", ""UpdatedAt"")
VALUES
    ('{meldingId1}', 'ulykkesvarsel', NOW(), ''::hstore, 'testsource', ''::hstore, NOW()),
    ('{meldingId2}', 'otherapp', NOW(), ''::hstore, 'testsource', ''::hstore, NOW());
";

                await using (var cmd = new NpgsqlCommand(insertMeldinger, conn))
                {
                    await cmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
                }
                var insertDocuments =
                    $@"
INSERT INTO public.documents
    (""Id"", ""InternalDocumentReference"", ""MeldingId"", ""CreatedAt"", ""UpdatedAt"", ""FileName"", ""IsAttachment"")
VALUES
    ('{docId1}', 'ref1', '{meldingId1}', NOW(), NOW(), 'MainContent.xml', FALSE),
    ('{docId2}', 'ref2', '{meldingId1}', NOW(), NOW(), 'Attachment1.pdf', TRUE),
    ('{docId3}', 'ref3', '{meldingId1}', NOW(), NOW(), 'Varsel om arbeidsulykke med alvorlig personskade eller dødsfall.pdf', TRUE),
    ('{docId4}', 'ref4', '{meldingId1}', NOW(), NOW(), 'Attachment2.pdf', TRUE),
    ('{docId5}', 'ref5', '{meldingId2}', NOW(), NOW(), 'Varsel om arbeidsulykke med alvorlig personskade eller dødsfall.pdf', TRUE);
";
                await using (var cmd = new NpgsqlCommand(insertDocuments, conn))
                {
                    await cmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
                }
            }

            // Migrate to latest (applies AddStructuredData)
            await using (var db = new ReceiverDbContext(options))
            {
                await db.Database.MigrateAsync(
                    cancellationToken: TestContext.Current.CancellationToken
                );
            }

            // Assert using EF Core
            var options2 = new DbContextOptionsBuilder<ReceiverDbContext>()
                .UseNpgsql(_connStr)
                .Options;
            await using (var db2 = new ReceiverDbContext(options2))
            {
                var docs = await db2.Documents.ToListAsync(
                    cancellationToken: TestContext.Current.CancellationToken
                );
                docs.ShouldContain(d =>
                    d.Id == docId1 && d.DocumentType == DocumentType.StructuredData
                );
                docs.ShouldContain(d =>
                    d.Id == docId2 && d.DocumentType == DocumentType.Attachment
                );
                docs.ShouldContain(d =>
                    d.Id == docId3 && d.DocumentType == DocumentType.Attachment
                );
                docs.ShouldContain(d =>
                    d.Id == docId4 && d.DocumentType == DocumentType.Attachment
                );
                docs.ShouldContain(d =>
                    d.Id == docId5 && d.DocumentType == DocumentType.Attachment
                );
            }
        }
    }
}
