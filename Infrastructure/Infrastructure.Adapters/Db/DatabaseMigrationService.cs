using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Db;

internal class DatabaseMigrationService(
    ReceiverDbContext dbContext,
    ILogger<DatabaseMigrationService> logger
) : IDatabaseMigrationService
{
    public async Task RunMigrations()
    {
        logger.LogInformation("Applying database migrations...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");
    }
}
