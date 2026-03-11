using System.Diagnostics;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure;

internal static class Tracer
{
    public static readonly ActivitySource Source = new("Infrastructure");
}
