using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
//add a using directive to ArchUnitNET.Fluent.ArchRuleDefinition to easily define ArchRules
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ArchUnit.Tests;

public class APIPortLayerTests
{
    static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(Layers.APIPortAssembly, Layers.SystemConsoleAssembly)
        .Build();

    [Fact]
    public void TypesInAPIPortLayer_HaveAPIPortsNamespace()
    {
        IArchRule archRule = Types()
            .That()
            .Are(Layers.APIPortLayer)
            .Should()
            .ResideInNamespaceMatching(
                $"^({Constants.NameSpacePrefix}\\.Domain\\.Ports\\.App|{Constants.NameSpacePrefix}\\.Domain\\.Ports\\.App\\..*)$"
            );

        archRule.Check(Architecture);
    }

    [Fact]
    public void TypesInAPIPortLayer_ArePublic()
    {
        IArchRule archRule = Types().That().Are(Layers.APIPortLayer).Should().BePublic();

        archRule.Check(Architecture);
    }

    [Fact]
    public void TypesInAPIPortLayer_DoNotDependOnOtherTypesThanAPIPorts()
    {
        IArchRule archRule = Types()
            .That()
            .Are(Layers.APIPortLayer)
            .Should()
            .NotDependOnAny(
                Types()
                    .That()
                    .DoNotResideInNamespaceMatching(
                        $"^({Constants.CoverageCollectorNamespace}|System.*|{Constants.NameSpacePrefix}\\.Domain\\.Ports\\.App|{Constants.NameSpacePrefix}\\.Domain\\.Ports\\.App\\..*|{Constants.NameSpacePrefix}\\.Domain\\.Data.*)$"
                    )
            );

        archRule.Check(Architecture);
    }
}
