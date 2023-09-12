using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using IngameDebugConsole;
using UniInject;
using UniInject.Extensions;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class RuntimeLoadedScriptManager : AbstractSingletonBehaviour, INeedInjection
{
    public static RuntimeLoadedScriptManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<RuntimeLoadedScriptManager>();

    private const string RuntimeLoadedScriptsFolderName = "RuntimeLoadedScripts";
    private const string TemplateModName = "TemplateMod";
    private const string TemplateModNamePlaceholder = "MODNAME";
    private const string TemplateModDllFolderPlaceholder = "DEFAULT_DLL_FOLDER";

    [Inject]
    private Injector injector;

    [Inject]
    private Settings settings;

    private readonly Dictionary<IRuntimeLoadedScript,RuntimeLoadedScriptContext> scriptToContext = new();
    private readonly Dictionary<Type, string> typeToModFolder = new();
    private readonly Dictionary<string, ModContext> modFolderToModContext = new();

    private List<string> lastEnabledMods = new();

    private bool appDomainTypesChanged = true;
    private List<Type> runtimeLoadedScriptImplementations = new();
    private List<Type> RuntimeLoadedScriptImplementations
    {
        get
        {
            if (appDomainTypesChanged)
            {
                appDomainTypesChanged = false;
                runtimeLoadedScriptImplementations = GetRuntimeLoadedScriptImplementations(true);
            }

            return runtimeLoadedScriptImplementations;
        }
    }

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

        AddDebugLogConsoleCommand();

        lastEnabledMods = settings.EnabledMods.ToList();

        LoadAndInstantiateScripts();
    }

    private void Update()
    {
        UpdateEnabledMods();
    }

    private void UpdateEnabledMods()
    {
        if (lastEnabledMods.SequenceEqual(settings.EnabledMods))
        {
            return;
        }

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

    private void AddDebugLogConsoleCommand()
    {
        DebugLogConsole.AddCommand("mod.path", "Copy and log path to folders with runtime loaded scripts",
            () =>
            {
                string text = GetAbsoluteUserDefinedRuntimeLoadedScriptsFolder();
                ClipboardUtils.CopyToClipboard(text);
                Debug.Log($"Mods folder: {text}");
            });

        DebugLogConsole.AddCommand("mod.assemblies", "Show all assemblies in current app domain",
            () =>
            {
                string text = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(assembly => assembly.GetName().Name)
                    .ToCsv();
                ClipboardUtils.CopyToClipboard(text);
                Debug.Log($"Copy and log assemblies in app domain: {text}");
            });

        DebugLogConsole.AddCommand("mod.assemblies.exposed", "Copy and log all assemblies in current app domain that are exposed to mods by default",
            () =>
            {
                string text = defaultExposedAssemblyNames.ToCsv();
                ClipboardUtils.CopyToClipboard(text);
                Debug.Log($"Assemblies exposed to mods by default: {text}");
            });

        DebugLogConsole.AddCommand("mod.create", "Create a new mod with the given name from template",
            (string modName) => CreateModFromTemplate(modName),
            "name");

        DebugLogConsole.AddCommand("mod.interfaces", "Copy and log all IRuntimeLoadedScript subtypes that can be implemented in a mod.",
            () =>
            {
                List<Type> runtimeLoadedScriptSubtypes = GetRuntimeLoadedScriptInterfaces();
                string text = runtimeLoadedScriptSubtypes.Select(type => type.Name).ToCsv(", ", "", "");
                ClipboardUtils.CopyToClipboard(text);
                Debug.Log($"Subtypes of IRuntimeLoadedScript: {text}");
            });
    }

    private List<Type> GetRuntimeLoadedScriptInterfaces()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(domainAssembly => domainAssembly.GetTypes())
            .Where(type => type.IsInterface
                           && typeof(IRuntimeLoadedScript).IsAssignableFrom(type))
            .ToList();
    }

    private void CreateModFromTemplate(string modName)
    {
        string targetModFolder = $"{GetAbsoluteUserDefinedRuntimeLoadedScriptsFolder()}/{modName}";
        DirectoryInfo targetModFolderInfo = new(targetModFolder);
        if (targetModFolderInfo.Exists)
        {
            Debug.Log($"Directory already exists: '{targetModFolder}'");
            UiManager.CreateNotification("A mod with this name already exists.");
            return;
        }

        string templateModFolder = ApplicationUtils.GetStreamingAssetsPath($"{RuntimeLoadedScriptsFolderName}/{TemplateModName}");
        if (!Directory.Exists(templateModFolder))
        {
            throw new Exception($"Template mod folder not found: '{templateModFolder}'");
        }

        DirectoryUtils.CopyAll(templateModFolder, targetModFolder,
            CopyDirectoryFilter.Exclude(path =>
            {
                string fileNameToLower = Path.GetFileName(path).ToLowerInvariant();
                return fileNameToLower.EndsWith(".meta")
                       || fileNameToLower.EndsWith(".sln")
                       || fileNameToLower == "bin"
                       || fileNameToLower == "obj";
            }));

        // Replace placeholders in created files
        List<string> textFileExtensions = new() { "txt", "json", "xml", "csproj", "cs", };
        foreach (FileInfo fileInfo in targetModFolderInfo.GetFiles())
        {
            try
            {
                if (textFileExtensions.Contains(fileInfo.Extension.TrimStart('.')))
                {
                    string fileContentWithPlaceholders = File.ReadAllText(fileInfo.FullName);
                    string fileContentNoPlaceholders = ReplaceTemplateModPlaceholders(fileContentWithPlaceholders, modName);
                    File.WriteAllText(fileInfo.FullName, fileContentNoPlaceholders);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to replace template mod placeholders in file content {fileInfo}");
            }
        }

        // Replace mod name placeholder in file name
        foreach (FileInfo fileInfo in targetModFolderInfo.GetFiles())
        {
            try
            {
                string fileNameNoPlaceholders = ReplaceTemplateModPlaceholders(fileInfo.Name, modName);
                if (fileNameNoPlaceholders != fileInfo.Name)
                {
                    File.Move(fileInfo.FullName, $"{fileInfo.DirectoryName}/{fileNameNoPlaceholders}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to replace template mod placeholders in file name {fileInfo}");
            }
        }
    }

    private string ReplaceTemplateModPlaceholders(string text, string modName)
    {
        string dataPath = Application.isEditor
            ? new DirectoryInfo(Application.dataPath + "/../../Build/Windows/Melody Mania_Data").FullName
            : Application.dataPath;
        return text
            .Replace(TemplateModNamePlaceholder, modName)
            .Replace(TemplateModDllFolderPlaceholder, $"{dataPath}/Managed");
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
        List<IOnDisableMod> disableModHandlers = GetCurrentRuntimeLoadedInstances<IOnDisableMod>(modFolder);
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
        try
        {
            LoadScriptsIntoAppDomain();
            InstantiateScripts();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to load mods: {ex.Message}");
            UiManager.CreateNotification($"Failed to load mods. Check log for details.\n" +
                                         $"Try to disable mods and restart the app.");
        }
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
            if (IsModEnabled(modFolder))
            {
                Debug.Log($"Loading scripts from {modFolder}");
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

    private bool IsModEnabled(Type type)
    {
        if (!typeToModFolder.TryGetValue(type, out string modFolder))
        {
            return false;
        }

        return IsModEnabled(modFolder);
    }

    private bool IsModEnabled(IRuntimeLoadedScript runtimeLoadedScript)
    {
        return IsModEnabled(runtimeLoadedScript.GetType());
    }

    public List<string> GetModNames()
    {
        return GetModFolders()
            .Select(modFolder => GetModName(modFolder))
            .ToList();
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
            .Where(entry => IsModEnabled(entry.Key))
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
        appDomainTypesChanged = true;

        // Find types that are loaded from this mod folder
        List<Type> typesBefore = GetRuntimeLoadedScriptImplementations(false);

        // Load libraries in folder
        string[] externalDllFiles = Directory.GetFiles(modFolder, "*.dll", SearchOption.AllDirectories);
        foreach (string dllFile in externalDllFiles)
        {
            Assembly assembly = Assembly.LoadFile(dllFile);
            compilerWrapper.ReferenceAssembly(assembly);
            appDomainTypesChanged = true;
        }

        // Load C# files in folder
        string[] csFilePaths = Directory.GetFiles(modFolder, "*.cs");
        foreach (string filePath in csFilePaths)
        {
            LoadScriptFileIntoAppDomain(filePath, compilerWrapper);
        }

        // Find types that are loaded from this mod folder
        List<Type> typesAfter = GetRuntimeLoadedScriptImplementations(false);
        foreach (Type type in typesAfter.Except(typesBefore))
        {
            typeToModFolder[type] = modFolder;
        }

        // Check compilation was successful
        string report = compilerWrapper.GetReport();
        if (compilerWrapper.ErrorsCount > 0)
        {
            Debug.LogError($"Failed to load scripts of '{GetModName(modFolder)}'. Compilation output:\n{report}");
        }
        else
        {
            Debug.Log($"Successfully loaded scripts of '{GetModName(modFolder)}'. Compilation output:\n{report}");
        }
    }

    private void LoadScriptFileIntoAppDomain(string filePath, CompilerWrapper compilerWrapper)
    {
        // Find the types that are implemented by this file.
        try
        {
            compilerWrapper.Execute(filePath);
            appDomainTypesChanged = true;
        }
        catch (Exception ex)
        {
            throw new LoadModException($"Failed to load '{filePath}': {ex.Message}", ex);
        }
    }

    private List<IRuntimeLoadedScript> CreateInstancesOfRuntimeLoadedScripts()
    {
        Type parent = typeof(IRuntimeLoadedScript);
        return RuntimeLoadedScriptImplementations
            .Where(type => parent.IsAssignableFrom(type))
            .Select(type => (IRuntimeLoadedScript)Activator.CreateInstance(type))
            .ToList();
    }

    private List<Type> GetRuntimeLoadedScriptImplementations(bool logExceptions)
    {
        Type parent = typeof(IRuntimeLoadedScript);
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        List<ReflectionTypeLoadException> exceptions = new();

        List<Type> types = assemblies.SelectMany(assembly =>
        {
            Type[] typesOfAssembly;
            try
            {
                typesOfAssembly = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                exceptions.Add(ex);

                // Careful: types that could not be loaded are null in the array.
                typesOfAssembly = ex.Types;
            }

            return typesOfAssembly.Where(type => type != null
                                                 && !type.IsAbstract
                                                 && !type.IsInterface
                                                 && parent.IsAssignableFrom(type));
        }).ToList();

        if (logExceptions)
        {
            // Only log duplicate messages once.
            HashSet<string> loggedErrorMessages = new();
            exceptions
                .Distinct()
                .ForEach(ex =>
                {
                    if (loggedErrorMessages.Contains(ex.Message))
                    {
                        return;
                    }
                    Debug.LogException(ex);
                    loggedErrorMessages.Add(ex.Message);
                });
        }

        return types;
    }

    private void InstantiateScripts()
    {
        using DisposableStopwatch d = new($"Instantiate runtime loaded scripts took <ms>");

        // Instantiate new objects
        List<IRuntimeLoadedScript> currentAndObsoleteRuntimeLoadedScripts;
        try
        {
            currentAndObsoleteRuntimeLoadedScripts = CreateInstancesOfRuntimeLoadedScripts();
        }
        catch (Exception ex)
        {
            throw new LoadModException($"Failed to instantiate runtime loaded scripts: {ex.Message}", ex);
        }

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
                try
                {
                    LoadModSettings(modSettings);
                }
                catch (LoadModSettingsException ex)
                {
                    Debug.LogException(ex);
                    UiManager.CreateNotification($"Failed to load settings of mod '{GetModName(ex.ModFolder)}'");
                }
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
            throw new LoadModSettingsException($"Failed to load mod settings from '{modSettingsPath}'. Object type to deserialize: {modSettings.GetType().FullName}", ex)
            {
                ModSettingsPath = modSettingsPath,
                ModFolder = modFolder,
            };
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

        // string exposedAssemblyNameCsv = exposedAssemblies
        //     .Select(it => it.GetName().Name)
        //     .OrderBy(it => it)
        //     .ToCsv("\n");
        // Debug.Log($"Exposed assemblies: {exposedAssemblyNameCsv}");

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
