using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;

// Handles the loading, saving and application of themes for the app
// This includes the background material shader values, background particle effects, and UIToolkit colors/styles
public class ThemeManager : AbstractSingletonBehaviour, ISpriteHolder, INeedInjection
{
    /**
     * Filename without extension of the theme that should be loaded by default
     */
    public const string DefaultThemeName = "vinyl";
    public const string UiRenderTextureName = "ThemeManager.UiRenderTexture";
    public const string ParticleRenderTextureName = "ThemeManager.ParticleRenderTexture";
    private const string ExampleThemeFilePathInStreamingAssets = "Themes/example_theme.json.txt";
    private const string StaticBackgroundImageElementName = "staticBackgroundImage";
    private readonly Color defaultGoldenColor = Colors.CreateColor("#DACD4A");

    public static ThemeManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<ThemeManager>();

    [InjectedInInspector]
    public Material backgroundMaterial;

    [InjectedInInspector]
    public Material particleMaterial;

    [InjectedInInspector]
    public ParticleSystem backgroundParticleSystem;

    [InjectedInInspector]
    public BackgroundShaderControl backgroundShaderControl;

    [InjectedInInspector]
    public SceneRecipeManager sceneRecipeManager;

    [InjectedInInspector]
    public Camera backgroundParticlesCamera;

    [InjectedInInspector]
    public bool renderUiWithBackgroundShader = true;

    [InjectedInInspector]
    public bool applyThemeSpecificStyles = true;

    [InjectedInInspector]
    public VideoPlayer backgroundVideoPlayer;

    [InjectedInInspector]
    public VideoPlayer backgroundLightVideoPlayer;

    private Material backgroundMaterialCopy;
    private Material particleMaterialCopy;

    private readonly List<ThemeMeta> themeMetas = new();

    private readonly HashSet<VisualElement> alreadyProcessedVisualElements = new();

    private readonly HashSet<string> failedToLoadThemeNames = new();

    private readonly List<Sprite> loadedSprites = new();
    private Sprite dynamicBackgroundStaticImageSprite;

    private bool anyThemeLoaded;

    private bool isDropdownMenuOpened;

    [Inject]
    private Settings settings;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private RenderTextureManager renderTextureManager;

    [Inject]
    private BackgroundLightManager backgroundLightManager;

    private HashSet<VisualElement> registeredSfxVisualElements = new();

    private string lastThemeDynamicBackgroundJson;

    private readonly Dictionary<string, StyleSheet> filePathToStyleSheet = new();

    private readonly List<FileSystemWatcher> styleSheetFileSystemWatchers = new();

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void AwakeSingleton()
    {
        // Disable particle camera until a corresponding particle configuration has been loaded.
        backgroundParticlesCamera.gameObject.SetActive(false);
    }

    protected override void StartSingleton()
    {
        DirectoryUtils.CreateDirectory(ThemeFolderUtils.GetUserDefinedThemesFolderAbsolutePath());
        ImageManager.AddSpriteHolder(this);

        settings.ObserveEveryValueChanged(it => it.AnimatedBackground)
            .Subscribe(animatedBackground => backgroundShaderControl.SetSimpleBackgroundEnabled(!animatedBackground))
            .AddTo(gameObject);

        sceneNavigator.SceneChangedEventStream.Subscribe(_ => OnSceneChanged());

        CopyExampleThemeToUserDefinedThemesFolder();

        // Apply theme to context menu popups
        ContextMenuControl.AnyContextMenuOpenedEventStream
            .Subscribe(contextMenuPopupControl => ApplyThemeToContextMenuPopup(contextMenuPopupControl));

        uiManager.ChildrenChangedEventStream
            .Subscribe(evt => ApplyStyles(evt.targetChild));
        ApplyStyles(uiDocument.rootVisualElement);
    }

    private void ApplyThemeToContextMenuPopup(ContextMenuPopupControl contextMenuPopupControl)
    {
        VisualElement root = contextMenuPopupControl.VisualElement;
        ThemeJson themeJson = GetCurrentTheme()?.ThemeJson;
        if (themeJson == null
            || root == null)
        {
            return;
        }

        // Only apply font color
        themeJson.primaryFontColor.IfNotDefault(color =>
            root.Query().ForEach(element =>
            {
                element.style.color = new StyleColor(color);
            }));
    }

    private void OnSceneChanged()
    {
        ApplyThemeStyleUtils.ClearCache();
        registeredSfxVisualElements.Clear();
        alreadyProcessedVisualElements.Clear();
        ApplyThemeBackground(GetCurrentTheme());
    }

    private void CopyExampleThemeToUserDefinedThemesFolder()
    {
        string sourceExampleThemeFilePath = ApplicationUtils.GetStreamingAssetsPath(ExampleThemeFilePathInStreamingAssets);
        if (!FileUtils.Exists(sourceExampleThemeFilePath))
        {
            return;
        }

        string exampleThemeFileName = Path.GetFileName(ExampleThemeFilePathInStreamingAssets);
        string targetExampleThemeFilePath = $"{ThemeFolderUtils.GetUserDefinedThemesFolderAbsolutePath()}/{exampleThemeFileName}";
        FileUtils.Copy(sourceExampleThemeFilePath, targetExampleThemeFilePath, true);
        Debug.Log($"Copied example theme to user defined themes folder: {targetExampleThemeFilePath}");
    }

