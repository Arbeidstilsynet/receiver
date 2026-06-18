using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ArchUnit.Tests")]
[assembly: InternalsVisibleTo("MeldingerReceiver.Infrastructure.Test")]
[assembly: InternalsVisibleTo("MeldingerReceiver.App.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // Required for NSubstitute to make mocks of internal classes

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure;

interface IAssemblyInfo { }
