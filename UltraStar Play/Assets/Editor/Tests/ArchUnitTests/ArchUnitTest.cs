using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Extensions;
using ArchUnitNET.Fluent.Predicates;
using ArchUnitNET.NUnit;
using NUnit.Framework;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

public class ArchUnitTest
{
    [Test]
    public void CommonOnlineMultiplayerDoesNotReferenceSpecificOnlineMultiplayer()
    {
        Architecture architecture = ArchUnitTestUtils.LoadArchitectureByAssemblyNames(ArchUnitTestUtils.gameSpecificRuntimeAssemblyNames);

        IObjectProvider<IType> commonOnlineMultiplayerTypes = Types().That().ResideInNamespace("CommonOnlineMultiplayer");
        IObjectProvider<IType> steamOnlineMultiplayerTypes = Types().That().ResideInNamespace("SteamOnlineMultiplayer");

        Classes().That().Are(commonOnlineMultiplayerTypes)
            .Should().NotDependOnAny(steamOnlineMultiplayerTypes)
            .Check(architecture);
    }

    [Test]
    public void ClassesThatUseInjectAttributeShouldImplementINeedInjection()
    {
        Architecture architecture = ArchUnitTestUtils.LoadArchitectureByAssemblyNames(ArchUnitTestUtils.gameSpecificRuntimeAssemblyNames);

        // Classes that make use of Inject attribute
        Classes().That().FollowCustomPredicate(HaveMemberWithAttribute("UniInject.InjectAttribute"))
        // Should implement INeedInjection interface
        .Should().ImplementInterface("UniInject.INeedInjection")
        // Run check on Architecture
        .Check(architecture);
    }

    private static IPredicate<Class> HaveMemberWithAttribute(string attributeFullName)
    {
        return new SimplePredicate<Class>(
            clazz => clazz.Members.AnyMatch(member => member.HasAttribute(attributeFullName)),
            $"have attribute with name {attributeFullName}");
    }
}