    protected void LateUpdate()
    {
        // Use LateUpdate to apply theme styles
        // because this works for newly opened DropdownMenus in the same frame.
        if (isDropdownMenuOpened)
        {
            isDropdownMenuOpened = false;
            VisualElement dropdownParent = uiDocument.rootVisualElement.focusController.focusedElement as VisualElement;
            if (dropdownParent == null)
            {
                return;
            }

            alreadyProcessedVisualElements.Remove(dropdownParent);
            ApplyStyles(dropdownParent);
        }
    }

    public async void UpdateSceneTextures(Texture transitionTexture)
    {
        if (backgroundMaterialCopy == null)
        {
            backgroundMaterialCopy = new Material(backgroundMaterial);
        }

        if (particleMaterialCopy == null)
        {
            particleMaterialCopy = new Material(particleMaterial);
        }

        if (renderUiWithBackgroundShader)
        {
            // The UIDocument is rendered into a RenderTexture, which is then blended into the background shader.
            // particleRenderTexture may use a smaller resolution than the screen.
            renderTextureManager.GetOrCreateScreenAspectRatioRenderTexture(ParticleRenderTextureName,
                particleRenderTexture =>
                {
                    backgroundParticlesCamera.targetTexture = particleRenderTexture;
                });

            // uiRenderTexture should use the exact screen size.
            renderTextureManager.GetOrCreateScreenSizedRenderTexture(UiRenderTextureName,
                uiRenderTexture =>
                {
                    RenderTexture particleRenderTexture = renderTextureManager.GetExistingRenderTexture(ParticleRenderTextureName);

                    uiDocument.panelSettings.targetTexture = uiRenderTexture;
                    backgroundShaderControl.SetUiTextures(
                        uiRenderTexture,
                        particleRenderTexture,
                        transitionTexture);
                });
        }
        else
        {
            // The UIDocument is rendered directly to the screen by Unity.
            uiDocument.panelSettings.targetTexture = null;
        }

        if (!anyThemeLoaded)
        {
            await LoadCurrentThemeAsync();
        }
    }

    private async Awaitable LoadCurrentThemeAsync()
    {
        if (!settings.EnableDynamicThemes)
        {
            DisableDynamicBackground();
            return;
        }

        EScene currentScene = GetCurrentScene();
        if (IsIgnoredScene(currentScene))
        {
            return;
        }

        ThemeMeta themeMeta = GetCurrentTheme();
        if (themeMeta == null)
        {
            Debug.Log($"Cannot load theme. Theme is null.");
            return;
        }

        Debug.Log($"Loading theme '{themeMeta.FileNameWithoutExtension}'");
        loadedSprites.Clear();

        ApplyThemeStyleSheets(themeMeta);

        await Awaitable.EndOfFrameAsync();
        ApplyThemeBackground(themeMeta);
        alreadyProcessedVisualElements.Clear();
        ApplyStyles(uiDocument.rootVisualElement);
        anyThemeLoaded = true;
    }

    private void ApplyThemeStyleSheets(ThemeMeta themeMeta)
    {
        // Remove old theme style sheets
        foreach (StyleSheet styleSheet in filePathToStyleSheet.Values)
        {
            uiDocument.rootVisualElement.styleSheets.Remove(styleSheet);
        }

        if (themeMeta == null
            || themeMeta.ThemeJson == null
            || themeMeta.ThemeJson.styleSheets.IsNullOrEmpty())
        {
            return;
        }

        foreach (string styleSheetFile in themeMeta.ThemeJson.styleSheets)
        {
            string absoluteStyleSheetFile = ThemeMetaUtils.GetAbsoluteFilePath(themeMeta, styleSheetFile);
            if (!File.Exists(absoluteStyleSheetFile))
            {
                Debug.LogWarning($"Style Sheet file does not exist: {absoluteStyleSheetFile}");
                continue;
            }

            if (!filePathToStyleSheet.TryGetValue(absoluteStyleSheetFile, out StyleSheet styleSheet))
            {
                styleSheet = LoadAndCacheStyleSheet(absoluteStyleSheetFile);
            }

            if (!uiDocument.rootVisualElement.styleSheets.Contains(styleSheet))
            {
                uiDocument.rootVisualElement.styleSheets.Add(styleSheet);
            }
        }
    }

    private StyleSheet LoadAndCacheStyleSheet(string styleSheetFile)
    {
        string styleSheetContent = File.ReadAllText(styleSheetFile);
        StyleSheet styleSheet = StyleSheetUtils.CreateStyleSheet(styleSheetContent);
        filePathToStyleSheet[styleSheetFile] = styleSheet;

        AddStyleSheetFileSystemWatcher(styleSheetFile, styleSheet);

        return styleSheet;
    }

    private void AddStyleSheetFileSystemWatcher(string styleSheetFile, StyleSheet styleSheet)
    {
        void OnThemeStyleSheetFileChanged(object sender, FileSystemEventArgs e)
        {
            ThreadUtils.RunOnMainThread(() => UpdateThemeStyleSheet(styleSheetFile, styleSheet));
        }

        Debug.Log($"Creating file system watcher for theme style sheet: {styleSheetFile}");
        FileSystemWatcher fileSystemWatcher = FileSystemWatcherFactory.CreateFileSystemWatcher(
            Path.GetDirectoryName(styleSheetFile),
            new FileSystemWatcherConfig("ThemeStyleSheetWatcher", Path.GetFileName(styleSheetFile)),
            OnThemeStyleSheetFileChanged);
        styleSheetFileSystemWatchers.Add(fileSystemWatcher);
    }

