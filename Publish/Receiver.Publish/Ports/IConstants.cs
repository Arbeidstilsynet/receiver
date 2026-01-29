namespace Arbeidstilsynet.Meldinger.Receiver.Ports;

public interface IConstants
{
    public interface Stream
    {
        public const string MessageKey = "melding-received";

        public const string StreamName = "meldinger-receiver";
    }

    public interface ApiRoutes { }
}
