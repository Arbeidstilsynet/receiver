using System.Diagnostics;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Logic;

internal static class Tracer
{
    public static readonly ActivitySource Source = new("Domain.Logic");
}
