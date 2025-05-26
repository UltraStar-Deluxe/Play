using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using IngameDebugConsole;
using UniInject;
using UniInject.Extensions;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ModManager : AbstractSingletonBehaviour, INeedInjection
{
    public static ModManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<ModManager>();

    public const string ModInfoFileName = "modinfo.yml";
    private const string ModsPersistentDataFolderName = "ModsPersistentData";
    private const string TemplateModNamePlaceholder = "MODNAME";
    private const string TemplateModDllFolderPlaceholder = "DEFAULT_DLL_FOLDER";
    private static readonly ModName templateModName = new ModName("TemplateMod");

    [Inject]
    private Injector injector;

    [Inject]
    private Settings settings;

    private readonly Dictionary<IMod, ModObjectContext> modObjectToContext = new();
    private readonly Dictionary<Type, ModFolder> typeToModFolder = new();

    private List<ModName> lastEnabledMods = new();

    private bool appDomainTypesChanged = true;
    private List<Type> modTypes = new();
    private List<Type> ModTypes
    {
        get
        {
            if (appDomainTypesChanged)
            {
                appDomainTypesChanged = false;
                modTypes = GetModTypes();
            }

            return modTypes;
        }
    }

    private readonly Dictionary<ModFolder, FileSystemWatcher> modFolderToFileSystemWatcher = new();
    private readonly List<string> changedCsFiles = new();

    private readonly HashSet<ModFolder> failedToLoadModFolders = new();
    public IReadOnlyCollection<ModFolder> FailedToLoadModFolders => failedToLoadModFolders;
    public IReadOnlyCollection<ModFolder> EnabledFailedToLoadModFolders => FailedToLoadModFolders
        .Where(modFolder => IsModEnabled(modFolder))
        .ToList();

    private static readonly IReadOnlyList<string> defaultExposedAssemblyNames = new List<string>()
    {
        // Unity Engine
        "UnityEngine",
        "UnityEngine.AudioModule",
        "UnityEngine.CoreModule",
        "UnityEngine.InputLegacyModule",
        "UnityEngine.InputModule",
        "UnityEngine.UI",
        "UnityEngine.UIElementsModule",
        "UnityEngine.UIModule",
        "UnityEngine.VideoModule",

        "Unity.InputSystem",

        // From Unity Packages
        "com.achimmihca.uniinject",
        "playshared",
        "playsharedui",

        // From Plugins folder
        "UniRx",
        "Plugins",

        // Project Libraries
        "Common",
        "Scenes",
    };

    private CompilerWrapper evalCompilerWrapper;
    private CompilerWrapper EvalCompilerWrapper
    {
        get
        {
            if (evalCompilerWrapper == null)
            {
                evalCompilerWrapper = new();
                LoadExposedAppDomainAssembliesAndTypes(evalCompilerWrapper,
                    AppDomain.CurrentDomain.GetAssemblies(),
                    defaultExposedAssemblyNames,
                    new List<string>());
            }

            return evalCompilerWrapper;
        }
    }

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        DirectoryUtils.CreateDirectory(ModFolderUtils.GetDefaultModsRootFolderAbsolutePath());
        DirectoryUtils.CreateDirectory(ModFolderUtils.GetUserDefinedModsRootFolderAbsolutePath());

        // CopyDefaultModToPersistentDataPath("DemoMod");

        AddDebugLogConsoleCommand();

        CreateOrUpdateModFolderFileSystemWatchers();

        lastEnabledMods = GetEnabledMods();

        LoadAndInstantiateMods();
    }

    private void CreateOrUpdateModFolderFileSystemWatchers()
    {
        foreach (ModFolder modFolder in GetModFolders())
        {
            CreateOrUpdateModFolderFileSystemWatcher(modFolder);
        }
    }

    private void CreateOrUpdateModFolderFileSystemWatcher(ModFolder modFolder)
    {
        if (modFolderToFileSystemWatcher.ContainsKey(modFolder))
        {
            return;
        }

        FileSystemWatcher fileSystemWatcher = FileSystemWatcherFactory.CreateFileSystemWatcher(modFolder.Value,
            new FileSystemWatcherConfig("ModFolderWatcher", "*.cs")
            {
                IncludeSubdirectories = true,
            },
            (sender, args) => OnCsFileChanged(modFolder, args.FullPath));
        modFolderToFileSystemWatcher[modFolder] = fileSystemWatcher;
    }

    private void OnCsFileChanged(ModFolder modFolder, string filePath)
    {
        if (!settings.ReloadModsOnFileChange)
        {
            return;
        }

        changedCsFiles.Add(filePath);
    }

    private void Update()
    {
        UpdateEnabledMods();

        if (!changedCsFiles.IsNullOrEmpty())
        {
            Debug.Log($"Reloading mods because of changed files: {changedCsFiles.JoinWith(", ")}");
            changedCsFiles.Clear();
            ReloadMods();
        }
    }

    public void ReloadMods()
    {
        // Notify mods about reload
        foreach (IOnReloadMod modObject in GetModObjects<IOnReloadMod>())
        {
            try
            {
                modObject.OnReloadMod();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to call OnReloadMod on mod object '{modObject}' of type {modObject.GetType()}");
            }
        }

        LoadAndInstantiateMods();
    }

    private void UpdateEnabledMods()
    {
        List<ModName> enabledMods = GetEnabledMods();
        if (lastEnabledMods.SequenceEqual(enabledMods))
        {
            return;
        }

        List<ModName> newlyEnabledModNames = enabledMods
            .Except(lastEnabledMods)
            .ToList();
        List<ModName> newlyDisabledModNames = lastEnabledMods
            .Except(enabledMods)
            .ToList();
        lastEnabledMods = enabledMods;
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
        DebugLogConsole.AddCommand("mod.path", "Copy and log path to folder with mods",
            () =>
            {
                string text = ModFolderUtils.GetUserDefinedModsRootFolderAbsolutePath();
                ClipboardUtils.CopyToClipboard(text);
                Debug.Log($"Mods folder: {text}");
            });

        DebugLogConsole.AddCommand("mod.assemblies", "Show all assemblies in current app domain",
            () =>
            {
                string text = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(assembly => assembly.GetName().Name)
                    .JoinWith(", ");
                ClipboardUtils.CopyToClipboard(text);
                Debug.Log($"Copy and log assemblies in app domain: {text}");
            });

        DebugLogConsole.AddCommand("mod.assemblies.exposed", "Copy and log all assemblies in current app domain that are exposed to mods by default",
            () =>
            {
                string text = defaultExposedAssemblyNames.JoinWith(", ");
                ClipboardUtils.CopyToClipboard(text);
                Debug.Log($"Assemblies exposed to mods by default: {text}");
            });

        DebugLogConsole.AddCommand("mod.create", "Create a new mod with the given name from template",
            (string modName) =>
            {
                ModFolder newModFolder = CreateModFolderFromTemplate(new ModName(modName));
                Debug.Log($"Created new mod folder '{newModFolder}'");
                ApplicationUtils.OpenDirectory(newModFolder.Value);
            },
            "name");

        DebugLogConsole.AddCommand("mod.interfaces", "Copy and log all mod interfaces.",
            () =>
            {
                List<Type> modInterfaceTypes = GetModInterfaces();
                string text = modInterfaceTypes.Select(type => type.Name).JoinWith(", ");
                ClipboardUtils.CopyToClipboard(text);
                Debug.Log($"Mod interfaces: {text}");
            });

        DebugLogConsole.AddCommand("mod.reloadOnChange", "Toggle auto reload of mods when a .cs file in a mod folder changes.",
            () =>
            {
                settings.ReloadModsOnFileChange = !settings.ReloadModsOnFileChange;
                Debug.Log($"Reload changed mods: {settings.ReloadModsOnFileChange}");
            });

        DebugLogConsole.AddCommand("mod.eval", "Evaluate C# code in the current context. " +
                                               "Previous statements such as using statements become part of the context. " +
                                               "Surround the expression with braces to allow spaces in the expression.",
            (string expression) => EvaluateExpression(expression),
            "name");
    }

    private void EvaluateExpression(string expression)
    {
        Debug.Log($"> {expression}");
        EvalCompilerWrapper.EvaluateExpression(expression, out object result, out bool isResultSet);
        if (isResultSet)
        {
            Debug.Log(result);
        }
    }

    private List<Type> GetModInterfaces()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(domainAssembly => domainAssembly.GetTypes())
            .Where(type => type.IsInterface
                           && typeof(IMod).IsAssignableFrom(type))
            .ToList();
    }

    public ModFolder CreateModFolderFromTemplate(ModName modName)
    {
        string targetModFolder = $"{ModFolderUtils.GetUserDefinedModsRootFolderAbsolutePath()}/{modName}";
        DirectoryInfo targetModFolderInfo = new(targetModFolder);
        if (targetModFolderInfo.Exists)
        {
            Debug.Log($"Directory already exists: '{targetModFolder}'");
            NotificationManager.CreateNotification(Translation.Get(R.Messages.mod_error_nameConflict));
            return null;
        }

        string templateModFolder = ApplicationUtils.GetStreamingAssetsPath($"{ModFolderUtils.ModsRootFolderName}/{templateModName}");
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
        List<string> textFileExtensions = new() { "txt", "json", "yml", "xml", "csproj", "cs", };
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

        return new ModFolder(targetModFolder);
    }

    private string ReplaceTemplateModPlaceholders(string text, ModName modName)
    {
        string modFolderNameNoSpaces = modName.Value.Replace(" ", "");
        string dataPath = Application.isEditor
            ? new DirectoryInfo(Application.dataPath + "/../../Build/Windows/UltraStar Play_Data").FullName
            : Application.dataPath;
        return text
            .Replace(TemplateModNamePlaceholder, modFolderNameNoSpaces)
            .Replace(TemplateModDllFolderPlaceholder, $"{dataPath}/Managed");
    }

    private void OnEnableMods(List<ModName> newlyEnabledModNames)
    {
        Debug.Log($"Reloading mods because of newly enabled mod: {newlyEnabledModNames.JoinWith(", ")}");
        CreateOrUpdateModFolderFileSystemWatchers();
        LoadAndInstantiateMods();
    }

    private void OnDisableMods(List<ModName> newlyDisabledModNames)
    {
        newlyDisabledModNames.ForEach(modName => OnDisableMod(modName));
    }

    private void OnDisableMod(ModName modName)
    {
        SaveModSettings(modName);

        ModFolder modFolder = GetModFolder(modName);
        List<IOnDisableMod> disableModHandlers = DoGetModObjects<IOnDisableMod>(modFolder, false);
        disableModHandlers.ForEach(disableModHandler =>
        {
            try
            {
                Debug.Log($"Calling {disableModHandler.GetType().Name}.OnDisableMod");
                disableModHandler.OnDisableMod();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to call {disableModHandler.GetType()}.{nameof(disableModHandler.OnDisableMod)}");
            }
        });
    }

    public void LoadAndInstantiateMods()
    {
        try
        {
            LoadModsIntoAppDomain();
            UpdateModObjects();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to load mods: {ex.Message}");
            NotificationManager.CreateNotification(Translation.Get(R.Messages.mod_error_failedToLoad));
        }
    }

    private void LoadModsIntoAppDomain()
    {
        if (GetEnabledMods().IsNullOrEmpty())
        {
            return;
        }

        failedToLoadModFolders.Clear();

        List<ModFolder> modFolders = GetModFolders();
        foreach (ModFolder modFolder in modFolders)
        {
            if (IsModEnabled(modFolder))
            {
                try
                {
                    LoadModIntoAppDomain(modFolder);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    Debug.LogError($"Failed to load mod '{modFolder.ModName}' into app domain: {ex.Message}");
                    failedToLoadModFolders.Add(modFolder);
                }
            }
        }
    }

    public bool IsModEnabled(ModFolder modFolder)
    {
        return GetEnabledMods().Contains(modFolder.ModName);
    }

    private bool IsModEnabled(Type type)
    {
        if (!typeToModFolder.TryGetValue(type, out ModFolder modFolder))
        {
            return false;
        }

        return IsModEnabled(modFolder);
    }

    public bool IsModLoadedSuccessfully(ModFolder modFolder)
    {
        return !failedToLoadModFolders.Contains(modFolder);
    }

    private bool IsModLoadedSuccessfully(Type type)
    {
        if (!typeToModFolder.TryGetValue(type, out ModFolder modFolder))
        {
            return false;
        }

        return IsModLoadedSuccessfully(modFolder);
    }

    private bool IsModEnabled(IMod mod)
    {
        return IsModEnabled(mod.GetType());
    }

    public static List<ModFolder> GetModFolders()
    {
        List<string> modRootFolders = ModFolderUtils.GetModRootFolders();

        HashSet<ModName> ignoredFolderNames = new HashSet<ModName>()
        {
            templateModName,
        };

        return modRootFolders
            .SelectMany(modRootFolder =>
                Directory.GetDirectories(modRootFolder).Select(directory => new ModFolder(directory)))
            .Where(modFolder => !ignoredFolderNames.Contains(modFolder.ModName))
            .Where(modFolder => GetModInfo(modFolder) != null)
            .ToList();
    }

    private ModFolder GetModFolder(IMod script)
    {
        if (typeToModFolder.TryGetValue(script.GetType(), out ModFolder modFolder))
        {
            return modFolder;
        }

        return null;
    }

    public static List<T> GetModObjects<T>(ModFolder modFolder = null, bool onlyEnabledMods = true)
        where T : IMod
    {
        ModManager instance = Instance;
        if (instance == null)
        {
            return new List<T>();
        }

        return instance.DoGetModObjects<T>(modFolder, onlyEnabledMods);
    }

    private List<T> DoGetModObjects<T>(ModFolder modFolder = null, bool onlyEnabledMods = true)
        where T : IMod
    {
        if (settings.EnabledMods.IsNullOrEmpty()
            && onlyEnabledMods)
        {
            return new List<T>();
        }

        return modObjectToContext
            .Where(entry => modFolder == null
                || Equals(modFolder.Value, entry.Value.ModFolder))
            .Where(entry => !entry.Value.IsObsolete
                            && entry.Key is T)
            .Where(entry => !onlyEnabledMods || IsModEnabled(entry.Key))
            .Select(entry => (T)entry.Key)
            .ToList();
    }

    private void LoadModIntoAppDomain(ModFolder modFolder)
    {
        Debug.Log($"Loading mod '{modFolder.ModName}' into app domain");
        using DisposableStopwatch d = new($"Loading mod '{modFolder.ModName}' into app domain took <ms> ms");

        List<string> exposedAssemblyNames = defaultExposedAssemblyNames.ToList();
        ModInfo modInfo = GetModInfo(modFolder);
        if (modInfo != null
            && !modInfo.requiredAssemblies.IsNullOrEmpty())
        {
            exposedAssemblyNames.AddRange(modInfo.requiredAssemblies);
        }

        List<string> exposedTypeNames = new List<string>();
        if (modInfo != null
            && !modInfo.requiredTypes.IsNullOrEmpty())
        {
            exposedTypeNames.AddRange(modInfo.requiredTypes);
        }

        CompilerWrapper compilerWrapper = new();

        // Load AppDomain libraries
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        LoadExposedAppDomainAssembliesAndTypes(compilerWrapper, assemblies, exposedAssemblyNames, exposedTypeNames);
        appDomainTypesChanged = true;

        // Find types that are loaded from this mod folder
        List<Type> typesBefore = GetModTypes();

        // Load libraries in folder
        string[] externalDllFiles = Directory.GetFiles(modFolder.Value, "*.dll", SearchOption.AllDirectories);
        foreach (string dllFile in externalDllFiles)
        {
            Assembly assembly = Assembly.LoadFile(dllFile);
            compilerWrapper.ReferenceAssembly(assembly);
            appDomainTypesChanged = true;
        }

        // Load C# files in folder
        string[] csFilePaths = Directory.GetFiles(modFolder.Value, "*.cs");
        foreach (string filePath in csFilePaths)
        {
            LoadScriptFileIntoAppDomain(modFolder, filePath, compilerWrapper);
        }

        // Find types that are loaded from this mod folder
        List<Type> typesAfter = GetModTypes();
        foreach (Type type in typesAfter.Except(typesBefore))
        {
            typeToModFolder[type] = modFolder;
        }

        // Check compilation was successful
        string fullReport = compilerWrapper.FullReport;
        if (compilerWrapper.FullReportErrorCount > 0)
        {
            Debug.LogError($"Failed to load scripts of mod '{modFolder.ModName}'. Full compilation output:\n{fullReport}");
        }
        else
        {
            Debug.Log($"Successfully loaded scripts of mod '{modFolder.ModName}'. Full compilation output:\n{fullReport}");
        }
    }

    private void LoadScriptFileIntoAppDomain(ModFolder modFolder, string filePath, CompilerWrapper compilerWrapper)
    {
        string fileName = Path.GetFileName(filePath);

        try
        {
            compilerWrapper.StartNewPartialReport();

            string code = File.ReadAllText(filePath);
            compilerWrapper.EvaluateCode(code);
        }
        catch (Exception ex)
        {
            throw new LoadModException($"Failed to load file '{fileName}' of mod '{modFolder.ModName}'. " +
                                       $"Compilation output of the file:\n{compilerWrapper.PartialReport}", ex);
        }

        if (compilerWrapper.PartialReportErrorCount > 0)
        {
            throw new LoadModException($"Errors in file '{fileName}' of mod '{modFolder.ModName}'. " +
                                       $"Compilation output of the file:\n{compilerWrapper.PartialReport}");
        }
    }

    private List<IMod> CreateModObjects()
    {
        Type parent = typeof(IMod);
        return ModTypes
            .Where(type => parent.IsAssignableFrom(type)
                           && IsModEnabled(type)
                           && IsModLoadedSuccessfully(type))
            .Select(type => (IMod)Activator.CreateInstance(type))
            .ToList();
    }

    private List<Type> GetModTypes()
    {
        return GetTypeInAppDomain<IMod>();
    }

    private void UpdateModObjects()
    {
        using DisposableStopwatch d = new($"Instantiate mod objects took <ms> ms");

        // Instantiate new objects
        List<IMod> currentAndObsoleteModObjects;
        try
        {
            currentAndObsoleteModObjects = CreateModObjects();
        }
        catch (Exception ex)
        {
            throw new LoadModException($"Failed to instantiate mod objects: {ex.Message}", ex);
        }

        List<IMod> currentModObjects = GetNewestImplementations(currentAndObsoleteModObjects);

        // Mark context of old instances as obsolete
        List<IMod> newlyObsoleteModObjects = modObjectToContext
            .Where(entry => !currentModObjects.Contains(entry.Key) && !entry.Value.IsObsolete)
            .Select(entry => entry.Key)
            .ToList();
        foreach (IMod obsoleteModObject in newlyObsoleteModObjects)
        {
            if (modObjectToContext.TryGetValue(obsoleteModObject, out ModObjectContext modObjectContext))
            {
                modObjectContext.SetObsolete();
            }

            // Notify object that it is now obsolete
            if (obsoleteModObject is IOnModInstanceBecomesObsolete onModInstanceBecomesObsolete)
            {
                try
                {
                    onModInstanceBecomesObsolete.OnModInstanceBecomesObsolete();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    Debug.LogError($"Failed to notify mod object that it is obsolete now: '{obsoleteModObject}' of type {obsoleteModObject.GetType()}");
                }
            }
        }

        // Create context for new instances
        foreach (IMod modObject in currentModObjects)
        {
            ModFolder modFolder = GetModFolder(modObject);
            string modPersistentDataFolder = GetModPersistentDataFolder(modFolder);
            ModObjectContext modObjectContext = new(modFolder.Value, modFolder.ModName.Value, modPersistentDataFolder, false);
            modObjectToContext[modObject] = modObjectContext;
        }

        // Load mod settings
        foreach (IMod modObject in currentModObjects)
        {
            if (modObject is IModSettings modSettings)
            {
                try
                {
                    LoadModSettings(modSettings);
                }
                catch (LoadModSettingsException ex)
                {
                    Debug.LogException(ex);
                    NotificationManager.CreateNotification(Translation.Get(R.Messages.mod_error_settingsFailedToLoad,
                     "name", ex.ModFolder.ModName));
                }
            }
        }

        // Bind then inject mod objects
        List<IAutoBoundMod> autoBoundScripts = currentModObjects
            .OfType<IAutoBoundMod>()
            .ToList();
        foreach (IMod modObject in currentModObjects)
        {
            ModObjectContext modObjectContext = modObjectToContext[modObject];

            Injector childInjector = injector
                .CreateChildInjector()
                .WithBindingForInstance(modObjectContext);
            foreach (IAutoBoundMod autoBoundScript in autoBoundScripts)
            {
                ExistingInstanceProvider<object> modSettingsProvider = new(autoBoundScript);
                childInjector.AddBinding(new Binding(autoBoundScript.GetType(), modSettingsProvider));
            }

            childInjector.Inject(modObject);
        }

        // Execute mod loaded callback
        foreach (IMod modObject in currentModObjects)
        {
            if (modObject is IOnLoadMod modAction)
            {
                Debug.Log($"Calling {modObject.GetType().Name}.OnLoadMod");
                modAction.OnLoadMod();
            }
        }
    }

    private void LoadModSettings(IModSettings modSettings)
    {
        ModFolder modFolder = GetModFolder(modSettings);
        if (modFolder == null)
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
                JsonConverter.FillFromJsonCopy(json, modSettings);

                if (modSettings is IOnAfterLoadModSettings afterLoadModSettings)
                {
                    afterLoadModSettings.OnAfterLoadModSettings();
                }
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

    private void SaveModSettings(IMod modSettings)
    {
        ModFolder modFolder = GetModFolder(modSettings);
        if (modFolder == null)
        {
            return;
        }

        string modSettingsPath = GetModSettingsPath(modFolder);
        string modPersistentDataFolder = GetModPersistentDataFolder(modFolder);
        try
        {
            if (modSettings is IOnBeforeSaveModSettings beforeSaveModSettings)
            {
                beforeSaveModSettings.OnBeforeSaveModSettings();
            }

            Debug.Log($"Writing mod settings of type {modSettings.GetType()} to file '{modSettingsPath}'");
            DirectoryUtils.CreateDirectory(modPersistentDataFolder);
            string json = JsonConverter.ToJson(modSettings, true);
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

    private void LoadExposedAppDomainAssembliesAndTypes(
        CompilerWrapper compilerWrapper,
        Assembly[] assemblies,
        IReadOnlyList<string> exposedAssemblyNames,
        IReadOnlyList<string> exposedTypeNames)
    {
        if (exposedAssemblyNames.IsNullOrEmpty()
            && exposedTypeNames.IsNullOrEmpty())
        {
            return;
        }

        if (!exposedAssemblyNames.IsNullOrEmpty())
        {
            List<Assembly> exposedAssemblies = assemblies
                .Where(assembly => exposedAssemblyNames.Contains(assembly.GetName().Name))
                .ToList();
            foreach (Assembly assembly in exposedAssemblies)
            {
                compilerWrapper.ReferenceAssembly(assembly);
            }
        }

        if (!exposedTypeNames.IsNullOrEmpty())
        {
            HashSet<string> remainingTypeNames = new HashSet<string>(exposedTypeNames);
            List<Type> foundTypes = new List<Type>();
            foreach (Assembly assembly in assemblies)
            {
                foreach (string typeName in remainingTypeNames.ToList())
                {
                    try
                    {
                        Type type = assembly.GetType(typeName, false, false);
                        if (type != null)
                        {
                            foundTypes.Add(type);
                            remainingTypeNames.Remove(typeName);
                            if (remainingTypeNames.IsNullOrEmpty())
                            {
                                // All types done
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Ignore
                    }
                }

                if (remainingTypeNames.IsNullOrEmpty())
                {
                    break;
                }
            }

            if (!remainingTypeNames.IsNullOrEmpty())
            {
                throw new LoadModException($"Required types not found in app domain: {remainingTypeNames.JoinWith(", ")}");
            }
            else
            {
                compilerWrapper.ImportTypes(foundTypes.ToArray());
            }
        }
    }

    public static string GetModPersistentDataFolder(ModFolder modFolder)
    {
        return ApplicationUtils.GetPersistentDataPath($"{ModsPersistentDataFolderName}/{modFolder.ModName}");
    }

    public static string GetModSettingsPath(ModFolder modFolder)
    {
        return $"{GetModPersistentDataFolder(modFolder)}/modsettings.json";
    }

    public static ModFolder GetModFolder(ModName modName)
    {
        return Instance.typeToModFolder
            .Values
            .Distinct()
            .FirstOrDefault(modFolder => Equals(modFolder.ModName, modName));
    }

    public static string GetModDisplayName(ModFolder modFolder)
    {
        ModInfo modInfo = GetModInfo(modFolder);
        if (modInfo != null
            && !modInfo.name.IsNullOrEmpty())
        {
            return modInfo.name;
        }
        return modFolder.ModName.Value;
    }

    public static ModInfo GetModInfo(ModFolder modFolder)
    {
        string modInfoPath = $"{modFolder}/{ModInfoFileName}";
        if (!FileUtils.Exists(modInfoPath))
        {
            return null;
        }

        try
        {
            string modInfoString = File.ReadAllText(modInfoPath);
            ModInfo modInfo = YamlConverter.FromYaml<ModInfo>(modInfoString);

            return modInfo;
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
        modFolderToFileSystemWatcher.ForEach(entry => entry.Value.Dispose());
        modFolderToFileSystemWatcher.Clear();
    }

    private List<ModName> GetEnabledMods()
    {
        return settings.EnabledMods
            .Select(modName => new ModName(modName))
            .ToList();
    }

    private void SaveAllModSettings()
    {
        GetModFolders().ForEach(modFolder => SaveModSettings(modFolder.ModName));
    }

    private void SaveModSettings(ModName modName)
    {
        modObjectToContext
            .Where(entry => !entry.Value.IsObsolete
                            && entry.Value.ModName == modName.Value)
            .Select(entry => entry.Key)
            .OfType<IModSettings>()
            .ForEach(modSettings => SaveModSettings(modSettings));
    }

    public void SetModEnabled(ModFolder modFolder, bool newValue)
    {
        ModName modName = modFolder.ModName;
        if (newValue
            && !settings.EnabledMods.Contains(modName.Value))
        {
            settings.EnabledMods.Add(modName.Value);
        }
        else if (!newValue
                 && settings.EnabledMods.Contains(modName.Value))
        {
            settings.EnabledMods
                .RemoveAll(enabledModFolderName => enabledModFolderName == modName.Value);
        }
    }

    private static List<Type> GetTypeInAppDomain<T>()
    {
        Type parent = typeof(T);
        Debug.Log($"Searching implementations of {parent} in app domain.");

        using DisposableStopwatch d = new($"Searching implementations of {parent} in app domain took <ms> ms");

        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        List<Type> types = assemblies.SelectMany(assembly =>
        {
            Type[] typesOfAssembly;
            try
            {
                typesOfAssembly = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Careful: types that could not be loaded are null in the array.
                typesOfAssembly = ex.Types;
            }

            return typesOfAssembly.Where(type => type != null
                                                 && !type.IsAbstract
                                                 && !type.IsInterface
                                                 && parent.IsAssignableFrom(type));
        }).ToList();
        return types;
    }
}
