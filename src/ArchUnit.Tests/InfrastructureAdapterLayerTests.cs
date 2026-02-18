using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
//add a using directive to ArchUnitNET.Fluent.ArchRuleDefinition to easily define ArchRules
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ArchUnit.Tests;

public class InfrastructureAdapterLayerTests
{
    static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(Layers.InfrastructureAdapterAssembly, Layers.SystemConsoleAssembly)
        .Build();

    [Fact]
    public void TypesInInfrastructureLayer_HaveInfrastructureNamespace()
    {
        IArchRule archRule = Types()
            .That()
            .Are(Layers.InfrastructureAdapterLayer)
            .Should()
            .ResideInNamespaceMatching(
                $"^({Constants.NameSpacePrefix}\\.Infrastructure\\.Adapters|{Constants.NameSpacePrefix}\\.Infrastructure\\.Adapters\\..*)$"
            );

        archRule.Check(Architecture);
    }

    [Fact]
    public void TypesInInfrastructureLayer_AreInternal()
    {
        IArchRule archRule = Types()
            .That()
            .Are(Layers.InfrastructureAdapterLayer)
            .And()
            .DoNotResideInNamespaceMatching(
                $"^({Constants.NameSpacePrefix}\\.Infrastructure\\.Adapters\\.DependencyInjection|{Constants.NameSpacePrefix}\\.Infrastructure\\.Adapters\\.DependencyInjection\\..*|{Constants.NameSpacePrefix}\\.Infrastructure\\.Adapters\\.Db\\.Migrations)$"
            )
            .Should()
            .NotBePublic();

        archRule.Check(Architecture);
    }

    [Fact]
    public void TypesInInfrastructureLayer_UseCorrectLogger()
    {
        IArchRule archRule = Types()
            .That()
            .Are(Layers.InfrastructureAdapterLayer)
            .Should()
            .NotDependOnAny(typeof(System.Console))
            .Because(
                "We want to use streamlined logging. Try using ILogger<T> via DependencyInjection to log."
            );
        archRule.Check(Architecture);
    }
}
