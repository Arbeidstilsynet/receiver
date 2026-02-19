using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ArchUnit.Tests")]
[assembly: InternalsVisibleTo("MeldingerReceiver.Infrastructure.Adapters.Test")]
[assembly: InternalsVisibleTo("MeldingerReceiver.API.Adapters.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // Required for NSubstitute to make mocks of internal classes

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters;

interface IAssemblyInfo { }
