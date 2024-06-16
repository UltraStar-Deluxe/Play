using System;
using System.Collections.Generic;
using System.Linq;
using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using UnityEngine;
using Assembly = System.Reflection.Assembly;

public static class ArchUnitTestUtils
{
    public static readonly List<string> gameSpecificRuntimeAssemblyNames = new List<String>()
    {
        "playshared",
        "playsharedui",
        "Common",
        "Scenes",
        "SongEditorScene",
    };

    public static Architecture LoadArchitectureByAssemblyNames(List<string> assemblyNames)
    {
        List<Assembly> assemblies = assemblyNames
            .Select(name => Assembly.Load(name))
            .ToList();

        // Log assemblies that are used in the tests
        string assemblyCsv = assemblies
            .Select(assembly => assembly.GetName().Name)
            .JoinWith(", ");
        Debug.Log($"Loading {assemblies.Count} assembly as architecture for ArchUnit tests: {assemblyCsv}");

        // Create ArchUnit architecture object from assemblies
        Architecture architecture = new ArchLoader()
            .LoadAssemblies(assemblies.ToArray())
            .Build();
        return architecture;
    }
}
