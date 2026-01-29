using System.Diagnostics;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters;

internal static class Tracer
{
    public static readonly ActivitySource Source = new("Infrastructure.Adapters");
}
