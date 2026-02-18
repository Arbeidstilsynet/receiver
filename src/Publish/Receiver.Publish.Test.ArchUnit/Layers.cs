using ArchUnitNET.Domain;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ArchUnit.Tests
{
    internal static class Constants
    {
        internal static string NameSpacePrefix = @"Arbeidstilsynet\.Receiver";
        internal static string RootNamespace = $"^({NameSpacePrefix}|{NameSpacePrefix}\\..*)$";
        internal static string ExtensionsNamespace = CreateNamespaceRegex("Extensions");
        internal static string DependencyInjectionNamespace = CreateNamespaceRegex(
            "DependencyInjection"
        );
        internal static string ModelNamespace = CreateNamespaceRegex("Model");

        private static string CreateNamespaceRegex(string namespaceSection)
        {
            return $@"^({NameSpacePrefix}\.{namespaceSection}|{NameSpacePrefix}\.{namespaceSection}\..*|{NameSpacePrefix}\..*\.{namespaceSection}|{NameSpacePrefix}\..*\.{namespaceSection}\..*)$";
        }

        internal static string CoverageCollectorNamespace =
            "Microsoft.CodeCoverage.Instrumentation.Static.Tracker";
    }

    internal static class Layers
    {
        internal static readonly System.Reflection.Assembly MeldingerReceiverCommonAssembly =
            typeof(Arbeidstilsynet.Receiver.IAssemblyInfo).Assembly;

        internal static readonly System.Reflection.Assembly SystemConsoleAssembly =
            typeof(System.Console).Assembly;

        internal static readonly IObjectProvider<IType> MeldingerReceiverCommonLayer = Types()
            .That()
            .ResideInAssembly(MeldingerReceiverCommonAssembly)
            .And()
            .DoNotResideInNamespace(Constants.CoverageCollectorNamespace)
            .As("MeldingerReceiver Common Layer");

        internal static readonly IObjectProvider<IType> PublicInterfaces = Interfaces()
            .That()
            .Are(MeldingerReceiverCommonLayer)
            .And()
            .ArePublic()
            .As("public interfaces");

        internal static readonly IObjectProvider<IType> InterfaceImplementations = Classes()
            .That()
            .Are(MeldingerReceiverCommonLayer)
            .And()
            .AreAssignableTo(PublicInterfaces)
            .And()
            .AreNot(PublicInterfaces)
            .As("interface implementations");

        internal static readonly IObjectProvider<IType> PublicAbstractClasses = Classes()
            .That()
            .Are(MeldingerReceiverCommonLayer)
            .And()
            .AreAbstract()
            .And()
            .ArePublic()
            .As("public abstract classes");

        internal static readonly IObjectProvider<IType> ExportableTypes = Types()
            .That()
            .ResideInNamespaceMatching(Constants.ExtensionsNamespace)
            .Or()
            .ResideInNamespaceMatching(Constants.DependencyInjectionNamespace)
            .Or()
            .ResideInNamespaceMatching(Constants.ModelNamespace)
            .As("inside exportable namespaces");

        internal static readonly IObjectProvider<IType> TypesInInternalNamespaces = Types()
            .That()
            .Are(MeldingerReceiverCommonLayer)
            .And()
            .AreNot(ExportableTypes)
            .As("outside exportable namespaces");
    }
}
