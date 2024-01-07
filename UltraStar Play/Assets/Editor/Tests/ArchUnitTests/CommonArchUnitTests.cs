using System;
using System.Collections.Generic;
using System.Linq;
using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Extensions;
using ArchUnitNET.Fluent.Conditions;
using ArchUnitNET.Fluent.Predicates;
using ArchUnitNET.Loader;
using ArchUnitNET.NUnit;
using NUnit.Framework;
using UnityEngine;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using Assembly = System.Reflection.Assembly;

public class CommonArchUnitTests
{
    private readonly List<string> gameSpecificRuntimeAssemblyNames = new List<String>()
    {
        "playshared",
        "playsharedui",
        "Common",
        "Scenes",
        "SongEditorScene",
    };

    public Architecture LoadArchitectureByAssemblyNames(List<string> assemblyNames)
    {
        List<Assembly> assemblies = assemblyNames
            .Select(name => Assembly.Load(name))
            .ToList();

        // Log assemblies that are used in the tests
        string assemblyCsv = assemblies
            .Select(assembly => assembly.GetName().Name)
            .ToCsv();
        Debug.Log($"Loading {assemblies.Count} assembly as architecture for ArchUnit tests: {assemblyCsv}");

        // Create ArchUnit architecture object from assemblies
        Architecture architecture = new ArchLoader()
            .LoadAssemblies(assemblies.ToArray())
            .Build();
        return architecture;
    }

    [Test]
    public void ClassesThatUseInjectAttributeShouldImplementINeedInjection()
    {
        Architecture architecture = LoadArchitectureByAssemblyNames(gameSpecificRuntimeAssemblyNames);

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
        Architecture architecture = LoadArchitectureByAssemblyNames(gameSpecificRuntimeAssemblyNames);

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
