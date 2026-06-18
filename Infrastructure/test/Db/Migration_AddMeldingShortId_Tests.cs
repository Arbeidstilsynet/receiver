using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Test.fixtures;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shouldly;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Test.Db
{
    public class Migration_AddMeldingShortId_Tests
        : IClassFixture<PostgresDbDemoFixture>,
            IAsyncLifetime
    {
        private readonly PostgresDbDemoFixture _fixture;
        private readonly string _connStr;

        public Migration_AddMeldingShortId_Tests(PostgresDbDemoFixture fixture)
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
        public async Task Migration_ComputesShortId_ForExistingRows()
        {
            var options = new DbContextOptionsBuilder<ReceiverDbContext>()
                .UseNpgsql(_connStr)
                .Options;

            // Migrate up to just before AddMeldingShortId.
            await using (var db = new ReceiverDbContext(options))
            {
                await db.Database.MigrateAsync(
                    "20260209123104_AddDocumentTags",
                    cancellationToken: TestContext.Current.CancellationToken
                );
            }

            // Insert a melding using the pre-migration schema (no ShortId column yet).
            var meldingId = Guid.Parse("22222222-2222-2222-2222-aaaabbbbcccc");
            await using (var conn = new NpgsqlConnection(_connStr))
            {
                await conn.OpenAsync(TestContext.Current.CancellationToken);
                var insert =
                    $@"
INSERT INTO public.meldinger
    (""Id"", ""ApplicationId"", ""CreatedAt"", ""InternalTags"", ""Source"", ""Tags"", ""UpdatedAt"", ""ReceivedAt"")
VALUES
    ('{meldingId}', 'ulykkesvarsel', NOW(), ''::hstore, 'testsource', ''::hstore, NOW(), NOW());
";
                await using var cmd = new NpgsqlCommand(insert, conn);
                await cmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
            }

            // Apply AddMeldingShortId.
            await using (var db = new ReceiverDbContext(options))
            {
                await db.Database.MigrateAsync(
                    cancellationToken: TestContext.Current.CancellationToken
                );
            }

            // The computed column should hold the trailing 12 hex characters of the GUID.
            await using (var conn = new NpgsqlConnection(_connStr))
            {
                await conn.OpenAsync(TestContext.Current.CancellationToken);
                await using var cmd = new NpgsqlCommand(
                    $@"SELECT ""ShortId"" FROM public.meldinger WHERE ""Id"" = '{meldingId}';",
                    conn
                );
                var shortId = (string?)
                    await cmd.ExecuteScalarAsync(TestContext.Current.CancellationToken);
                shortId.ShouldBe("aaaabbbbcccc");
            }
        }
    }
}
