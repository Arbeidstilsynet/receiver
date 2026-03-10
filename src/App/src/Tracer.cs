using System.Diagnostics;

namespace Arbeidstilsynet.MeldingerReceiver.App;

internal static class Tracer
{
    public static readonly ActivitySource Source = new("App");
}
