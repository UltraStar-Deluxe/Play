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

    private const string RuntimeLoadedScriptsFolderName = "RuntimeLoadedScripts";

    [Inject]
    private Injector injector;

    [Inject]
    private Settings settings;

    private readonly Dictionary<IRuntimeLoadedScript,RuntimeLoadedScriptContext> scriptToContext = new();
    private readonly Dictionary<Type, string> typeToSourceFile = new();
    private readonly Dictionary<Type, string> typeToModFolder = new();
    private readonly Dictionary<string, ModContext> modFolderToModContext = new();

    private List<string> lastEnabledMods = new();

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

        lastEnabledMods = settings.EnabledMods.ToList();

        LoadAndInstantiateScripts();
    }

    private void Update()
    {
        if (!lastEnabledMods.SequenceEqual(settings.EnabledMods))
        {
            List<string> newlyEnabledModNames = settings.EnabledMods
                .Except(lastEnabledMods)
                .ToList();
            List<string> newlyDisabledModNames = lastEnabledMods
                .Except(settings.EnabledMods)
                .ToList();
            lastEnabledMods = settings.EnabledMods.ToList();
            if (!newlyDisabledModNames.IsNullOrEmpty())
            {
                OnDisableMods(newlyDisabledModNames);
            }

            if (!newlyEnabledModNames.IsNullOrEmpty())
            {
                OnEnableMods(newlyEnabledModNames);
            }
        }
    }

    private void OnEnableMods(List<string> newlyEnabledModNames)
    {
        Debug.Log($"Reloading mods because of newly enabled: {newlyEnabledModNames.ToCsv()}");
        LoadAndInstantiateScripts();
    }

    private void OnDisableMods(List<string> newlyDisabledModNames)
    {
        newlyDisabledModNames.ForEach(modName => OnDisableMod(modName));
    }

    private void OnDisableMod(string modName)
    {
        string modFolder = GetModFolderByModName(modName);
        List<IDisableModHandler> disableModHandlers = GetCurrentRuntimeLoadedInstances<IDisableModHandler>(modFolder);
        disableModHandlers.ForEach(disableModHandler =>
        {
            try
            {
                disableModHandler.OnDisableMod();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to call {disableModHandler.GetType()}.{nameof(disableModHandler.OnDisableMod)}");
            }
        });
    }

    private void LoadAndInstantiateScripts()
    {
        LoadScriptsIntoAppDomain();
        InstantiateScripts();
    }

    private void LoadScriptsIntoAppDomain()
    {
        if (settings.EnabledMods.IsNullOrEmpty())
        {
            return;
        }

        List<string> modFolders = GetModFolders();
        foreach (string modFolder in modFolders)
        {
            Debug.Log($"Loading scripts from {modFolder}");
            if (IsModEnabled(modFolder))
            {
                LoadScriptsIntoAppDomain(modFolder);
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
        return settings.EnabledMods.Contains(GetModName(modFolder));
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

    private string GetModFolder(IRuntimeLoadedScript script)
    {
        if (typeToModFolder.TryGetValue(script.GetType(), out string modFolder))
        {
            return modFolder;
        }

        return "";
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

    private void LoadScriptsIntoAppDomain(string modFolder)
    {
        using DisposableStopwatch d = new($"Loading mod '{GetModName(modFolder)}' into app domain took <ms>");

        List<string> exposedAssemblyNames = defaultExposedAssemblyNames.ToList();
        ModInfoJson modInfoJson = GetModInfo(modFolder);
        if (modInfoJson != null
            && !modInfoJson.requires.IsNullOrEmpty())
        {
            exposedAssemblyNames.AddRange(modInfoJson.requires);
        }

        CompilerWrapper compilerWrapper = new();

        // Load AppDomain libraries
        LoadExposedAppDomainAssemblies(compilerWrapper, exposedAssemblyNames);

        // Load libraries in folder
        string[] externalDllFiles = Directory.GetFiles(modFolder, "*.dll", SearchOption.AllDirectories);
        foreach (string dllFile in externalDllFiles)
        {
            Assembly assembly = Assembly.LoadFile(dllFile);
            compilerWrapper.ReferenceAssembly(assembly);
        }

        // Load C# files in folder
        string[] csFilePaths = Directory.GetFiles(modFolder, "*.cs");
        foreach (string filePath in csFilePaths)
        {
            LoadScriptFileIntoAppDomain(modFolder, filePath, compilerWrapper);
        }

        // Check compilation was successful
        string report = compilerWrapper.GetReport();
        if (compilerWrapper.ErrorsCount > 0)
        {
            Debug.LogError($"Failed to load scripts in '{modFolder}'. Compilation output:\n{report}");
        }
        else
        {
            Debug.Log($"Successfully loaded scripts in {modFolder}. Compilation output:\n{report}");
        }
    }

    private void LoadScriptFileIntoAppDomain(string modFolder, string filePath, CompilerWrapper compilerWrapper)
    {
        // Find the runtime loaded types that are instantiated by this file.
        List<Type> runtimeLoadedTypesBefore;
        try
        {
            runtimeLoadedTypesBefore = CompilerWrapper
                .CreateInstancesOf<IRuntimeLoadedScript>()
                .Select(it => it.GetType())
                .ToList();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to instantiate runtime loaded scripts: {ex.Message}");
            throw ex;
        }

        try
        {
            compilerWrapper.Execute(filePath);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to load '{filePath}': {ex.Message}");
            return;
        }

        List<Type> runtimeLoadedTypesAfter = new();
        try
        {
            runtimeLoadedTypesAfter = CompilerWrapper
                .CreateInstancesOf<IRuntimeLoadedScript>()
                .Select(it => it.GetType())
                .ToList();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to instantiate runtime loaded scripts: {ex.Message}");
            throw ex;
        }

        List<Type> newRuntimeLoadedTypes = runtimeLoadedTypesAfter.Except(runtimeLoadedTypesBefore).ToList();
        foreach (Type type in newRuntimeLoadedTypes)
        {
            typeToSourceFile[type] = filePath;
            typeToModFolder[type] = modFolder;
        }
    }

    private void InstantiateScripts()
    {
        using DisposableStopwatch d = new($"Instantiate runtime loaded scripts took <ms>");

        // Instantiate new objects
        List<IRuntimeLoadedScript> currentAndObsoleteRuntimeLoadedScripts = CompilerWrapper.CreateInstancesOf<IRuntimeLoadedScript>();

        List<IRuntimeLoadedScript> currentRuntimeLoadedScripts = GetNewestImplementations(currentAndObsoleteRuntimeLoadedScripts);

        // Mark context of old instances as obsolete
        scriptToContext
            .Where(entry => !currentRuntimeLoadedScripts.Contains(entry.Key))
            .ForEach(entry => entry.Value.SetObsolete());

        // Create context for new instances
        foreach (IRuntimeLoadedScript runtimeLoadedScript in currentRuntimeLoadedScripts)
        {
            string modFolder = GetModFolder(runtimeLoadedScript);
            RuntimeLoadedScriptContext runtimeLoadedScriptContext = new(modFolder, false);
            scriptToContext[runtimeLoadedScript] = runtimeLoadedScriptContext;
        }

        // Load mod settings
        foreach (IRuntimeLoadedScript runtimeLoadedScript in currentRuntimeLoadedScripts)
        {
            if (runtimeLoadedScript is IModSettings modSettings)
            {
                LoadModSettings(modSettings);
            }
        }

        // Bind then inject runtime loaded scripts
        List<IAutoBoundRuntimeLoadedScript> autoBoundScripts = currentRuntimeLoadedScripts
            .OfType<IAutoBoundRuntimeLoadedScript>()
            .ToList();
        foreach (IRuntimeLoadedScript runtimeLoadedScript in currentRuntimeLoadedScripts)
        {
            RuntimeLoadedScriptContext runtimeLoadedScriptContext = scriptToContext[runtimeLoadedScript];

            string modFolder = GetModFolder(runtimeLoadedScript);
            ModContext modContext = GetOrCreateModContext(modFolder);

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

    private ModContext GetOrCreateModContext(string modFolder)
    {
        if (!modFolderToModContext.TryGetValue(modFolder, out ModContext modContext))
        {
            modContext = new(modFolder);
            modFolderToModContext[modFolder] = modContext;
        }

        return modContext;
    }

    private void LoadModSettings(IModSettings modSettings)
    {
        string modFolder = GetModFolder(modSettings);
        if (modFolder.IsNullOrEmpty())
        {
            return;
        }

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

    private void SaveModSettings(IRuntimeLoadedScript modSettings)
    {
        string modFolder = GetModFolder(modSettings);
        if (modFolder.IsNullOrEmpty())
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

    public string GetModFolderByModName(string modName)
    {
        return typeToModFolder
            .Values
            .Distinct()
            .FirstOrDefault(modFolder => GetModName(modFolder) == modName);
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
            .ForEach(modSettings => SaveModSettings(modSettings));
    }
}
