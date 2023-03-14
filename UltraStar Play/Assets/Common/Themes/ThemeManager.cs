using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

// Handles the loading, saving and application of themes for the app
// This includes the background material shader values, background particle effects, and UIToolkit colors/styles
public class ThemeManager : AbstractSingletonBehaviour, ISpriteHolder, INeedInjection
{
    /**
     * Filename without extension of the theme that should be loaded by default
     */
    public const string DefaultThemeName = "default_dark";
    private const string ThemeFolderName = "Themes";
    private const float DefaultSceneChangeAnimationTimeInSeconds = 0.25f;

    public static ThemeManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<ThemeManager>();

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
    
    private Material backgroundMaterialCopy;
    private Material particleMaterialCopy;

    private RenderTexture uiRenderTexture;
    public RenderTexture UiRenderTexture
    {
        get
        {
            if (uiRenderTexture != null
                && (uiRenderTexture.width != Screen.width
                    || uiRenderTexture.height != Screen.height))
            {
                Debug.Log("Recreate uiRenderTexture because of screen size change.");
                Destroy(uiRenderTexture);
                uiRenderTexture = null;
            }
            
            if (uiRenderTexture == null)
            {
                uiRenderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            }

            return uiRenderTexture;
        }
    }
    
    private RenderTexture particleRenderTexture;
    public RenderTexture ParticleRenderTexture
    {
        get
        {
            if (particleRenderTexture != null
                 && (particleRenderTexture.width != Screen.width
                     || particleRenderTexture.height != Screen.height))
            {
                Debug.Log("Recreate particleRenderTexture because of screen size change.");
                Destroy(particleRenderTexture);
                particleRenderTexture = null;
            }
            
            if (particleRenderTexture == null)
            {
                particleRenderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            }

            return particleRenderTexture;
        }
    }

    private readonly List<ThemeMeta> themeMetas = new();

    private readonly HashSet<VisualElement> alreadyProcessedVisualElements = new();

    private readonly HashSet<string> failedToLoadThemeNames = new();

    private readonly List<Sprite> loadedSprites = new();

    private bool anyThemeLoaded;

    private bool isDropdownMenuOpened;
    
    [Inject]
    private Settings settings;

