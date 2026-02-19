using System.Diagnostics;

namespace Arbeidstilsynet.MeldingerReceiver.API.Adapters;

internal static class Tracer
{
    public static readonly ActivitySource Source = new("API.Adapters");
}
