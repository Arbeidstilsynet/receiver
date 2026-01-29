using ArchUnitNET.Domain;
using ArchUnitNET.Fluent.Syntax.Elements.Types;
//add a using directive to ArchUnitNET.Fluent.ArchRuleDefinition to easily define ArchRules
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ArchUnit.Tests;

internal static class ArchUnitExtensions
{
    public static TypesShouldConjunction ShouldNotDependOnTypesIn(
        this IObjectProvider<IType> myTypes,
        IObjectProvider<IType> typesToNotDependOn,
        params IObjectProvider<IType>[] additionalTypesToNotDependOn
    )
    {
        var rule = Types().That().Are(myTypes).Should().NotDependOnAny(typesToNotDependOn);

        return additionalTypesToNotDependOn.Aggregate(
            rule,
            (current, type) =>
                current.And().Types().That().Are(myTypes).Should().NotDependOnAny(type)
        );
    }
}
