using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MeldingerReceiver.App.Test")]

namespace Arbeidstilsynet.MeldingerReceiver.App;

public interface IAssemblyInfo
{
    public const string AppName = "MeldingerReceiver";
}
