using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UniInject;
using UniInject.Extensions;
using UnityEngine;

public class RuntimeLoadedScriptDemo : MonoBehaviour, INeedInjection
{
    [Inject]
    private Injector injector;

    [InjectedInInspector]
    public string runtimeLoadedScriptsFolder;

    [InjectedInInspector]
    public readonly List<string> exposedAssemblyNamePatterns = new()
    {
        "System.*",
        "UnityEngine.*",
        "com.achimmihca.*",
        "playshared",
        "playsharedui",
        "Common",
        "Scenes",
    };

    private void Start()
    {
        CompilerWrapper compilerWrapper = CreateCompilerWrapper();

        // load text files and run them
        string[] csFilePaths = Directory.GetFiles(runtimeLoadedScriptsFolder, "*.cs");
        foreach (string filePath in csFilePaths)
        {
            if (IsIgnoredCsFile(filePath))
            {
                continue;
            }

            compilerWrapper.Execute(filePath);

            string fileName = Path.GetFileName(filePath);
            string report = compilerWrapper.GetReport();
            if (!report.IsNullOrEmpty())
            {
                string logMessage = $"{fileName} {report}";
                if (compilerWrapper.ErrorsCount > 0)
                {
                    Debug.LogError($"Error in '{fileName}'. Compilation output:\n{logMessage}");
                }
                else
                {
                    Debug.Log($"Successfully compiled file '{fileName}'. Compilation output:\n{logMessage}");
                }
            }
        }

        // Create instances. This includes built-ins as well as loaded ones.
        if (compilerWrapper.ErrorsCount > 0)
        {
            return;
        }

        List<IRuntimeLoadedRunnable> runtimeLoadedRunnables = compilerWrapper
            .CreateInstancesOf<IRuntimeLoadedRunnable>()
            .ToList();

        RuntimeLoadedScriptContext runtimeLoadedScriptContext = new(runtimeLoadedScriptsFolder);

        Injector childInjector = injector
            .CreateChildInjector()
            .WithBindingForInstance(runtimeLoadedScriptContext);
        foreach (IRuntimeLoadedRunnable runtimeLoadedRunnable in GetNewestImplementations(runtimeLoadedRunnables))
        {
            childInjector.Inject(runtimeLoadedRunnable);
            runtimeLoadedRunnable.Run();
        }
    }

    private bool IsIgnoredCsFile(string filePath)
    {
        return false;
    }

    private CompilerWrapper CreateCompilerWrapper()
    {
        CompilerWrapper result = new();

        LoadExposedAppDomainAssemblies(result);

        // External DLL files
        string[] externalDllFiles = Directory.GetFiles(runtimeLoadedScriptsFolder, "*.dll", SearchOption.AllDirectories);
        foreach (string dllFile in externalDllFiles)
        {
            Assembly assembly = Assembly.LoadFile(dllFile);
            result.ReferenceAssembly(assembly);
        }

        return result;
    }

    private bool IsIgnoredDllFile(string dllFile)
    {
        string fileName = Path.GetFileName(dllFile);
        if (fileName.Contains("UnityEngine", StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        HashSet<string> ignoredDllFileNames = new()
        {
            "playshared",
            "playsharedui",
            "Common",
            "Scenes",
        };
        return ignoredDllFileNames.Contains(fileName);
    }

    private static List<T> GetNewestImplementations<T>(List<T> instances)
    {
        // Multiple implementations of the same class can be loaded, e.g. during development.
        // These implementations cannot be unloaded without unloading the whole AppDomain.
        // Thus, if there are multiple instances with the same class name then we only take the last implementation, which is the most up-to-date version.
        return instances.GroupBy(highscoreProvider => highscoreProvider.GetType().Name)
            .Select(group => group.Last())
            .ToList();
    }

    private void LoadExposedAppDomainAssemblies(CompilerWrapper compilerWrapper)
    {
        List<Assembly> exposedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => IsExposedAssembly(assembly))
            .ToList();
        string exposedAssemblyNameCsv = exposedAssemblies
            .Select(it => it.GetName().Name)
            .OrderBy(it => it)
            .ToCsv("\n");
        Debug.Log($"Exposed assemblies: {exposedAssemblyNameCsv}");
        foreach (Assembly assembly in exposedAssemblies)
        {
            compilerWrapper.ReferenceAssembly(assembly);
        }
    }

    private bool IsExposedAssembly(Assembly assembly)
    {
        return exposedAssemblyNamePatterns.AnyMatch(pattern =>
            Regex.IsMatch(assembly.GetName().Name, pattern));
    }
}
