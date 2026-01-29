using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MeldingerReceiver.API.Adapters.Test")]

namespace Arbeidstilsynet.MeldingerReceiver.API.Adapters;

public interface IAssemblyInfo
{
    public const string AppName = "MeldingerReceiver";
}
