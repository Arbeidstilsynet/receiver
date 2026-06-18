using ArchUnitNET.Domain;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ArchUnit.Tests
{
    internal static class Constants
    {
        internal static string NameSpacePrefix =
            $"Arbeidstilsynet\\.{Arbeidstilsynet.MeldingerReceiver.App.IAssemblyInfo.AppName}";

        internal static string CoverageCollectorNamespace =
            "Microsoft.CodeCoverage.Instrumentation.Static.Tracker";
    }

    internal static class Layers
    {
        internal static readonly System.Reflection.Assembly DomainLogicAssembly =
            typeof(Arbeidstilsynet.MeldingerReceiver.Domain.Logic.IAssemblyInfo).Assembly;
        internal static readonly System.Reflection.Assembly APIPortAssembly =
            typeof(Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App.IAssemblyInfo).Assembly;
        internal static readonly System.Reflection.Assembly InfrastructureAdapterAssembly =
            typeof(Arbeidstilsynet.MeldingerReceiver.Infrastructure.IAssemblyInfo).Assembly;
        internal static readonly System.Reflection.Assembly InfrastructurePortAssembly =
            typeof(Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure.IAssemblyInfo).Assembly;
        internal static readonly System.Reflection.Assembly DomainAssembly =
            typeof(Arbeidstilsynet.MeldingerReceiver.Domain.Data.IAssemblyInfo).Assembly;
        internal static readonly System.Reflection.Assembly APIAdapterAssembly =
            typeof(Arbeidstilsynet.MeldingerReceiver.App.IAssemblyInfo).Assembly;

        internal static readonly System.Reflection.Assembly SystemConsoleAssembly =
            typeof(System.Console).Assembly;

        internal static readonly IObjectProvider<IType> DomainLogicLayer = Types()
            .That()
            .ResideInAssembly(DomainLogicAssembly)
            .And()
            .DoNotResideInNamespace(Constants.CoverageCollectorNamespace)
            .And()
            .DoNotResideInNamespaceMatching(
                $"^({Constants.CoverageCollectorNamespace}|{Constants.NameSpacePrefix}\\.Domain\\.Logic\\.DependencyInjection|{Constants.NameSpacePrefix}\\.Domain\\.Logic\\.DependencyInjection\\..*)$"
            )
            .As("Application Service Layer");
        internal static readonly IObjectProvider<IType> InfrastructureAdapterLayer = Types()
            .That()
            .ResideInAssembly(InfrastructureAdapterAssembly)
            .And()
            .DoNotResideInNamespaceMatching(
                $"^({Constants.CoverageCollectorNamespace}|{Constants.NameSpacePrefix}\\.Infrastructure\\.DependencyInjection|{Constants.NameSpacePrefix}\\.Infrastructure\\.DependencyInjection\\..*)$"
            )
            .And()
            .DoNotResideInNamespace(Constants.CoverageCollectorNamespace)
            .As("Infrastructure Adapter Layer");
        internal static readonly IObjectProvider<IType> InfrastructurePortLayer = Types()
            .That()
            .ResideInAssembly(InfrastructurePortAssembly)
            .And()
            .DoNotResideInNamespace(Constants.CoverageCollectorNamespace)
            .As("Infrastructure Port Layer");
        internal static readonly IObjectProvider<IType> APIPortLayer = Types()
            .That()
            .ResideInAssembly(APIPortAssembly)
            .And()
            .DoNotResideInNamespace(Constants.CoverageCollectorNamespace)
            .As("API Port Layer");
        internal static readonly IObjectProvider<IType> DomainLayer = Types()
            .That()
            .ResideInAssembly(DomainAssembly)
            .And()
            .DoNotResideInNamespace(Constants.CoverageCollectorNamespace)
            .As("Domain Layer");
        internal static readonly IObjectProvider<IType> APIAdapterLayer = Types()
            .That()
            .ResideInAssembly(APIAdapterAssembly)
            .And()
            .DoNotResideInNamespace(Constants.CoverageCollectorNamespace)
            .As("API Adapter Layer");
    }
}
