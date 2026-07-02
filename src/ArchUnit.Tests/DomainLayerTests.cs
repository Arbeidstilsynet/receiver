using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
//add a using directive to ArchUnitNET.Fluent.ArchRuleDefinition to easily define ArchRules
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ArchUnit.Tests;

public class DomainLayerTests
{
    static readonly Architecture Architecture = new ArchLoader()
        .LoadAssembliesIncludingDependencies(Layers.DomainAssembly, Layers.SystemConsoleAssembly)
        .Build();

    [Fact]
    public void TypesInDomainLayer_HaveDomainNamespace()
    {
        IArchRule archRule = Types()
            .That()
            .Are(Layers.DomainLayer)
            .Should()
            .ResideInNamespaceMatching(
                $"^({Constants.NameSpacePrefix}\\.Domain\\.Data|{Constants.NameSpacePrefix}\\.Domain\\.Data\\..*)$"
            );

        archRule.Check(Architecture);
    }

    [Fact]
    public void TypesInDomainLayer_ArePublic()
    {
        IArchRule archRule = Types().That().Are(Layers.DomainLayer).Should().BePublic();

        archRule.Check(Architecture);
    }

    [Fact]
    public void TypesInDomainLayer_DoNotDependOnOtherTypesThanDomain()
    {
        IArchRule archRule = Types()
            .That()
            .Are(Layers.DomainLayer)
            .Should()
            .NotDependOnAny(
                Types()
                    .That()
                    .DoNotResideInNamespaceMatching(
                        $"^(System.*|{Constants.CoverageCollectorNamespace}|{Constants.NameSpacePrefix}\\.Domain\\.Data.*)$"
                    )
            );

        archRule.Check(Architecture);
    }
}
