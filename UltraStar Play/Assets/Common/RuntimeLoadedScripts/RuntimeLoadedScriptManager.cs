using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Flurl.Http.Configuration;
using UniInject;
using UniInject.Extensions;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class RuntimeLoadedScriptManager : AbstractSingletonBehaviour, INeedInjection
{
    public static RuntimeLoadedScriptManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<RuntimeLoadedScriptManager>();

    private const string RuntimeLoadedScriptsFolderName = "RuntimeLoadedScripts";

    [Inject]
    private Injector injector;

    [Inject]
    private Settings settings;

    private readonly Dictionary<IRuntimeLoadedScript,RuntimeLoadedScriptContext> scriptToContext = new();

    private static readonly IReadOnlyList<string> defaultExposedAssemblyNames = new List<string>()
    {
        "com.achimmihca.portaudioforunity",
        "com.achimmihca.primeinputactions",
        "com.achimmihca.protrans",
        "com.achimmihca.scenechangeanimations",
        "com.achimmihca.simplehttpserverforunity",
        "com.achimmihca.uniinject",
        "com.achimmihca.utfunknownunity",
        "UniRx",
        "Common",
        "playshared",
        "playsharedui",
        "Scenes",
        "System",
        "System.Collections",
        "System.Collections.ObjectModel",
        "System.Collections.Generic",
        "System.Linq",
        "UnityEngine",
        "UnityEngine.AudioModule",
        "UnityEngine.CoreModule",
        "UnityEngine.InputLegacyModule",
        "UnityEngine.InputModule",
        "UnityEngine.UI",
        "UnityEngine.UIElementsModule",
        "UnityEngine.UIModule",
        "UnityEngine.VideoModule",
    };

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        DirectoryUtils.CreateDirectory(GetAbsoluteDefaultRuntimeLoadedScriptsFolder());
        DirectoryUtils.CreateDirectory(GetAbsoluteUserDefinedRuntimeLoadedScriptsFolder());

        // CopyDefaultModToPersistentDataPath("DemoMod");

        if (settings.EnabledRuntimeLoadedMods.IsNullOrEmpty())
        {
            return;
        }

        List<string> modFolders = GetModFolders();
        foreach (string modFolder in modFolders)
        {
            Debug.Log($"Loading scripts from {modFolder}");
            if (IsModEnabled(modFolder))
            {
                InstantiateScripts(modFolder);
            }
        }
    }

    private void CopyDefaultModToPersistentDataPath(string modName)
    {
        try
        {
            string demoModSourceFolder = $"{GetAbsoluteDefaultRuntimeLoadedScriptsFolder()}/{modName}";
            string demoModTargetFolder = $"{GetAbsoluteUserDefinedRuntimeLoadedScriptsFolder()}/{modName}";
            if (Directory.Exists(demoModSourceFolder)
                && !Directory.Exists(demoModTargetFolder))
            {
                Debug.Log($"Copying default mod '{modName}' to persistentDataPath (from: '{demoModSourceFolder}', to: '{demoModTargetFolder}')");
                DirectoryUtils.CopyAll(demoModSourceFolder, demoModTargetFolder,
                    CopyDirectoryFilter.Exclude(path =>
                    {
                        string fileNameToLower = Path.GetFileName(path).ToLowerInvariant();
                        return fileNameToLower.EndsWith(".meta")
                            || fileNameToLower.EndsWith(".sln")
                            || fileNameToLower == "bin"
                            || fileNameToLower == "obj";
                    }));
            }
            else
            {
                Debug.Log($"Not copying default mod '{modName}' to persistentDataPath because the target folder already exists.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to copy default mod '{modName}' to persistentDataPath.");
        }
    }

    public bool IsModEnabled(string modFolder)
    {
        return settings.EnabledRuntimeLoadedMods.Contains(GetModName(modFolder));
    }

    public List<string> GetModFolders()
    {
        List<string> modParentFolders = new()
        {
            GetAbsoluteUserDefinedRuntimeLoadedScriptsFolder(),
        };

        return modParentFolders
            .SelectMany(modParentFolder => Directory.GetDirectories(modParentFolder))
            .ToList();
    }

    public List<T> GetCurrentRuntimeLoadedInstances<T>(string modFolder = null)
        where T : IRuntimeLoadedScript
    {
        return scriptToContext
            .Where(entry => modFolder == null
                || modFolder == entry.Value.ModFolder)
            .Where(entry => !entry.Value.IsInstanceObsolete
                            && entry.Key is T)
            .Select(entry => (T)entry.Key)
            .ToList();
    }

    private void InstantiateScripts(string modFolder)
    {
        List<string> exposedAssemblyNames = defaultExposedAssemblyNames.ToList();
        ModInfoJson modInfoJson = GetModInfo(modFolder);
        if (modInfoJson != null
            && !modInfoJson.requires.IsNullOrEmpty())
        {
            exposedAssemblyNames.AddRange(modInfoJson.requires);
        }

        CompilerWrapper compilerWrapper = CreateCompilerWrapper(modFolder, exposedAssemblyNames);
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

        // Create context for new instances
        foreach (IRuntimeLoadedScript runtimeLoadedScript in currentRuntimeLoadedScripts)
        {
            RuntimeLoadedScriptContext runtimeLoadedScriptContext = new(modFolder, false);
            scriptToContext[runtimeLoadedScript] = runtimeLoadedScriptContext;
        }

        // Load mod settings
        foreach (IRuntimeLoadedScript runtimeLoadedScript in currentRuntimeLoadedScripts)
        {
            if (runtimeLoadedScript is IModSettings modSettings)
            {
                LoadModSettings(modFolder, modSettings);
            }
        }

        // Inject instantiated objects
        ModContext modContext = new(modFolder);

        // Bind then inject runtime loaded scripts
        List<IAutoBoundRuntimeLoadedScript> autoBoundScripts = currentRuntimeLoadedScripts
            .OfType<IAutoBoundRuntimeLoadedScript>()
            .ToList();
        foreach (IRuntimeLoadedScript runtimeLoadedScript in currentRuntimeLoadedScripts)
        {
            RuntimeLoadedScriptContext runtimeLoadedScriptContext = scriptToContext[runtimeLoadedScript];
            Injector childInjector = injector
                .CreateChildInjector()
                .WithBindingForInstance(modContext)
                .WithBindingForInstance(runtimeLoadedScriptContext);
            foreach (IAutoBoundRuntimeLoadedScript autoBoundScript in autoBoundScripts)
            {
                ExistingInstanceProvider<object> modSettingsProvider = new(autoBoundScript);
                childInjector.AddBinding(new Binding(autoBoundScript.GetType(), modSettingsProvider));
            }

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

    private void LoadModSettings(string modFolder, IModSettings modSettings)
    {
        string modSettingsPath = GetModSettingsPath(modFolder);
        try
        {
            if (File.Exists(modSettingsPath))
            {
                Debug.Log($"Reading mod settings of type {modSettings.GetType()} from file '{modSettingsPath}'");
                string json = File.ReadAllText(modSettingsPath);
                JsonConverter.FillFromJsonCopy(json, modSettings, false);
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to load mod settings from '{modSettingsPath}'. Object type to deserialize: {modSettings.GetType().FullName}");
        }
    }

    private void SaveModSettings(string modFolder, IRuntimeLoadedScript modSettings)
    {
        if (modSettings == null)
        {
            return;
        }

        string modSettingsPath = GetModSettingsPath(modFolder);
        try
        {
            Debug.Log($"Writing mod settings of type {modSettings.GetType()} to file '{modSettingsPath}'");
            string json = JsonConverter.ToJson(modSettings);
            File.WriteAllText(modSettingsPath, json);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to save mod settings to '{modSettingsPath}'. Object type to serialize: {modSettings.GetType().FullName}");
        }
    }

    private CompilerWrapper CreateCompilerWrapper(string scriptsFolder, List<string> exposedAssemblyNames)
    {
        CompilerWrapper compilerWrapper = new();

        // Load AppDomain libraries
        LoadExposedAppDomainAssemblies(compilerWrapper, exposedAssemblyNames);

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

    private void LoadExposedAppDomainAssemblies(CompilerWrapper compilerWrapper, List<string> exposedAssemblyNames)
    {
        if (exposedAssemblyNames.IsNullOrEmpty())
        {
            return;
        }

        List<Assembly> exposedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => exposedAssemblyNames.Contains(assembly.GetName().Name))
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

    public static string GetAbsoluteDefaultRuntimeLoadedScriptsFolder()
    {
        return ApplicationUtils.GetStreamingAssetsPath(RuntimeLoadedScriptsFolderName);
    }

    public static string GetAbsoluteUserDefinedRuntimeLoadedScriptsFolder()
    {
        return ApplicationUtils.GetPersistentDataPath(RuntimeLoadedScriptsFolderName);
    }

    public static string GetModSettingsPath(string modFolder)
    {
        return $"{modFolder}/modsettings.json";
    }

    public static string GetModName(string modFolder)
    {
        ModInfoJson modInfoJson = GetModInfo(modFolder);
        if (modInfoJson != null
            && !modInfoJson.name.IsNullOrEmpty())
        {
            return modInfoJson.name;
        }
        return Path.GetFileName(modFolder);
    }

    public static ModInfoJson GetModInfo(string modFolder)
    {
        string modInfoPath = $"{modFolder}/modinfo.json";
        if (!FileUtils.Exists(modInfoPath))
        {
            return null;
        }

        try
        {
            string modInfoString = File.ReadAllText(modInfoPath);
            ModInfoJson modInfoJson = JsonConverter.FromJson<ModInfoJson>(modInfoString);
            return modInfoJson;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to load mod info from '{modInfoPath}': {ex.Message}");
            return null;
        }
    }

    protected override void OnDestroySingleton()
    {
        SaveAllModSettings();
    }

    private void SaveAllModSettings()
    {
        scriptToContext
            .Where(entry => !entry.Value.IsInstanceObsolete)
            .Select(entry => entry.Key)
            .OfType<IModSettings>()
            .ForEach(modSettings =>
            {
                if (scriptToContext.TryGetValue(modSettings, out RuntimeLoadedScriptContext context)
                    && !context.IsInstanceObsolete)
                {
                    SaveModSettings(context.ModFolder, modSettings);
                }
            });
    }
}
