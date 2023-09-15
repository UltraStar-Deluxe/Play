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
    public static ModManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<ModManager>();

    public const string ModInfoFileName = "modinfo.yml";
    private const string ModsRootFolderName = "Mods";
    private const string TemplateModFolderName = "TemplateMod";
    private const string TemplateModNamePlaceholder = "MODNAME";
    private const string TemplateModDllFolderPlaceholder = "DEFAULT_DLL_FOLDER";

    [Inject]
    private Injector injector;

    [Inject]
    private Settings settings;

    private readonly Dictionary<IMod, ModObjectContext> modObjectToContext = new();
    private readonly Dictionary<Type, string> typeToModFolder = new();
    private readonly Dictionary<string, ModContext> modFolderToModContext = new();

    private List<string> lastEnabledMods = new();

    private bool appDomainTypesChanged = true;
    private List<Type> modTypes = new();
    private List<Type> ModTypes
    {
        get
        {
            if (appDomainTypesChanged)
            {
                appDomainTypesChanged = false;
                modTypes = GetModTypes(false);
            }

            return modTypes;
        }
    }

    private bool shouldReloadChangedMods;
    private readonly Dictionary<string, FileSystemWatcher> modFolderToFileSystemWatcher = new();
    private readonly List<string> changedCsFiles = new();

    private readonly HashSet<string> failedToLoadModFolders = new();

    private static readonly IReadOnlyList<string> defaultExposedAssemblyNames = new List<string>()
    {
        // Unity engine
        "UnityEngine",
        "UnityEngine.AudioModule",
        "UnityEngine.CoreModule",
        "UnityEngine.InputLegacyModule",
        "UnityEngine.InputModule",
        "UnityEngine.UI",
        "UnityEngine.UIElementsModule",
        "UnityEngine.UIModule",
        "UnityEngine.VideoModule",

        // Third Party Packages
        "com.achimmihca.portaudioforunity",
        "com.achimmihca.primeinputactions",
        "com.achimmihca.protrans",
        "com.achimmihca.scenechangeanimations",
        "com.achimmihca.simplehttpserverforunity",
        "com.achimmihca.uniinject",
        "com.achimmihca.utfunknownunity",
        "playshared",
        "playsharedui",

        // Plugins
        "UniRx",

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
        DirectoryUtils.CreateDirectory(GetAbsoluteDefaultModsRootFolder());
        DirectoryUtils.CreateDirectory(GetAbsoluteUserDefinedModsRootFolder());

        // CopyDefaultModToPersistentDataPath("DemoMod");

        AddDebugLogConsoleCommand();

        CreateOrUpdateModFolderFileSystemWatchers();

        lastEnabledMods = settings.EnabledMods.ToList();

        LoadAndInstantiateScripts();
    }

    private void CreateOrUpdateModFolderFileSystemWatchers()
    {
        foreach (string modFolder in GetModFolders())
        {
            CreateOrUpdateModFolderFileSystemWatcher(modFolder);
        }
    }

    private void CreateOrUpdateModFolderFileSystemWatcher(string modFolder)
    {
        if (modFolderToFileSystemWatcher.ContainsKey(modFolder))
        {
            return;
        }

        FileSystemWatcher fileSystemWatcher = FileSystemWatcherUtils.CreateFileSystemWatcher(modFolder, "*.cs",
            (sender, args) => OnCsFileChanged(modFolder, args.FullPath));
        modFolderToFileSystemWatcher[modFolder] = fileSystemWatcher;
    }

    private void OnCsFileChanged(string modFolder, string filePath)
    {
        if (!shouldReloadChangedMods)
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
            Debug.Log($"Reloading mods because of changed files: {changedCsFiles.ToCsv()}");
            changedCsFiles.Clear();
            LoadAndInstantiateScripts();
        }
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
                string text = GetAbsoluteUserDefinedModsRootFolder();
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
            (string modName) => CreateModFolderFromTemplate(modName),
            "name");

        DebugLogConsole.AddCommand("mod.interfaces", "Copy and log all mod interfaces.",
            () =>
            {
                List<Type> modTypes = GetModInterfaces();
                string text = modTypes.Select(type => type.Name).ToCsv(", ", "", "");
                ClipboardUtils.CopyToClipboard(text);
                Debug.Log($"Mod interfaces: {text}");
            });

        DebugLogConsole.AddCommand("mod.reloadOnChange", "Toggle auto reload of mods when a .cs file in a mod folder changes.",
            () =>
            {
                shouldReloadChangedMods = !shouldReloadChangedMods;
                Debug.Log($"Reload changed mods: {shouldReloadChangedMods}");
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

    private void CreateModFolderFromTemplate(string modName)
    {
        string targetModFolder = $"{GetAbsoluteUserDefinedModsRootFolder()}/{modName}";
        DirectoryInfo targetModFolderInfo = new(targetModFolder);
        if (targetModFolderInfo.Exists)
        {
            Debug.Log($"Directory already exists: '{targetModFolder}'");
            UiManager.CreateNotification("A mod with this name already exists.");
            return;
        }

        string templateModFolder = ApplicationUtils.GetStreamingAssetsPath($"{ModsRootFolderName}/{TemplateModFolderName}");
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
        Debug.Log($"Reloading mods because of newly enabled mod: {newlyEnabledModNames.ToCsv()}");
        CreateOrUpdateModFolderFileSystemWatchers();
        LoadAndInstantiateScripts();
    }

    private void OnDisableMods(List<string> newlyDisabledModNames)
    {
        newlyDisabledModNames.ForEach(modName => OnDisableMod(modName));
    }

    private void OnDisableMod(string modName)
    {
        string modFolder = GetModFolderByModName(modName);
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

    private void LoadAndInstantiateScripts()
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
            UiManager.CreateNotification($"Failed to load mods. Check log for details.\n" +
                                         $"Try to disable mods and restart the app.");
        }
    }

    private void LoadModsIntoAppDomain()
    {
        if (settings.EnabledMods.IsNullOrEmpty())
        {
            return;
        }

        failedToLoadModFolders.Clear();

        List<string> modFolders = GetModFolders();
        foreach (string modFolder in modFolders)
        {
            if (IsModEnabled(modFolder))
            {
                string modName = GetModName(modFolder);

                try
                {
                    LoadModIntoAppDomain(modFolder, modName);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    Debug.LogError($"Failed to load mod '{modName}' into app domain: {ex.Message}");
                    failedToLoadModFolders.Add(modFolder);
                }
            }
        }
    }

    private void CopyDefaultModToPersistentDataPath(string modFolderName)
    {
        try
        {
            string demoModSourceFolder = $"{GetAbsoluteDefaultModsRootFolder()}/{modFolderName}";
            string demoModTargetFolder = $"{GetAbsoluteUserDefinedModsRootFolder()}/{modFolderName}";
            if (Directory.Exists(demoModSourceFolder)
                && !Directory.Exists(demoModTargetFolder))
            {
                Debug.Log($"Copying default mod '{modFolderName}' to persistentDataPath (from: '{demoModSourceFolder}', to: '{demoModTargetFolder}')");
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
                Debug.Log($"Not copying default mod '{modFolderName}' to persistentDataPath because the target folder already exists.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to copy default mod '{modFolderName}' to persistentDataPath.");
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

    public bool IsModLoadedSuccessfully(string modFolder)
    {
        return !failedToLoadModFolders.Contains(modFolder);
    }

    private bool IsModLoadedSuccessfully(Type type)
    {
        if (!typeToModFolder.TryGetValue(type, out string modFolder))
        {
            return false;
        }

        return IsModLoadedSuccessfully(modFolder);
    }

    private bool IsModEnabled(IMod mod)
    {
        return IsModEnabled(mod.GetType());
    }

    public List<string> GetModFolders()
    {
        List<string> modParentFolders = new()
        {
            GetAbsoluteDefaultModsRootFolder(),
            GetAbsoluteUserDefinedModsRootFolder(),
        };

        HashSet<string> ignoredFolderNames = new HashSet<string>()
        {
            TemplateModFolderName,
        };

        return modParentFolders
            .SelectMany(modParentFolder => Directory.GetDirectories(modParentFolder))
            .Where(modFolder => !ignoredFolderNames.Contains(Path.GetFileName(modFolder)))
            .ToList();
    }

    private string GetModFolder(IMod script)
    {
        if (typeToModFolder.TryGetValue(script.GetType(), out string modFolder))
        {
            return modFolder;
        }

        return "";
    }

    public static List<T> GetModObjects<T>(string modFolder = null, bool onlyFromEnabledMods = true)
        where T : IMod
    {
        ModManager instance = Instance;
        if (instance == null)
        {
            return new List<T>();
        }

        return instance.DoGetModObjects<T>(modFolder, onlyFromEnabledMods);
    }

    private List<T> DoGetModObjects<T>(string modFolder = null, bool onlyFromEnabledMods = true)
        where T : IMod
    {
        if (settings.EnabledMods.IsNullOrEmpty()
            && onlyFromEnabledMods)
        {
            return new List<T>();
        }

        return modObjectToContext
            .Where(entry => modFolder == null
                || modFolder == entry.Value.ModFolder)
            .Where(entry => !entry.Value.IsObsolete
                            && entry.Key is T)
            .Where(entry => !onlyFromEnabledMods || IsModEnabled(entry.Key))
            .Select(entry => (T)entry.Key)
            .ToList();
    }

    private void LoadModIntoAppDomain(string modFolder, string modName)
    {
        Debug.Log($"Loading mod '{modName}' into app domain");
        using DisposableStopwatch d = new($"Loading mod '{modName}' into app domain took <ms> ms");

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
        List<Type> typesBefore = GetModTypes(false);

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
            LoadScriptFileIntoAppDomain(modName, filePath, compilerWrapper);
        }

        // Find types that are loaded from this mod folder
        List<Type> typesAfter = GetModTypes(false);
        foreach (Type type in typesAfter.Except(typesBefore))
        {
            typeToModFolder[type] = modFolder;
        }

        // Check compilation was successful
        string fullReport = compilerWrapper.FullReport;
        if (compilerWrapper.FullReportErrorCount > 0)
        {
            Debug.LogError($"Failed to load scripts of mod '{modName}'. Full compilation output:\n{fullReport}");
        }
        else
        {
            Debug.Log($"Successfully loaded scripts of mod '{modName}'. Full compilation output:\n{fullReport}");
        }
    }

    private void LoadScriptFileIntoAppDomain(string modName, string filePath, CompilerWrapper compilerWrapper)
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
            throw new LoadModException($"Failed to load file '{fileName}' of mod '{modName}'. " +
                                       $"Compilation output of the file:\n{compilerWrapper.PartialReport}", ex);
        }

        if (compilerWrapper.PartialReportErrorCount > 0)
        {
            throw new LoadModException($"Errors in file '{fileName}' of mod '{modName}'. " +
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

    private List<Type> GetModTypes(bool logExceptions)
    {
        Debug.Log("Searching mod types in app domain.");

        using DisposableStopwatch d = new($"Searching mod types in app domain took <ms> ms");

        Type parent = typeof(IMod);
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

    private void UpdateModObjects()
    {
        using DisposableStopwatch d = new($"Instantiate runtime loaded scripts took <ms> ms");

        // Instantiate new objects
        List<IMod> currentAndObsoleteModObjects;
        try
        {
            currentAndObsoleteModObjects = CreateModObjects();
        }
        catch (Exception ex)
        {
            throw new LoadModException($"Failed to instantiate runtime loaded scripts: {ex.Message}", ex);
        }

        List<IMod> currentModObjects = GetNewestImplementations(currentAndObsoleteModObjects);

        // Mark context of old instances as obsolete
        modObjectToContext
            .Where(entry => !currentModObjects.Contains(entry.Key))
            .ForEach(entry => entry.Value.SetObsolete());

        // Create context for new instances
        foreach (IMod modObject in currentModObjects)
        {
            string modFolder = GetModFolder(modObject);
            ModObjectContext modObjectContext = new(modFolder, false);
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
                    UiManager.CreateNotification($"Failed to load settings of mod '{GetModName(ex.ModFolder)}'");
                }
            }
        }

        // Bind then inject runtime loaded scripts
        List<IAutoBoundMod> autoBoundScripts = currentModObjects
            .OfType<IAutoBoundMod>()
            .ToList();
        foreach (IMod modObject in currentModObjects)
        {
            ModObjectContext modObjectContext = modObjectToContext[modObject];

            string modFolder = GetModFolder(modObject);
            ModContext modContext = GetOrCreateModContext(modFolder);

            Injector childInjector = injector
                .CreateChildInjector()
                .WithBindingForInstance(modContext)
                .WithBindingForInstance(modObjectContext);
            foreach (IAutoBoundMod autoBoundScript in autoBoundScripts)
            {
                ExistingInstanceProvider<object> modSettingsProvider = new(autoBoundScript);
                childInjector.AddBinding(new Binding(autoBoundScript.GetType(), modSettingsProvider));
            }

            childInjector.Inject(modObject);
        }

        // Execute mod actions
        foreach (IMod modObject in currentModObjects)
        {
            if (modObject is IModAction modAction)
            {
                Debug.Log($"Calling {modObject.GetType().Name}.Invoke");
                modAction.Invoke();
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

    private void SaveModSettings(IMod modSettings)
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
                foreach (string typeName in remainingTypeNames)
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
            }

            if (!remainingTypeNames.IsNullOrEmpty())
            {
                throw new LoadModException($"Required types not found in app domain: {remainingTypeNames.ToCsv(", ", "", "")}");
            }
            else
            {
                compilerWrapper.ImportTypes(foundTypes.ToArray());
            }
        }
    }

    public static string GetAbsoluteDefaultModsRootFolder()
    {
        return ApplicationUtils.GetStreamingAssetsPath(ModsRootFolderName);
    }

    public static string GetAbsoluteUserDefinedModsRootFolder()
    {
        return ApplicationUtils.GetPersistentDataPath(ModsRootFolderName);
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
        ModInfo modInfo = GetModInfo(modFolder);
        if (modInfo != null
            && !modInfo.name.IsNullOrEmpty())
        {
            return modInfo.name;
        }
        return Path.GetFileName(modFolder);
    }

    public static ModInfo GetModInfo(string modFolder)
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

    private void SaveAllModSettings()
    {
        modObjectToContext
            .Where(entry => !entry.Value.IsObsolete)
            .Select(entry => entry.Key)
            .OfType<IModSettings>()
            .ForEach(modSettings => SaveModSettings(modSettings));
    }
}