    private void UpdateThemeStyleSheet(string styleSheetFile, StyleSheet styleSheet)
    {
        Debug.Log($"Reloading changed style sheet file: {styleSheetFile}");
        try
        {
            bool wasAdded = uiDocument.rootVisualElement.styleSheets.Contains(styleSheet);
            uiDocument.rootVisualElement.styleSheets.Remove(styleSheet);

            string styleSheetContent = File.ReadAllText(styleSheetFile);
            StyleSheetUtils.BuildStyleSheet(styleSheet, styleSheetContent);

            if (wasAdded)
            {
                uiDocument.rootVisualElement.styleSheets.Add(styleSheet);
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to update style sheet with content from file '{styleSheetFile}'");
        }
    }

    private void ApplyThemeBackground(ThemeMeta themeMeta)
    {
        if (themeMeta == null)
        {
            return;
        }
        ApplyThemeStaticBackgroundImage(themeMeta);
        ApplyThemeDynamicBackground(themeMeta);
    }

    private Image GetExistingStaticBackgroundElement(string name)
    {
        Image backgroundElement = uiDocument.rootVisualElement.Q<Image>(name);
        return backgroundElement;
    }

    private Image GetOrCreateStaticBackgroundElement(string name)
    {
        Image backgroundElement = GetExistingStaticBackgroundElement(name);
        if (backgroundElement != null)
        {
            return backgroundElement;
        }

        backgroundElement = new Image();
        backgroundElement.name = name;
        backgroundElement.AddToClassList("overlay");
        backgroundElement.AddToClassList("staticBackgroundElement");
        uiDocument.rootVisualElement.AddAsFirstChild(backgroundElement);
        return backgroundElement;
    }

    private async void ApplyThemeStaticBackgroundImage(ThemeMeta themeMeta)
    {
        EScene currentScene = GetCurrentScene();
        if (!ThemeMetaUtils.HasStaticBackground(themeMeta, settings, currentScene))
        {
            DisableStaticBackground();
            return;
        }

        Image backgroundElement = GetOrCreateStaticBackgroundElement(StaticBackgroundImageElementName);
        if (backgroundElement == null)
        {
            return;
        }

        StaticBackgroundJson staticBackgroundJson = ThemeMetaUtils.GetStaticBackgroundJsonForScene(themeMeta, currentScene);
        string absoluteImageFilePath = ThemeMetaUtils.GetAbsoluteFilePath(themeMeta, staticBackgroundJson.imagePath);
        if (ApplicationUtils.IsSupportedImageFormat(Path.GetExtension(absoluteImageFilePath)))
        {
            Sprite loadedSprite = await ImageManager.LoadSpriteFromUriAsync(absoluteImageFilePath);
            backgroundElement.style.backgroundImage = new StyleBackground(loadedSprite);
            ApplyThemeStyleUtils.TryApplyScaleMode(backgroundElement, staticBackgroundJson.imageScaleMode);
        }
        else
        {
            backgroundElement.style.backgroundImage = null;
        }
    }

    private void StopVideoPlayer(VideoPlayer videoPlayer)
    {
        videoPlayer.Stop();
        videoPlayer.url = "";
        videoPlayer.playbackSpeed = 1;
    }

    private void StartVideoPlayer(VideoPlayer videoPlayer, string videoUrl, float playbackSpeed = 0)
    {
        if (videoPlayer.url != videoUrl)
        {
            videoPlayer.url = videoUrl;
        }

        if (!videoPlayer.isPlaying)
        {
            videoPlayer.Play();
        }

        float finalPlaybackSpeed = playbackSpeed > 0
            ? playbackSpeed
            : 1;
        if (Math.Abs(videoPlayer.playbackSpeed - finalPlaybackSpeed) > 0.01f)
        {
            videoPlayer.playbackSpeed = finalPlaybackSpeed;
        }
    }

    private void ApplyThemeDynamicBackground(ThemeMeta themeMeta)
    {
        EScene currentScene = GetCurrentScene();
        if (!ThemeMetaUtils.HasDynamicBackground(themeMeta, settings, currentScene))
        {
            DisableDynamicBackground();
            return;
        }

        DynamicBackgroundJson backgroundJson = ThemeMetaUtils.GetDynamicBackgroundJsonForScene(themeMeta, currentScene);
        if (backgroundJson == null)
        {
            backgroundJson = new();
        }

        string backgroundJsonAsString = JsonConverter.ToJson(backgroundJson);
        if (backgroundJsonAsString != lastThemeDynamicBackgroundJson)
        {
            ApplyThemeParticleBackground(themeMeta, backgroundJson);
        }

        ApplyThemeBaseBackground(themeMeta, backgroundJson);
        ApplyThemeLightBackground(themeMeta, backgroundJson);

        lastThemeDynamicBackgroundJson = backgroundJsonAsString;
    }

    private async void ApplyThemeParticleBackground(ThemeMeta themeMeta, DynamicBackgroundJson backgroundJson)
    {
        // Material
        if (!backgroundJson.gradientRampFile.IsNullOrEmpty())
        {
            string gradientPath = ThemeMetaUtils.GetAbsoluteFilePath(themeMeta, backgroundJson.gradientRampFile);
            if (File.Exists(gradientPath))
            {
                TextureWrapMode textureWrapMode = backgroundJson.gradientScrollingSpeed > 0
                    ? TextureWrapMode.Repeat
                    : TextureWrapMode.Clamp;
                Sprite gradientSprite = await ImageManager.LoadSpriteFromUriAsync(gradientPath);
                loadedSprites.Add(gradientSprite);
                gradientSprite.texture.wrapMode = textureWrapMode;
                backgroundMaterial.SetTexture("_ColorRampTex", gradientSprite.texture);
            }
            else
            {
                Debug.LogError($"[THEME] Gradient Ramp file can't be opened at path: {backgroundJson.gradientRampFile}");
            }
        }

        if (!backgroundJson.gradientType.IsNullOrEmpty())
        {
            EDynamicBackgroundGradientType result;
            if (Enum.TryParse(backgroundJson.gradientType, true, out result))
            {
                backgroundMaterial.SetFloat("_Gradient", (int)result);
            }
        }

        // default to Color.clear to hide pattern if no file specified or found
        Color patternColor = Color.clear;
        if (!backgroundJson.patternFile.IsNullOrEmpty())
        {
            string patternPath = ThemeMetaUtils.GetAbsoluteFilePath(themeMeta, backgroundJson.patternFile);
            if (File.Exists(patternPath))
            {
                Sprite patternSprite = await ImageManager.LoadSpriteFromUriAsync(patternPath);
                loadedSprites.Add(patternSprite);
                patternSprite.texture.wrapMode = TextureWrapMode.Repeat;
                backgroundMaterial.SetTexture("_PatternTex", patternSprite.texture);

                patternColor = backgroundJson.patternColor;
            }
            else
            {
                Debug.LogError($"[THEME] Pattern file can't be opened at path: {backgroundJson.patternFile}");
            }
        }

        float screenRatio = Screen.width / (float)Screen.height;
        backgroundMaterial.SetVector("_PatternTex_ST", new Vector4(backgroundJson.patternScale.x * screenRatio, backgroundJson.patternScale.y, backgroundJson.patternScrolling.x, backgroundJson.patternScrolling.y));
        backgroundMaterial.SetColor("_PatternColor", patternColor);
        backgroundMaterial.SetFloat("_Scale", backgroundJson.gradientScale);
        backgroundMaterial.SetFloat("_Smoothness", backgroundJson.gradientSmoothness);
        backgroundMaterial.SetFloat("_Angle", backgroundJson.gradientAngle);
        backgroundMaterial.SetFloat("_EnableGradientAnimation", backgroundJson.gradientAnimation ? 1 : 0);
        backgroundMaterial.SetFloat("_GradientAnimSpeed", backgroundJson.gradientAnimSpeed);
        backgroundMaterial.SetFloat("_GradientAnimAmp", backgroundJson.gradientAnimAmplitude);
        backgroundMaterial.SetFloat("_ColorRampScrolling", backgroundJson.gradientScrollingSpeed);
        backgroundMaterial.SetFloat("_UiShadowOpacity", backgroundJson.uiShadowOpacity);
        backgroundMaterial.SetVector("_UiShadowOffset", backgroundJson.uiShadowOffset);

        if (backgroundJson.uiShadowOpacity > 0)
        {
            backgroundMaterial.EnableKeyword("_UI_SHADOW");
        }
        else
        {
            backgroundMaterial.DisableKeyword("_UI_SHADOW");
        }

        // Particles
        if (!backgroundJson.particleFile.IsNullOrEmpty())
        {
            backgroundParticleSystem.gameObject.SetActive(true);
            backgroundParticlesCamera.gameObject.SetActive(true);
            string particlePath = ThemeMetaUtils.GetAbsoluteFilePath(themeMeta, backgroundJson.particleFile);
            if (File.Exists(particlePath))
            {
                Sprite particleSprite = await ImageManager.LoadSpriteFromUriAsync(particlePath);
                loadedSprites.Add(particleSprite);
                particleSprite.texture.wrapMode = TextureWrapMode.Clamp;
                particleMaterial.mainTexture = particleSprite.texture;
            }
            else
            {
                Debug.LogError($"[THEME] Particle file can't be opened at path: {backgroundJson.particleFile}");
            }
        }
        else
        {
            backgroundParticleSystem.gameObject.SetActive(false);
            backgroundParticlesCamera.gameObject.SetActive(false);
            RenderTextureUtils.Clear(backgroundParticlesCamera.targetTexture);
        }

        ParticleSystem.MainModule main = backgroundParticleSystem.main;
        main.startColor = new Color(1, 1, 1, backgroundJson.particleOpacity);

        backgroundParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        backgroundParticleSystem.Play();
    }

    private async void ApplyThemeBaseBackground(ThemeMeta themeMeta, DynamicBackgroundJson backgroundJson)
    {
        string absoluteVideoFilePath = ThemeMetaUtils.GetAbsoluteFilePath(themeMeta, backgroundJson.videoPath);
        if (!absoluteVideoFilePath.IsNullOrEmpty()
            && ApplicationUtils.IsSupportedVideoFormat(Path.GetExtension(absoluteVideoFilePath)))
        {
            string uri = WebRequestUtils.AbsoluteFilePathToUri(absoluteVideoFilePath);
            string videoPlayerUrl = ApplicationUtils.GetVideoPlayerUri(uri);
            StartVideoPlayer(backgroundVideoPlayer, videoPlayerUrl, backgroundJson.videoPlaybackSpeed);
            backgroundShaderControl.SetBaseTextureEnabled(true);
        }
        else
        {
            StopVideoPlayer(backgroundVideoPlayer);
            backgroundShaderControl.SetBaseTextureEnabled(false);

            // Try to use static image as base background
            string absoluteImageFilePath = ThemeMetaUtils.GetAbsoluteFilePath(themeMeta, backgroundJson.imagePath);
            if (!absoluteImageFilePath.IsNullOrEmpty()
                && ApplicationUtils.IsSupportedImageFormat(Path.GetExtension(absoluteImageFilePath)))
            {
                Sprite loadedSprite = await ImageManager.LoadSpriteFromUriAsync(absoluteImageFilePath);
                dynamicBackgroundStaticImageSprite = loadedSprite;
                backgroundShaderControl.SetBaseTexture(loadedSprite.texture);
                backgroundShaderControl.SetBaseTextureEnabled(true);
            }
            else
            {
                dynamicBackgroundStaticImageSprite = null;
                backgroundShaderControl.SetBaseTexture(backgroundShaderControl.baseTexture);
                backgroundShaderControl.SetBaseTextureEnabled(false);
            }
        }
    }

    private void ApplyThemeLightBackground(ThemeMeta themeMeta, DynamicBackgroundJson backgroundJson)
    {
        string absoluteLightVideoFilePath = ThemeMetaUtils.GetAbsoluteFilePath(themeMeta, backgroundJson.lightVideoPath);
        if (!absoluteLightVideoFilePath.IsNullOrEmpty()
            && ApplicationUtils.IsSupportedVideoFormat(Path.GetExtension(absoluteLightVideoFilePath)))
        {
            string uri = WebRequestUtils.AbsoluteFilePathToUri(absoluteLightVideoFilePath);
            string videoPlayerUrl = ApplicationUtils.GetVideoPlayerUri(uri);
            StartVideoPlayer(backgroundLightVideoPlayer, videoPlayerUrl, backgroundJson.lightVideoPlaybackSpeed);

            // Use the video as background lights instead of the bokeh particle system
            backgroundLightManager.IsBackgroundLightEnabled = false;
        }
        else
        {
            StopVideoPlayer(backgroundLightVideoPlayer);

            // Use the bokeh particle system as background lights
            backgroundLightManager.IsBackgroundLightEnabled = true;
        }
    }

    protected override void OnDestroySingleton()
    {
        if (backgroundMaterialCopy != null)
        {
            backgroundMaterial.CopyPropertiesFromMaterial(backgroundMaterialCopy);
            Destroy(backgroundMaterialCopy);
        }

        if (particleMaterialCopy != null)
        {
            particleMaterial.CopyPropertiesFromMaterial(particleMaterialCopy);
            Destroy(particleMaterialCopy);
        }

        foreach (FileSystemWatcher styleSheetFileSystemWatcher in styleSheetFileSystemWatchers)
        {
            styleSheetFileSystemWatcher.Dispose();
        }

        ApplyThemeStyleUtils.ClearCache();
    }

    public void SetCurrentTheme(ThemeMeta themeMeta)
    {
        settings.ThemeName = themeMeta.FileNameWithoutExtension;
        LoadCurrentThemeAsync();
    }

    public ThemeMeta GetCurrentTheme()
    {
        return GetThemeByName(settings.ThemeName);
    }

    public ThemeMeta GetThemeByName(string themeName)
    {
        if (failedToLoadThemeNames.Contains(themeName))
        {
            // This theme is already known to fail. Do not try again.
            return GetDefaultTheme();
        }

        ThemeMeta themeMeta = GetThemeMetas()
            .FirstOrDefault(themeMeta => themeMeta.FileNameWithoutExtension == themeName);
        if (themeMeta == null)
        {
            failedToLoadThemeNames.Add(themeName);
            Debug.LogWarning($"No theme found with name {themeName}. Using default theme instead.");
            return GetDefaultTheme();
        }

        return themeMeta;
    }

    public ThemeMeta GetDefaultTheme()
    {
        ThemeMeta defaultThemeMeta = GetThemeMetas()
            .FirstOrDefault(themeMeta => themeMeta.FileNameWithoutExtension == DefaultThemeName);

        if (defaultThemeMeta == null)
        {
            string availableThemeMetasCsv = GetThemeMetas().Select(themeMeta => themeMeta.FileNameWithoutExtension).JoinWith(", ");
            Debug.LogError($"Default theme '{DefaultThemeName}' not found. Available themes: {availableThemeMetasCsv}");
        }

        return defaultThemeMeta;
    }

    public List<ThemeMeta> GetThemeMetas()
    {
        if (!themeMetas.IsNullOrEmpty())
        {
            return themeMetas;
        }

        List<string> themeFolders = ThemeFolderUtils.GetThemeFolders();

        themeFolders.ForEach(themeFolder =>
        {
            if (Directory.Exists(themeFolder))
            {
                string[] themeFilesInFolder = Directory.GetFiles(themeFolder, "*.json", SearchOption.AllDirectories);
                List<ThemeMeta> themeMetasInFolder = themeFilesInFolder
                    .Select(absoluteThemeFilePath => new ThemeMeta(absoluteThemeFilePath))
                    .ToList();
                themeMetas.AddRange(themeMetasInFolder);
            }
        });

        ResolveThemeMetaUtils.ResolveThemes(themeMetas);

        string themeNamesCsv = themeMetas.Select(themeMeta => themeMeta.FileNameWithoutExtension).JoinWith(", ");
        Debug.Log($"Found {themeMetas.Count} themes: {themeNamesCsv}");

        return themeMetas;
    }

    private void ApplyStyles(VisualElement root = null)
    {
        root ??= uiDocument.rootVisualElement;
        if (!applyThemeSpecificStyles
            // Settings can be null when running a specific scene in the Unity editor
            // and injection did not finish yet.
            || settings == null)
        {
            return;
        }

        // using DisposableStopwatch d = new($"Apply styles to '{root.name}' in frame {Time.frameCount}", ELogEventLevel.Verbose);

        if (!settings.EnableDynamicThemes)
        {
            DisableDynamicBackground();
            return;
        }

        EScene currentScene = GetCurrentScene();
        if (IsIgnoredScene(currentScene))
        {
            return;
        }

        ThemeMeta themeMeta = GetCurrentTheme();
        if(themeMeta == null
           || themeMeta.ThemeJson == null)
        {
            Debug.LogWarning("Not applying theme styles because current theme is null");
            return;
        }

        // Scene specific elements
        ApplySceneSpecificStyles(themeMeta, root, GetCurrentScene());

        // Text styles
        ApplyTextStyles(themeMeta, root);

        // Control styles
        ApplyControlStyles(themeMeta, root);
    }

    private void ApplyControlStyles(ThemeMeta themeMeta, VisualElement root)
    {
        // Buttons
        root.Query<Button>().ForEach(button =>
        {
            ControlStyleConfig styleConfig = GetColorStyleConfig(themeMeta, button);
            ApplyControlColorConfigToVisualElement(button, styleConfig);

            RegisterDefaultButtonSfxCallback(button);
        });

        // Choosers
        ControlStyleConfig defaultControlStyleConfig = themeMeta.ThemeJson.defaultControl;

        if (defaultControlStyleConfig != null)
        {
            root.Query(null, "chooserControlsRow").ForEach(controlsRow =>
            {
                defaultControlStyleConfig.backgroundColor.IfNotDefault(backgroundColor =>
                {
                    controlsRow.style.backgroundColor = new StyleColor(backgroundColor);
                });

                if (defaultControlStyleConfig.backgroundGradient != null)
                {
                    ApplyThemeStyleUtils.ApplyGradient(controlsRow, defaultControlStyleConfig.backgroundGradient);
                }
                else
                {
                    ApplyThemeStyleUtils.ApplyGradient(controlsRow, null);
                }

                defaultControlStyleConfig.fontColor.IfNotDefault(fontColor =>
                {
                    controlsRow.Query<Label>().ForEach(label =>
                    {
                        if (ApplyThemeStyleUtils.IsIgnoredVisualElement(label))
                        {
                            return;
                        }
                        label.style.color = new StyleColor(fontColor);
                    });
                });
            });
        }

        // Toggle
        root.Query<Toggle>().ForEach(toggle =>
        {
            VisualElement styleTarget = toggle.Q(null, "unity-toggle__input");
            ControlStyleConfig styleConfig = GetColorStyleConfig(themeMeta, toggle);
            ApplyControlColorConfigToVisualElement(toggle, styleConfig, styleTarget);

            RegisterDefaultButtonSfxCallback(toggle);
        });

        // SlideToggle
        root.Query<SlideToggle>().ForEach(slideToggle =>
        {
            VisualElement styleTarget = slideToggle.Q(null, "slide-toggle__input");
            ControlStyleConfig styleConfig = GetColorStyleConfig(themeMeta, slideToggle);
            ApplyControlColorConfigToVisualElement(slideToggle, styleConfig, styleTarget);
        });

        // Dropdown menus
        if (defaultControlStyleConfig != null)
        {
            List<string> ussClassNamesForApplyButtonColors = new()
            {
                "unity-base-popup-field__input",
                "unity-enum-field__input",
            };
            ussClassNamesForApplyButtonColors.ForEach(ussClassName =>
            {
                root.Query<VisualElement>(null, ussClassName)
                    .ForEach(styleTarget =>
                    {
                        ApplyControlColorConfigToVisualElement(styleTarget, defaultControlStyleConfig);
                    });
            });

            root.Query<DropdownField>().ForEach(RegisterOpenDropdownMenuCallback);
            root.Query<EnumField>().ForEach(RegisterOpenDropdownMenuCallback);
            if (VisualElementUtils.IsDropdownListFocused(uiDocument.rootVisualElement.focusController, out VisualElement unityBaseDropdown))
            {
                unityBaseDropdown.Query<VisualElement>(null, "unity-base-dropdown__container-inner").ForEach(entry =>
                {
                    if (alreadyProcessedVisualElements.Contains(entry))
                    {
                        return;
                    }
                    alreadyProcessedVisualElements.Add(entry);
                    entry.style.backgroundColor = new StyleColor(defaultControlStyleConfig.backgroundColor);
                });
            }
        }

        // ListViews
        root.Query<ListView>().ForEach(listView => ApplyThemeStyleUtils.UpdateStylesOnListViewSelectionChanged(listView));
        root.Query<ListViewH>().ForEach(listView =>
        {
            ApplyThemeStyleUtils.UpdateStylesOnListViewFocusChanged(listView);
            ApplyThemeStyleUtils.UpdateStylesOnListViewSelectionChanged(listView);
        });

        // Panels
        root.Query<VisualElement>(null, "dynamicPanel").ForEach(visualElement =>
        {
            ControlStyleConfig styleConfig = GetColorStyleConfig(themeMeta, visualElement);
            ApplyControlColorConfigToVisualElement(visualElement, styleConfig);
        });

        root.Query<VisualElement>(null, "staticPanel").ForEach(visualElement =>
        {
            ControlStyleConfig styleConfig = GetColorStyleConfig(themeMeta, visualElement);
            ApplyControlColorConfigToVisualElement(visualElement, styleConfig);
        });

        // Remove borders
        List<string> ussClassNamesForRemoveBorder = new()
        {
            "slide-toggle__input",
            "slide-toggle__input-knob",
        };
        ussClassNamesForRemoveBorder.ForEach(ussClassName =>
        {
            root.Query(null,ussClassName).ForEach(it => it.SetBorderColor(Colors.clearBlack));
        });
    }

    private void ApplyTextStyles(ThemeMeta themeMeta, VisualElement root)
    {
        ThemeJson themeJson = themeMeta.ThemeJson;
        ApplyThemeStyleUtils.ApplyPrimaryFontColor(themeJson.primaryFontColor, root);
        ApplyThemeStyleUtils.ApplySecondaryFontColor(themeJson.secondaryFontColor, root);
        ApplyThemeStyleUtils.ApplyWarningFontColor(themeJson.warningFontColor, root);
        ApplyThemeStyleUtils.ApplyErrorFontColor(themeJson.errorFontColor, root);

        // Text shadow of elements without a background
        ApplyThemeStyleUtils.ApplyNoBackgroundInHierarchyTextShadow(themeJson.noBackgroundInHierarchyTextShadow, root);
    }

    private void RegisterDefaultButtonSfxCallback(VisualElement visualElement)
    {
        if (registeredSfxVisualElements.Contains(visualElement))
        {
            return;
        }

        if (visualElement is Button button)
        {
            button.RegisterCallbackButtonTriggered(_ => SfxManager.PlayButtonSound());
        }
        else if (visualElement is Toggle toggle)
        {
            toggle.RegisterValueChangedCallback(_ => SfxManager.PlayButtonSound());
        }
    }

    private bool IsIgnoredScene(EScene currentScene)
    {
        return currentScene
            is EScene.SongEditorScene
            or EScene.CreditsScene;
    }

    private ControlStyleConfig GetColorStyleConfig(ThemeMeta themeMeta, VisualElement visualElement)
    {
        if (visualElement.ClassListContains("transparentButton")
            || visualElement is ToggleButton)
        {
            return ObjectUtils.FirstNonDefault(
                themeMeta.ThemeJson.transparentButton,
                themeMeta.ThemeJson.textOnlyButton,
                themeMeta.ThemeJson.defaultControl);
        }

        if (visualElement.ClassListContains("textOnlyButton"))
        {
            return ObjectUtils.FirstNonDefault(
                themeMeta.ThemeJson.textOnlyButton,
                themeMeta.ThemeJson.transparentButton,
                themeMeta.ThemeJson.defaultControl);
        }

        if (visualElement.ClassListContains("dangerButton"))
        {
            return ObjectUtils.FirstNonDefault(
                themeMeta.ThemeJson.dangerButton,
                themeMeta.ThemeJson.defaultControl);
        }

        if (visualElement.ClassListContains("transparentBackgroundColor"))
        {
            return new ControlStyleConfig()
            {
                backgroundColor = Color.clear,
                hoverBackgroundColor = Color.clear,
                focusBackgroundColor = Color.clear,
                activeBackgroundColor = Color.clear,
                disabledBackgroundColor = Color.clear,
            };
        }

        if (visualElement.ClassListContains("dynamicPanel"))
        {
            return ObjectUtils.FirstNonDefault(
                themeMeta.ThemeJson.dynamicPanel,
                themeMeta.ThemeJson.defaultControl);
        }

        if (visualElement.ClassListContains("staticPanel"))
        {
            return ObjectUtils.FirstNonDefault(
                themeMeta.ThemeJson.staticPanel,
                themeMeta.ThemeJson.defaultControl);
        }

        if (visualElement is SlideToggle)
        {
            return ObjectUtils.FirstNonDefault(
                themeMeta.ThemeJson.slideToggleOff,
                themeMeta.ThemeJson.defaultControl);
        }

        if (visualElement is Toggle)
        {
            return ObjectUtils.FirstNonDefault(
                themeMeta.ThemeJson.toggle,
                themeMeta.ThemeJson.defaultControl);
        }

        return themeMeta.ThemeJson.defaultControl;
    }

    private void RegisterOpenDropdownMenuCallback(VisualElement visualElement)
    {
        if (alreadyProcessedVisualElements.Contains(visualElement))
        {
            return;
        }
        alreadyProcessedVisualElements.Add(visualElement);

        visualElement.RegisterCallback<NavigationSubmitEvent>(evt => OnOpenDropdownMenu(), TrickleDown.TrickleDown);
        visualElement.RegisterCallback<ClickEvent>(evt => OnOpenDropdownMenu(), TrickleDown.TrickleDown);
        visualElement.RegisterCallback<PointerDownEvent>(evt => OnOpenDropdownMenu(), TrickleDown.TrickleDown);
    }

    private void ApplySceneSpecificStyles(
        ThemeMeta themeMeta,
        VisualElement root,
        EScene currentScene)
    {
        if (currentScene is EScene.SingScene)
        {
            themeMeta.ThemeJson.lyricsContainerGradient.IfNotNull(gradient =>
                root.Query(null, "lyricsContainer").ForEach(element =>
                {
                    element.style.backgroundImage = GradientManager.GetGradientTexture(gradient);
                    element.style.backgroundColor = new StyleColor(StyleKeyword.None);
                }));
        }

        if (currentScene is EScene.SongSelectScene)
        {
            themeMeta.ThemeJson.videoPreviewColor.IfNotDefault(color =>
            {
                root.Query(R.UxmlNames.songPreviewVideoImage).ForEach(element =>
                {
                    // Transparency is applied in the fade-in animation
                    element.style.unityBackgroundImageTintColor = new StyleColor(color.WithAlpha(255));
                });
            });
        }
    }

    private bool IsSkipApplyThemeStylesToVisualElement(VisualElement visualElement)
    {
        List<string> ignoredUxmlNamesAndUssClasses = new ()
        {
            "hiddenContinueButton",
            "contextMenuButton",
        };

        foreach (string excludedNameOrClass in ignoredUxmlNamesAndUssClasses)
        {
            if (visualElement.ClassListContains(excludedNameOrClass) || visualElement.name == excludedNameOrClass)
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyControlColorConfigToVisualElement(VisualElement visualElement, ControlStyleConfig controlStyleConfig, VisualElement styleTarget=null)
    {
        if (controlStyleConfig == null
            || alreadyProcessedVisualElements.Contains(visualElement)
            || IsSkipApplyThemeStylesToVisualElement(visualElement))
        {
            return;
        }
        alreadyProcessedVisualElements.Add(visualElement);

        if (styleTarget == null)
        {
            styleTarget = visualElement;
        }
        ApplyThemeStyleUtils.ApplyControlStyles(visualElement, styleTarget, controlStyleConfig);
    }

    private void OnOpenDropdownMenu()
    {
        isDropdownMenuOpened = true;
    }

    public IReadOnlyCollection<Sprite> GetSprites()
    {
        if (dynamicBackgroundStaticImageSprite != null)
        {
            List<Sprite> result = new(loadedSprites);
            result.Add(dynamicBackgroundStaticImageSprite);
            return result;
        }
        else
        {
            return loadedSprites;
        }
    }

    private void DisableDynamicBackground()
    {
        backgroundVideoPlayer.Stop();
        backgroundLightVideoPlayer.Stop();
        backgroundParticleSystem.gameObject.SetActive(false);
    }

    private void DisableStaticBackground()
    {
        Image backgroundImageElement = GetExistingStaticBackgroundElement(StaticBackgroundImageElementName);
        if (backgroundImageElement != null)
        {
            backgroundImageElement.RemoveFromHierarchy();
        }
    }

    public List<Color32> GetMicrophoneColors(ThemeJson themeJson = null)
    {
        if (themeJson == null)
        {
            themeJson = GetCurrentTheme()?.ThemeJson;
        }

        List<Color32> themeMicrophoneColors = themeJson?.microphoneColors;
        if (!themeMicrophoneColors.IsNullOrEmpty())
        {
            return themeMicrophoneColors;
        }

        return new List<Color32>
        {
            Colors.CreateColor("#9E77ED"),
            Colors.CreateColor("#CDF564"),
            Colors.CreateColor("#FF4633"),
            Colors.CreateColor("#519BF6"),
            Colors.CreateColor("#FFC665"),
            Colors.CreateColor("#F673A2"),
            Colors.CreateColor("#3C04F2"),
            Colors.CreateColor("#9FC4D0"),
            Colors.CreateColor("#FFCDD3"),
            Colors.CreateColor("#F7E32C"),
            Colors.CreateColor("#0AEFFF"),
        };
    }

    public Dictionary<ESentenceRating, Color32> GetSentenceRatingColors()
    {
        Dictionary<ESentenceRating, Color32> result = new()
        {
            { ESentenceRating.Perfect, Colors.CreateColor("#3AFF4EAF")},
            { ESentenceRating.Great, Colors.CreateColor("#20CF327F")},
            { ESentenceRating.Good, Colors.CreateColor("#44ABDC7F")},
            { ESentenceRating.NotBad, Colors.CreateColor("#E7B41C7F")},
            { ESentenceRating.Bad, Colors.CreateColor("#961CE77F")},
        };

        Dictionary<string, Color32> ratingNameToColor = GetCurrentTheme()?.ThemeJson?.phraseRatingColors;
        if (ratingNameToColor.IsNullOrEmpty())
        {
            return result;
        }

        ratingNameToColor.ForEach(entry =>
        {
            if (Enum.TryParse(entry.Key, true, out ESentenceRating rating))
            {
                result[rating] = entry.Value;
            }
        });
        return result;
    }

    public Dictionary<string, Color32> GetSongEditorLayerColors()
    {
        Dictionary<string, Color32> result = new()
        {
            { "P1", Colors.CreateColor("#2ecc71")},
            { "P2", Colors.CreateColor("#9b59b6")},
            { "MicRecording", Colors.CreateColor("#1D67C2")},
            { "ButtonRecording", Colors.CreateColor("#138BBA")},
            { "CopyPaste", Colors.CreateColor("#F08080")},
            { "Import", Colors.CreateColor("#0F9799")},
            { "PitchDetection", Colors.CreateColor("#ADBBE0", 200)},
            { "SpeechRecognition", Colors.CreateColor("#D4A994", 200)},
        };

        Dictionary<string, Color32> layerNameToColor = GetCurrentTheme()?.ThemeJson?.songEditorLayerColors;
        if (layerNameToColor.IsNullOrEmpty())
        {
            return result;
        }

        layerNameToColor.ForEach(entry =>
        {
            result[entry.Key] = entry.Value;
        });
        return result;
    }

    public async void ReloadThemes()
    {
        themeMetas.Clear();
        failedToLoadThemeNames.Clear();
        await LoadCurrentThemeAsync();
    }

    private EScene GetCurrentScene()
    {
        return sceneRecipeManager.GetCurrentScene();
    }

    public Color32 GetGoldenColor()
    {
        return GetCurrentTheme().ThemeJson.goldenColor
            .OrIfDefault(defaultGoldenColor);
    }
}

