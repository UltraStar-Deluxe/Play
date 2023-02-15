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

    public Material backgroundMaterial;
    public Material particleMaterial;
    public ParticleSystem backgroundParticleSystem;

    private BackgroundShaderControl backgroundShaderControl;
    public BackgroundShaderControl BackgroundShaderControl
    {
        get
        {
            if (backgroundShaderControl == null)
            {
                backgroundShaderControl = GetComponentInChildren<BackgroundShaderControl>();
            }
            return backgroundShaderControl;
        }
    }

    private Material backgroundMaterialCopy;
    private Material particleMaterialCopy;

    private RenderTexture uiRenderTexture;
    public RenderTexture UiRenderTexture
    {
        get
        {
            if (uiRenderTexture == null)
            {
                uiRenderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            }

            return uiRenderTexture;
        }
    }

    private readonly List<ThemeMeta> themeMetas = new();

    private readonly HashSet<VisualElement> alreadyProcessedVisualElements = new();

    private readonly HashSet<string> failedToLoadThemeNames = new();

    private readonly List<Sprite> loadedSprites = new();

    private bool anyThemeLoaded;

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
        ImageManager.AddSpriteHolder(this);
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

        // UI is rendered into a RenderTexture, which is then blended into the screen using the background shader
        uiDocument.panelSettings.targetTexture = UiRenderTexture;
        BackgroundShaderControl.SetUiRenderTextures(
            UiRenderTexture,
            transitionTexture);

        if (anyThemeLoaded)
        {
            ApplyThemeSpecificStylesToVisualElementsInScene();
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

        EScene currentScene = ESceneUtils.GetCurrentScene();
        if (currentScene == EScene.SongEditorScene)
        {
            // Song editor is out of scope for theming.
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
            ApplyThemeSpecificStylesToVisualElementsInScene();
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

        if (!ThemeMetaUtils.HasStaticBackground(themeMeta, settings))
        {
            DisableStaticBackground();
            return;
        }

        string absoluteFilePath = ThemeMetaUtils.GetAbsoluteFilePath(themeMeta, themeMeta.ThemeJson.staticBackground.imagePath);
        ImageManager.LoadSpriteFromUri(absoluteFilePath, loadedSprite =>
        {
            backgroundElement.style.backgroundImage = new StyleBackground(loadedSprite);
            if (!themeMeta.ThemeJson.staticBackground.scaleMode.IsNullOrEmpty()
                && Enum.TryParse(themeMeta.ThemeJson.staticBackground.scaleMode, out ScaleMode scaleMode))
            {
                backgroundElement.style.unityBackgroundScaleMode = new StyleEnum<ScaleMode>(scaleMode);
            }
        });
    }

    private void ApplyThemeDynamicBackground(ThemeMeta themeMeta)
    {
        if (!ThemeMetaUtils.HasDynamicBackground(themeMeta, settings))
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

        Color patternColor = Color.clear; // default to clear to hide pattern if no file specified or found
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
            ApplicationUtils.GetStreamingAssetsPath(ThemeFolderName),
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

    public void ApplyThemeSpecificStylesToVisualElementsInScene()
    {
        if (settings.DeveloperSettings.disableDynamicThemes)
        {
            DisableDynamicBackground();
            return;
        }

        EScene currentScene = ESceneUtils.GetCurrentScene();
        if (currentScene == EScene.SongEditorScene)
        {
            // Song editor is out of scope for theming.
            return;
        }

        ThemeMeta currentThemeMeta = GetCurrentTheme();
        if(currentThemeMeta == null
           || currentThemeMeta.ThemeJson == null)
        {
            Debug.LogWarning("Not applying theme styles because current theme is null");
            return;
        }

        ApplyThemeStaticBackground(currentThemeMeta);

        VisualElement root = uiDocument.rootVisualElement;

        Color backgroundButtonColor = currentThemeMeta.ThemeJson.buttonMainColor;
        Color backgroundButtonColorHover = Color.Lerp(backgroundButtonColor, Color.white, 0.2f);
        Color backgroundButtonColorFocus = Color.Lerp(backgroundButtonColor, Color.white, 0.2f);
        Color itemPickerBackgroundColor = UIUtils.ColorHSVOffset(backgroundButtonColor, 0, -0.1f, 0.01f);

        Color fontColorAll = currentThemeMeta.ThemeJson.fontColor;
        bool useGlobalFontColor = fontColorAll != Color.clear;

        Color fontColorButtons = useGlobalFontColor ? fontColorAll : currentThemeMeta.ThemeJson.fontColorButtons;
        Color fontColorLabels = useGlobalFontColor ? fontColorAll : currentThemeMeta.ThemeJson.fontColorLabels;

        ControlColorConfig buttonColorConfig = new()
        {
            fontColor = fontColorButtons,
            backgroundColor = backgroundButtonColor,
            hoverBackgroundColor = backgroundButtonColorHover,
            focusBackgroundColor = backgroundButtonColorFocus,
            activeToggleButtonColor = backgroundButtonColorFocus,
        };
        
        ControlColorConfig toggleButtonColorConfig = new()
        {
            fontColor = fontColorButtons,
            backgroundColor = Colors.clear,
            hoverBackgroundColor = backgroundButtonColorHover.WithAlpha(0.5f),
            focusBackgroundColor = backgroundButtonColorFocus.WithAlpha(0.5f),
            activeToggleButtonColor = backgroundButtonColorFocus,
        };

        // Change color of UXML elements:
        root.Query(null, "currentNoteLyrics", "previousNoteLyrics")
            .ForEach(el => el.style.color = backgroundButtonColor);

        root.Query<Button>().ForEach(button =>
        {
            foreach (string excludedNameOrClass in new []{"transparentBackgroundColor", "hiddenContinueButton"})
            {
                if (button.ClassListContains(excludedNameOrClass) || button.name == excludedNameOrClass)
                {
                    return;
                }
            }

            if (alreadyProcessedVisualElements.Contains(button))
            {
                return;
            }
            alreadyProcessedVisualElements.Add(button);

            if (button is ToggleButton)
            {
                UIUtils.SetBackgroundStyleWithHoverAndFocus(button, toggleButtonColorConfig);
            }
            else
            {
                UIUtils.SetBackgroundStyleWithHoverAndFocus(button, buttonColorConfig);
            }

            VisualElement image = button.Q("image");
            if (image != null)
            {
                image.style.unityBackgroundImageTintColor = fontColorButtons;
            }

            VisualElement backImage = button.Q("backImage");
            if (backImage != null)
            {
                backImage.style.unityBackgroundImageTintColor = fontColorButtons;
            }
        });
        root.Query<VisualElement>(null, "unity-toggle__input").ForEach(entry =>
        {
            if (alreadyProcessedVisualElements.Contains(entry))
            {
                return;
            }
            alreadyProcessedVisualElements.Add(entry);
            UIUtils.SetBackgroundStyleWithHoverAndFocus(entry, entry.parent, buttonColorConfig);
        });
        root.Query<VisualElement>("songEntryUiRoot").ForEach(entry =>
        {
            if (alreadyProcessedVisualElements.Contains(entry))
            {
                return;
            }
            alreadyProcessedVisualElements.Add(entry);
            UIUtils.SetBackgroundStyleWithHoverAndFocus(entry, buttonColorConfig);
        });

        UIUtils.ApplyFontColorForElements(root, new []{"Label", "titleImage", "sceneTitle", "sceneSubtitle"}, null, fontColorLabels);
        UIUtils.ApplyFontColorForElements(root, new []{"itemLabel"}, null, fontColorButtons);

        root.Query(null, "itemPickerItemLabel").ForEach(label => label.style.backgroundColor = itemPickerBackgroundColor);
        root.Query("titleImage").ForEach(image => image.style.unityBackgroundImageTintColor = fontColorLabels);
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
}
