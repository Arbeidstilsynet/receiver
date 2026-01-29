using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
//add a using directive to ArchUnitNET.Fluent.ArchRuleDefinition to easily define ArchRules
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ArchUnit.Tests;

public class ArchUnitTests
{
    static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            Layers.DomainLogicAssembly,
            Layers.APIPortAssembly,
            Layers.APIAdapterAssembly,
            Layers.InfrastructureAdapterAssembly,
            Layers.InfrastructurePortAssembly,
            Layers.DomainAssembly
        )
        .Build();

    [Fact]
    public void InfrastructureAdapterLayerShouldOnlyAccessInfrastructurePortLayer()
    {
        var adapterLayerShouldNotAccessApplicationLayer = Types()
            .That()
            .Are(Layers.InfrastructureAdapterLayer)
            .Should()
            .NotDependOnAny(Layers.DomainLogicLayer)
            .Because(
                $"{Layers.InfrastructureAdapterLayer.Description} should only access {Layers.InfrastructurePortLayer.Description}. Did you add new project references in {Layers.InfrastructureAdapterLayer.Description}?"
            );

        var adapterLayerShouldNotAccessAPIPortLayer = Types()
            .That()
            .Are(Layers.InfrastructureAdapterLayer)
            .Should()
            .NotDependOnAny(Layers.APIPortLayer)
            .Because(
                $"{Layers.InfrastructureAdapterLayer.Description} should only access {Layers.InfrastructurePortLayer.Description}. Did you add new project references in {Layers.InfrastructureAdapterLayer.Description}?"
            );

        var combinedArchRule = adapterLayerShouldNotAccessApplicationLayer.And(
            adapterLayerShouldNotAccessAPIPortLayer
        );

        combinedArchRule.Check(Architecture);
    }

    [Fact]
    public void InfrastructurePortLayerShouldNotAccessAnyOtherLayersAtAll()
    {
        var portLayerShouldNotAccessInfrastructureAdapterLayer = Types()
            .That()
            .Are(Layers.InfrastructurePortLayer)
            .Should()
            .NotDependOnAny(Layers.InfrastructureAdapterLayer)
            .Because(
                $"{Layers.InfrastructurePortLayer.Description} should not access any other layers at all. Did you add new project references in {Layers.InfrastructurePortLayer.Description}?"
            );

        var portLayerShouldNotAccessApplicationLayer = Types()
            .That()
            .Are(Layers.InfrastructurePortLayer)
            .Should()
            .NotDependOnAny(Layers.DomainLogicLayer)
            .Because(
                $"{Layers.InfrastructurePortLayer.Description} should not access any other layers at all. Did you add new project references in {Layers.InfrastructurePortLayer.Description}?"
            );

        var portLayerShouldNotAccessAPIPortLayer = Types()
            .That()
            .Are(Layers.InfrastructurePortLayer)
            .Should()
            .NotDependOnAny(Layers.APIPortLayer)
            .Because(
                $"{Layers.InfrastructurePortLayer.Description} should not access any other layers at all. Did you add new project references in {Layers.InfrastructurePortLayer.Description}?"
            );

        var combinedArchRule = portLayerShouldNotAccessInfrastructureAdapterLayer
            .And(portLayerShouldNotAccessApplicationLayer)
            .And(portLayerShouldNotAccessAPIPortLayer);

        combinedArchRule.Check(Architecture);
    }

    [Fact]
    public void APIPortLayerShouldNotAccessAnyOtherLayersAtAll()
    {
        var portLayerShouldNotAccessInfrastructureAdapterLayer = Types()
            .That()
            .Are(Layers.APIPortLayer)
            .Should()
            .NotDependOnAny(Layers.InfrastructureAdapterLayer)
            .Because(
                $"{Layers.APIPortLayer.Description} should not access any other layers at all. Did you add new project references in {Layers.APIPortLayer.Description}?"
            );

        var portLayerShouldNotAccessApplicationLayer = Types()
            .That()
            .Are(Layers.APIPortLayer)
            .Should()
            .NotDependOnAny(Layers.DomainLogicLayer)
            .Because(
                $"{Layers.APIPortLayer.Description} should not access any other layers at all. Did you add new project references in {Layers.APIPortLayer.Description}?"
            );

        var portLayerShouldNotAccessInfrastructurePortLayer = Types()
            .That()
            .Are(Layers.APIPortLayer)
            .Should()
            .NotDependOnAny(Layers.InfrastructurePortLayer)
            .Because(
                $"{Layers.APIPortLayer.Description} should not access any other layers at all. Did you add new project references in {Layers.APIPortLayer.Description}?"
            );

        var combinedArchRule = portLayerShouldNotAccessInfrastructureAdapterLayer
            .And(portLayerShouldNotAccessApplicationLayer)
            .And(portLayerShouldNotAccessInfrastructurePortLayer);

        combinedArchRule.Check(Architecture);
    }

    [Fact]
    public void APIAdapterLayerShouldOnlyAccessPortLayers()
    {
        var adapterLayerShouldNotAccessApplicationLayer = Types()
            .That()
            .Are(Layers.APIAdapterLayer)
            .Should()
            .NotDependOnAny(Layers.DomainLogicLayer)
            .Because(
                $"{Layers.InfrastructureAdapterLayer.Description} should only have direct code references to {Layers.InfrastructurePortLayer.Description} or {Layers.APIPortLayer.Description}."
            );

        var adapterLayerShouldNotAccessInfrastructureAdapterLayer = Types()
            .That()
            .Are(Layers.APIAdapterLayer)
            .Should()
            .NotDependOnAny(Layers.InfrastructureAdapterLayer)
            .Because(
                $"{Layers.InfrastructureAdapterLayer.Description} should only have direct code references to {Layers.InfrastructurePortLayer.Description} or {Layers.APIPortLayer.Description}."
            );

        var combinedArchRule = adapterLayerShouldNotAccessApplicationLayer.And(
            adapterLayerShouldNotAccessInfrastructureAdapterLayer
        );

        combinedArchRule.Check(Architecture);
    }

    [Fact]
    public void ApplicationLayerShouldOnlyAccessDomainAndPortLayers()
    {
        var applicationLayerShouldNotAccessInfrastructureAdapterLayer = Types()
            .That()
            .Are(Layers.DomainLogicLayer)
            .Should()
            .NotDependOnAny(Layers.InfrastructureAdapterLayer)
            .Because(
                $"{Layers.DomainLogicLayer.Description} should only access {Layers.DomainLayer.Description}, {Layers.APIPortLayer.Description} or {Layers.InfrastructurePortLayer.Description}. Did you add new project references in {Layers.DomainLogicLayer.Description}?"
            );

        var combinedArchRule = applicationLayerShouldNotAccessInfrastructureAdapterLayer;

        combinedArchRule.Check(Architecture);
    }
}
