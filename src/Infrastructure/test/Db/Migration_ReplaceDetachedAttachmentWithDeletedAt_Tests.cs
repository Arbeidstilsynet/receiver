using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db.Model;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Test.fixtures;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shouldly;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Test.Db
{
    public class Migration_ReplaceDetachedAttachmentWithDeletedAt_Tests
        : IClassFixture<PostgresDbDemoFixture>,
            IAsyncLifetime
    {
        private readonly PostgresDbDemoFixture _fixture;
        private readonly string _connStr;

        public Migration_ReplaceDetachedAttachmentWithDeletedAt_Tests(PostgresDbDemoFixture fixture)
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
        public async Task Migration_ConvertsDetachedAttachmentsToSoftDeletedAttachments()
        {
            var options = new DbContextOptionsBuilder<ReceiverDbContext>()
                .UseNpgsql(_connStr)
                .Options;
            await using (var db = new ReceiverDbContext(options))
            {
                await db.Database.MigrateAsync(
                    "20261006071322_PrepareForQuartzV4Changes",
                    cancellationToken: TestContext.Current.CancellationToken
                );
            }

            var meldingId = Guid.NewGuid();
            var activeId = Guid.NewGuid();
            var detachedId = Guid.NewGuid();
            var detachedUpdatedAt = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

            await using (var conn = new NpgsqlConnection(_connStr))
            {
                await conn.OpenAsync(TestContext.Current.CancellationToken);
                var insert =
                    $@"
INSERT INTO public.meldinger
    (""Id"", ""ApplicationId"", ""CreatedAt"", ""InternalTags"", ""Source"", ""Tags"", ""UpdatedAt"", ""ReceivedAt"")
VALUES
    ('{meldingId}', 'ulykkesvarsel', NOW(), ''::hstore, 'Api', ''::hstore, NOW(), NOW());
INSERT INTO public.documents
    (""Id"", ""InternalDocumentReference"", ""MeldingId"", ""CreatedAt"", ""UpdatedAt"", ""DocumentType"", ""ScanResult"", ""Tags"")
VALUES
    ('{activeId}', 'ref1', '{meldingId}', NOW(), NOW(), 'Attachment', 'Clean', ''::hstore),
    ('{detachedId}', 'ref2', '{meldingId}', NOW(), '{detachedUpdatedAt:O}', 'DetachedAttachment', 'Clean', ''::hstore);
";
                await using var cmd = new NpgsqlCommand(insert, conn);
                await cmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
            }

            await using (var db = new ReceiverDbContext(options))
            {
                await db.Database.MigrateAsync(
                    cancellationToken: TestContext.Current.CancellationToken
                );

                var docs = await db
                    .Documents.IgnoreQueryFilters()
                    .Where(d => d.MeldingId == meldingId)
                    .ToListAsync(TestContext.Current.CancellationToken);

                docs.ShouldAllBe(d => d.DocumentType == DocumentType.Attachment);
                docs.Single(d => d.Id == activeId).DeletedAt.ShouldBeNull();
                docs.Single(d => d.Id == detachedId).DeletedAt.ShouldBe(detachedUpdatedAt);
                (
                    await db
                        .Documents.Where(d => d.MeldingId == meldingId)
                        .ToListAsync(TestContext.Current.CancellationToken)
                )
                    .ShouldHaveSingleItem()
                    .Id.ShouldBe(activeId);
            }
        }
    }
}
