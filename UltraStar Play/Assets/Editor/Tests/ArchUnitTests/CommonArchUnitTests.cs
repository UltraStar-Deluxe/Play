using System;
using System.Collections.Generic;
using System.Linq;
using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Extensions;
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

    private static IPredicate<Class> HaveMemberWithAttribute(string attributeFullName)
    {
        return new SimplePredicate<Class>(
            clazz => clazz.Members.AnyMatch(member => member.HasAttribute(attributeFullName)),
            $"have attribute with name {attributeFullName}");
    }
}
