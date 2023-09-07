using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UniInject;
using UniInject.Extensions;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class RuntimeLoadedScriptManager : AbstractSingletonBehaviour, INeedInjection
{
    public static RuntimeLoadedScriptManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<RuntimeLoadedScriptManager>();

    [Inject]
    private Injector injector;

    [Inject]
    private Settings settings;

    private readonly Dictionary<IRuntimeLoadedScript,RuntimeLoadedScriptContext> scriptToContext = new();

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        if (settings.EnabledRuntimeLoadedMods.IsNullOrEmpty())
        {
            return;
        }

        List<string> modParentFolders = new()
        {
            ApplicationUtils.GetStreamingAssetsPath("RuntimeLoadedScripts"),
            ApplicationUtils.GetPersistentDataPath("RuntimeLoadedScripts"),
        };

        foreach (string modParentFolder in modParentFolders)
        {
            string[] modFolders = Directory.GetDirectories(modParentFolder);
            foreach (string modFolder in modFolders)
            {
                Debug.Log($"Loading scripts from {modFolder}");
                if (settings.EnabledRuntimeLoadedMods.Contains(modFolder))
                {
                    InstantiateScripts(modFolder);
                }
            }
        }
    }

    public List<T> GetCurrentRuntimeLoadedInstances<T>()
        where T : IRuntimeLoadedScript
    {
        return scriptToContext
            .Where(entry => !entry.Value.IsInstanceObsolete
                            && entry.Key is T)
            .Select(entry => (T)entry.Key)
            .ToList();
    }

    private void InstantiateScripts(string scriptsFolder)
    {
        CompilerWrapper compilerWrapper = CreateCompilerWrapper(scriptsFolder);
        if (compilerWrapper == null)
        {
            return;
        }

        // Instantiate new objects
        List<IRuntimeLoadedScript> currentAndObsoleteRuntimeLoadedScripts = compilerWrapper
            .CreateInstancesOf<IRuntimeLoadedScript>()
            .ToList();

        List<IRuntimeLoadedScript> currentRuntimeLoadedScripts = GetNewestImplementations(currentAndObsoleteRuntimeLoadedScripts);

        // Mark context of old instances as obsolete
        scriptToContext
            .Where(entry => !currentRuntimeLoadedScripts.Contains(entry.Key))
            .ForEach(entry => entry.Value.SetObsolete());

        // Inject instantiated objects
        foreach (IRuntimeLoadedScript runtimeLoadedScript in currentRuntimeLoadedScripts)
        {
            RuntimeLoadedScriptContext runtimeLoadedScriptContext = new(scriptsFolder, false);
            Injector childInjector = injector
                .CreateChildInjector()
                .WithBindingForInstance(runtimeLoadedScriptContext);
            scriptToContext[runtimeLoadedScript] = runtimeLoadedScriptContext;

            childInjector.Inject(runtimeLoadedScript);
        }

        // Run runnables
        foreach (IRuntimeLoadedScript runtimeLoadedScript in currentRuntimeLoadedScripts)
        {
            if (runtimeLoadedScript is IRuntimeLoadedRunnable runtimeLoadedRunnable)
            {
                runtimeLoadedRunnable.Run();
            }
        }
    }

    private CompilerWrapper CreateCompilerWrapper(string scriptsFolder)
    {
        CompilerWrapper compilerWrapper = new();

        // Load AppDomain libraries
        LoadExposedAppDomainAssemblies(compilerWrapper);

        // Load libraries in folder
        string[] externalDllFiles = Directory.GetFiles(scriptsFolder, "*.dll", SearchOption.AllDirectories);
        foreach (string dllFile in externalDllFiles)
        {
            Assembly assembly = Assembly.LoadFile(dllFile);
            compilerWrapper.ReferenceAssembly(assembly);
        }

        // Load C# files in folder
        string[] csFilePaths = Directory.GetFiles(scriptsFolder, "*.cs");
        foreach (string filePath in csFilePaths)
        {
            try
            {
                compilerWrapper.Execute(filePath);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to load '{filePath}': {ex.Message}");
                return null;
            }
        }

        // Check compilation was successful
        string report = compilerWrapper.GetReport();
        if (compilerWrapper.ErrorsCount > 0)
        {
            Debug.LogError($"Failed to load scripts in '{scriptsFolder}'. Compilation output:\n{report}");
            return null;
        }
        else
        {
            Debug.Log($"Successfully loaded scripts in {scriptsFolder}. Compilation output:\n{report}");
        }

        return compilerWrapper;
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
        return settings.RuntimeLoadedScriptExposedAssemblyNames.Contains(assembly.GetName().Name);
    }
}
