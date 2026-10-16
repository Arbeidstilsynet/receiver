using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Receiver.Publish.Test")]
[assembly: InternalsVisibleTo("Receiver.Publish.Test.ArchUnit")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Arbeidstilsynet.Receiver;

internal interface IAssemblyInfo { }