    [Inject]
    private UIDocument uiDocument;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        DirectoryUtils.CreateDirectory(GetAbsoluteUserDefinedThemesFolder());
        ImageManager.AddSpriteHolder(this);
    }

    protected void LateUpdate()
    {
        // Use LateUpdate to apply theme styles
        // because this works for newly opened DropdownMenus in the same frame.
        if (isDropdownMenuOpened)
        {
            isDropdownMenuOpened = false;
            ApplyThemeSpecificStylesToVisualElements(uiDocument.rootVisualElement.focusController.focusedElement as VisualElement);
        }
    }

    public void UpdateSceneTextures(Texture transitionTexture)
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
            backgroundParticlesCamera.targetTexture = ParticleRenderTexture;
            uiDocument.panelSettings.targetTexture = UiRenderTexture;
            backgroundShaderControl.SetUiRenderTextures(
                UiRenderTexture,
                ParticleRenderTexture,
                transitionTexture);
        }
        else
        {
            // The UIDocument is rendered directly to the screen by Unity.
            uiDocument.panelSettings.targetTexture = null;
            backgroundParticlesCamera.gameObject.SetActive(false);
        }

        if (anyThemeLoaded)
        {
            ApplyThemeSpecificStylesToVisualElements(uiDocument.rootVisualElement);
        }
        else
        {
            LoadCurrentTheme();
        }
    }

    private void LoadCurrentTheme()
    {
        if (settings.DeveloperSettings.disableDynamicThemes)
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
        StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(0, () =>
        {
            alreadyProcessedVisualElements.Clear();
            ApplyThemeBackground(themeMeta);
            ApplyThemeSpecificStylesToVisualElements(uiDocument.rootVisualElement);
            anyThemeLoaded = true;
        }));
    }

    private void ApplyThemeBackground(ThemeMeta themeMeta)
    {
        ApplyThemeStaticBackground(themeMeta);
        ApplyThemeDynamicBackground(themeMeta);
    }

    private void ApplyThemeStaticBackground(ThemeMeta themeMeta)
    {
        VisualElement backgroundElement = uiDocument.rootVisualElement;
        if (backgroundElement == null)
        {
            return;
        }

        EScene currentScene = GetCurrentScene();
        if (!ThemeMetaUtils.HasStaticBackground(themeMeta, settings, currentScene))
        {
            DisableStaticBackground();
            return;
        }
        
        StaticBackgroundJson staticBackgroundJson = ThemeMetaUtils.GetStaticBackgroundJsonForScene(themeMeta, currentScene);
        string absoluteFilePath = ThemeMetaUtils.GetAbsoluteFilePath(themeMeta, staticBackgroundJson.imagePath);
        ImageManager.LoadSpriteFromUri(absoluteFilePath, loadedSprite =>
        {
            backgroundElement.style.backgroundImage = new StyleBackground(loadedSprite);
            if (!staticBackgroundJson.scaleMode.IsNullOrEmpty()
                && Enum.TryParse(staticBackgroundJson.scaleMode, out ScaleMode scaleMode))
            {
                backgroundElement.style.unityBackgroundScaleMode = new StyleEnum<ScaleMode>(scaleMode);
            }
        });
    }

    private void ApplyThemeDynamicBackground(ThemeMeta themeMeta)
    {
        EScene currentScene = GetCurrentScene();
        if (!ThemeMetaUtils.HasDynamicBackground(themeMeta, settings, currentScene))
        {
            DisableDynamicBackground();
            return;
        }

        DynamicBackgroundJson backgroundJson = themeMeta.ThemeJson?.dynamicBackground;
        if (backgroundJson == null)
        {
            backgroundJson = new();
        }

        // Material
        if (!backgroundJson.gradientRampFile.IsNullOrEmpty())
        {
            string gradientPath = ThemeMetaUtils.GetAbsoluteFilePath(themeMeta, backgroundJson.gradientRampFile);
            if (File.Exists(gradientPath))
            {
                TextureWrapMode textureWrapMode = backgroundJson.gradientScrollingSpeed > 0
                    ? TextureWrapMode.Repeat
                    : TextureWrapMode.Clamp;
                ImageManager.LoadSpriteFromFile(gradientPath, gradientSprite =>
                {
                    loadedSprites.Add(gradientSprite);
                    gradientSprite.texture.wrapMode = textureWrapMode;
                    backgroundMaterial.SetTexture("_ColorRampTex", gradientSprite.texture);
                });
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
                ImageManager.LoadSpriteFromFile(patternPath, patternSprite =>
                {
                    loadedSprites.Add(patternSprite);
                    patternSprite.texture.wrapMode = TextureWrapMode.Repeat;
                    backgroundMaterial.SetTexture("_PatternTex", patternSprite.texture);
                });

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
            string particlePath = ThemeMetaUtils.GetAbsoluteFilePath(themeMeta, backgroundJson.particleFile);
            if (File.Exists(particlePath))
            {
                ImageManager.LoadSpriteFromFile(particlePath, particleSprite =>
                {
                    loadedSprites.Add(particleSprite);
                    particleSprite.texture.wrapMode = TextureWrapMode.Clamp;
                    particleMaterial.mainTexture = particleSprite.texture;
                });
            }
            else
            {
                Debug.LogError($"[THEME] Particle file can't be opened at path: {backgroundJson.particleFile}");
            }
        }
        else
        {
            backgroundParticleSystem.gameObject.SetActive(false);
        }

        ParticleSystem.MainModule main = backgroundParticleSystem.main;
        main.startColor = new Color(1, 1, 1, backgroundJson.particleOpacity);

        backgroundParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        backgroundParticleSystem.Play();
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        // Destroy all instantiated assets
        Destroy(uiRenderTexture);

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
    }

    public void SetCurrentTheme(ThemeMeta themeMeta)
    {
        settings.GraphicSettings.themeName = themeMeta.FileNameWithoutExtension;
        LoadCurrentTheme();
    }

    public ThemeMeta GetCurrentTheme()
    {
        return GetThemeByName(settings.GraphicSettings.themeName);
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
            string availableThemeMetasCsv = GetThemeMetas().Select(themeMeta => themeMeta.FileNameWithoutExtension).ToCsv();
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

        List<string> themeFolders = new List<string>
        {
            GetAbsoluteDefaultThemesFolder(),
            GetAbsoluteUserDefinedThemesFolder(),
        };

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

        string themeNamesCsv = themeMetas.Select(themeMeta => themeMeta.FileNameWithoutExtension).ToCsv();
        Debug.Log($"Found {themeMetas.Count} themes: {themeNamesCsv}");

        return themeMetas;
    }

    public static void ApplyThemeSpecificStylesToVisualElements(VisualElement root)
    {
        if (root == null)
        {
            return;
        }
        // using DisposableStopwatch d = new("ApplyThemeSpecificStylesToVisualElements took <ms>");
        
        ThemeManager themeManager = Instance;
        if (themeManager != null)
        {
            themeManager.DoApplyThemeSpecificStylesToVisualElements(root);
        }
    }
    
    private void DoApplyThemeSpecificStylesToVisualElements(VisualElement root)
    {
        if (!applyThemeSpecificStyles)
        {
            return;
        }

        if (settings.DeveloperSettings.disableDynamicThemes)
        {
            DisableDynamicBackground();
            return;
        }

        EScene currentScene = GetCurrentScene();
        if (IsIgnoredScene(currentScene))
        {
            // Song editor is out of scope for theming.
            return;
        }

        ThemeMeta themeMeta = GetCurrentTheme();
        if(themeMeta == null
           || themeMeta.ThemeJson == null)
        {
            Debug.LogWarning("Not applying theme styles because current theme is null");
            return;
        }
        ThemeJson themeJson = themeMeta.ThemeJson;
        
        ApplyThemeStaticBackground(themeMeta);


        ControlStyleConfig defaultControlStyleConfig = themeMeta.ThemeJson.defaultControl;
        
        // Scene specific elements
        ApplyThemeSpecificStylesToVisualElements(root, themeMeta, GetCurrentScene());
        
        // Basic font colors
        ApplyThemeStyleUtils.ApplyPrimaryFontColor(themeJson.primaryFontColor, root);
        ApplyThemeStyleUtils.ApplySecondaryFontColor(themeJson.secondaryFontColor, root);
        ApplyThemeStyleUtils.ApplyWarningFontColor(themeJson.warningFontColor, root);
        ApplyThemeStyleUtils.ApplyErrorFontColor(themeJson.errorFontColor, root);
        
        // Buttons
        root.Query<Button>().ForEach(button =>
        {
            ControlStyleConfig styleConfig = GetColorStyleConfig(themeMeta, button);
            ApplyControlColorConfigToVisualElement(button, styleConfig);
        });

        // ItemPickers
        if (defaultControlStyleConfig != null)
        {
            root.Query(null, "itemPickerControlsRow").ForEach(controlsRow =>
            {
                defaultControlStyleConfig.backgroundColor.IfNotDefault(backgroundColor =>
                {
                    controlsRow.style.backgroundColor = new StyleColor(backgroundColor);
                });
                
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
        
        if (visualElement.ClassListContains("lightButton"))
        {
            return ObjectUtils.FirstNonDefault(
                themeMeta.ThemeJson.lightButton,
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

    private void ApplyThemeSpecificStylesToVisualElements(
        VisualElement root,
        ThemeMeta themeMeta,
        EScene currentScene)
    {
        ThemeMeta currentThemeMeta = GetCurrentTheme();

        if (currentScene is EScene.SingScene)
        {
            currentThemeMeta.ThemeJson.lyricsContainerGradient.IfNotNull(gradient =>
                root.Query(null, "lyricsContainer").ForEach(element =>
                {
                    element.style.backgroundImage = GradientManager.GetGradientTexture(gradient);
                    element.style.backgroundColor = new StyleColor(StyleKeyword.None);
                }));
            
            currentThemeMeta.ThemeJson.primaryFontColor.IfNotDefault(color =>
                root.Query(R.UxmlNames.timeBarPositionIndicator).ForEach(it => it.style.backgroundColor = new StyleColor(color)));
        }
    }

    private bool IsSkipApplyThemeStylesToVisualElement(VisualElement visualElement)
    {
        List<string> ignoredUxmlNamesAndUssClasses = new ()
        {
            "hiddenContinueButton"
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

    public float GetSceneChangeAnimationTimeInSeconds()
    {
        string sceneChangeAnimationTimeString = GetCurrentTheme().ThemeJson.sceneTransitionAnimationTime;
        if (!sceneChangeAnimationTimeString.IsNullOrEmpty()
            && TimeUtils.TryParseDuration(sceneChangeAnimationTimeString, out long parsedDurationInMilliseconds))
        {
            return parsedDurationInMilliseconds / 1000f;
        }

        return DefaultSceneChangeAnimationTimeInSeconds;
    }

    public IReadOnlyCollection<Sprite> GetSprites()
    {
        return loadedSprites;
    }

    public static string GetAbsoluteUserDefinedThemesFolder()
    {
        return $"{Application.persistentDataPath}/{ThemeFolderName}";
    }

    public static string GetAbsoluteDefaultThemesFolder()
    {
        return ApplicationUtils.GetStreamingAssetsPath(ThemeFolderName);
    }
    
    private void DisableDynamicBackground()
    {
        backgroundShaderControl.DisableShader();
        backgroundParticleSystem.gameObject.SetActive(false);
    }

    private void DisableStaticBackground()
    {
        uiDocument.rootVisualElement.style.backgroundImage = new StyleBackground();
    }

    public List<Color32> GetMicrophoneColors()
    {
        List<Color32> themeMicrophoneColors = GetCurrentTheme()?.ThemeJson?.microphoneColors;
        if (!themeMicrophoneColors.IsNullOrEmpty())
        {
            return themeMicrophoneColors;
        }

        return new List<Color32>
        {
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
            { ESentenceRating.Good, Colors.CreateColor("#E7B41C7F")},
            { ESentenceRating.NotBad, Colors.CreateColor("#44ABDC7F")},
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
            { "MidiFile", Colors.CreateColor("#0F9799")},
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

    public void ReloadThemes()
    {
        themeMetas.Clear();
        LoadCurrentTheme();
    }

    private EScene GetCurrentScene()
    {
        return sceneRecipeManager.GetCurrentScene();
    }
}
