using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ArchUnit.Tests")]
[assembly: InternalsVisibleTo("MeldingerReceiver.Domain.Logic.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // Required for NSubstitute to make mocks of internal classes

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Logic;

interface IAssemblyInfo { }
