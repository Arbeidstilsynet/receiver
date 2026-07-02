using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MeldingerReceiver.App.Test")]
[assembly: InternalsVisibleTo("Tools.GenerateOpenApi")]

namespace Arbeidstilsynet.MeldingerReceiver.App;

public interface IAssemblyInfo
{
    public const string AppName = "MeldingerReceiver";
}
