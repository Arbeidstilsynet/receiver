using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
//add a using directive to ArchUnitNET.Fluent.ArchRuleDefinition to easily define ArchRules
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ArchUnit.Tests;

public class DomainLogicLayerTests
{
    static readonly Architecture Architecture = new ArchLoader()
        .LoadAssembliesIncludingDependencies(
            Layers.DomainLogicAssembly,
            Layers.SystemConsoleAssembly
        )
        .Build();

    [Fact]
    public void TypesInDomainLogicLayer_HaveApplicationNamespace()
    {
        IArchRule archRule = Types()
            .That()
            .Are(Layers.DomainLogicLayer)
            .Should()
            .ResideInNamespaceMatching(
                $"^({Constants.NameSpacePrefix}\\.Domain\\.Logic|{Constants.NameSpacePrefix}\\.Domain\\.Logic\\..*)$"
            );

        archRule.Check(Architecture);
    }

    [Fact]
    public void TypesInDomainLogicLayer_AreInternal()
    {
        IArchRule archRule = Types().That().Are(Layers.DomainLogicLayer).Should().NotBePublic();

        archRule.Check(Architecture);
    }

    [Fact]
    public void TypesInDomainLogicLayer_DoNotDependOnAWS()
    {
        IArchRule archRule = Types()
            .That()
            .Are(Layers.DomainLogicLayer)
            .Should()
            .NotDependOnAny(Types().That().ResideInNamespaceMatching("^Amazon.*$"));

        archRule.Check(Architecture);
    }

    [Fact]
    public void TypesInDomainLogicLayer_UseCorrectLogger()
    {
        IArchRule archRule = Types()
            .That()
            .Are(Layers.DomainLogicLayer)
            .Should()
            .NotDependOnAny(typeof(System.Console))
            .Because(
                "We want to use streamlined logging. Try using ILogger<T> via DependencyInjection to log."
            );

        archRule.Check(Architecture);
    }
}
