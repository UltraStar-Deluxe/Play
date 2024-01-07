using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Extensions;
using ArchUnitNET.Fluent.Conditions;
using ArchUnitNET.Fluent.Predicates;
using ArchUnitNET.NUnit;
using NUnit.Framework;
using UnityEngine;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

public class ArchUnitTests
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

    [Test]
    public void ClassesShouldNotBeEmptyInArchUnitTest()
    {
        Architecture architecture = ArchUnitTestUtils.LoadArchitectureByAssemblyNames(ArchUnitTestUtils.gameSpecificRuntimeAssemblyNames);

        IntWrapper conditionExecutionCount = new();
        Classes()
            .Should().FollowCustomCondition(CountConditionExecutions(conditionExecutionCount))
            .Check(architecture);

        Debug.Log($"Condition execution count: {conditionExecutionCount.value}");

        Assert.IsTrue(conditionExecutionCount.value > 0, "condition execution count is zero. Probably no classes in checked assemblies.");
    }

    private static IPredicate<Class> HaveMemberWithAttribute(string attributeFullName)
    {
        return new SimplePredicate<Class>(
            clazz => clazz.Members.AnyMatch(member => member.HasAttribute(attributeFullName)),
            $"have attribute with name {attributeFullName}");
    }

    private ICondition<Class> CountConditionExecutions(IntWrapper conditionWasExecutedAtLeastOnce)
    {
        // This condition is only executed for its side effect,
        // i.e. to count the executions of the condition.
        return new SimpleCondition<Class>(
            clazz =>
            {
                conditionWasExecutedAtLeastOnce.value++;
                return new ConditionResult(clazz, true);
            },
            "count condition executions");
    }

    private class IntWrapper
    {
        public int value;
    }
}
