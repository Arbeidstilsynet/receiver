namespace Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;

public interface IDatabaseMigrationService
{
    Task RunMigrations();
}
