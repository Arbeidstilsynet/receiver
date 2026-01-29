namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;

public interface IDatabaseMigrationService
{
    Task RunMigrations();
}
