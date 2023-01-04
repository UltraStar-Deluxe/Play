using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrimeInputActions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// Handles the loading, saving and application of themes for the app
// This includes the background material shader values, background particle effects, and UIToolkit colors/styles
public class ThemeManager : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        instance = null;
    }

    /**
     * Filename without extension of the theme that should be loaded by default
     */
    public const string DefaultThemeName = "default_blue";

    private const string ThemeFolderName = "Themes";

    private static ThemeManager instance;
    public static ThemeManager Instance
    {
        get
        {
            if (instance == null)
            {
                ThemeManager instanceInScene = GameObjectUtils.FindComponentWithTag<ThemeManager>("ThemeManager");
                if (instanceInScene != null)
                {
                    GameObjectUtils.TryInitSingleInstanceWithDontDestroyOnLoad(ref instance, ref instanceInScene);
                }
            }
            return instance;
        }
    }

    public Material backgroundMaterial;
    public Material particleMaterial;
    public ParticleSystem backgroundParticleSystem;
    public ThemeMeta currentThemeMeta;

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
    private RenderTexture userInterfaceRenderTexture;

    private readonly List<ThemeMeta> themeMetas = new();
    private readonly List<Texture2D> dynamicTextures = new();

    private readonly HashSet<VisualElement> alreadyProcessedVisualElements = new();

    private bool anyThemeLoaded;

    void Awake()
    {
        if (this != Instance)
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        if (this != Instance)
        {
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        if (this != Instance)
        {
            return;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (Instance != this)
        {
            return;
        }

        if (backgroundMaterialCopy == null)
        {
            backgroundMaterialCopy = new Material(backgroundMaterial);
        }

        if (particleMaterialCopy == null)
        {
            particleMaterialCopy = new Material(particleMaterial);
        }

        // UI is rendered into a RenderTexture, which is then blended into the screen using the background shader
        if (userInterfaceRenderTexture == null)
        {
            userInterfaceRenderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        }
        UIDocument uiDocument = GameObjectUtils.FindComponentWithTag<UIDocument>("UIDocument");
        uiDocument.panelSettings.targetTexture = userInterfaceRenderTexture;
        BackgroundShaderControl.SetUiRenderTextures(
            userInterfaceRenderTexture,
            UltraStarPlaySceneChangeAnimationControl.Instance.uiCopyRenderTexture);

        if (!anyThemeLoaded)
        {
            LoadTheme(GetCurrentTheme());
        }
    }

    private void LoadTheme(ThemeMeta themeMeta)
    {
        if (SettingsManager.Instance.Settings.DeveloperSettings.disableDynamicThemes)
        {
            return;
        }

        EScene currentScene = ESceneUtils.GetCurrentScene();
        if (currentScene == EScene.SongEditorScene)
        {
            // Song editor is out of scope for theming.
            return;
        }

        if (themeMeta == null)
        {
            Debug.Log($"Cannot load theme. Theme is null.");
            return;
        }

        anyThemeLoaded = true;

        StartCoroutine(ApplyTheme(themeMeta));
    }

    private void DestroyDynamicTextures()
    {
        foreach (Texture2D texture2D in dynamicTextures)
        {
            Destroy(texture2D);
        }
        dynamicTextures.Clear();
    }

    private IEnumerator ApplyTheme(ThemeMeta themeMeta)
    {
        alreadyProcessedVisualElements.Clear();

        // UIToolkit takes one frame to apply the style changes,
        // we wait so the background changes at the same frame
        yield return null;

        themeMeta.ThemeSettings?.songRatingIcons?.DestroyLoadedSprites();
        DestroyDynamicTextures();

        ApplyThemeDynamicBackground(themeMeta);
    }

    private void ApplyThemeDynamicBackground(ThemeMeta themeMeta)
    {
        DynamicBackground background = themeMeta.ThemeSettings?.dynamicBackground;

        // Material
        if (!string.IsNullOrEmpty(background.gradientRampFile))
        {
            string gradientPath = ThemeMetaUtils.GetAbsoluteFilePath(themeMeta, background.gradientRampFile);
            if (File.Exists(gradientPath))
            {
                TextureWrapMode textureWrapMode = background.gradientScrollingSpeed > 0
                    ? TextureWrapMode.Repeat
                    : TextureWrapMode.Clamp;
                ImageManager.LoadSpriteFromUri(gradientPath, gradientSprite =>
                {
                    gradientSprite.texture.wrapMode = textureWrapMode;
                    backgroundMaterial.SetTexture("_ColorRampTex", gradientSprite.texture);
                });
            }
            else
            {
                Debug.LogError($"[THEME] Gradient Ramp file can't be opened at path: {background.gradientRampFile}");
            }
        }

        if (!string.IsNullOrEmpty(background.gradientType))
        {
            DynamicBackground.GradientType result;
            if (Enum.TryParse(background.gradientType, true, out result))
            {
                backgroundMaterial.SetFloat("_Gradient", (int)result);
            }
        }

        Color patternColor = Color.clear; // default to clear to hide pattern if no file specified or found
        if (!string.IsNullOrEmpty(background.patternFile))
        {
            string patternPath = ThemeMetaUtils.GetAbsoluteFilePath(themeMeta, background.patternFile);
            if (File.Exists(patternPath))
            {
                ImageManager.LoadSpriteFromUri(patternPath, patternSprite =>
                {
                    patternSprite.texture.wrapMode = TextureWrapMode.Repeat;
                    backgroundMaterial.SetTexture("_PatternTex", patternSprite.texture);
                });

                patternColor = background.patternColor;
            }
            else
            {
                Debug.LogError($"[THEME] Pattern file can't be opened at path: {background.patternFile}");
            }
        }

        float screenRatio = Screen.width / (float)Screen.height;
        backgroundMaterial.SetVector("_PatternTex_ST", new Vector4(background.patternScale.x * screenRatio, background.patternScale.y, background.patternScrolling.x, background.patternScrolling.y));
        backgroundMaterial.SetColor("_PatternColor", patternColor);
        backgroundMaterial.SetFloat("_Scale", background.gradientScale);
        backgroundMaterial.SetFloat("_Smoothness", background.gradientSmoothness);
        backgroundMaterial.SetFloat("_Angle", background.gradientAngle);
        backgroundMaterial.SetFloat("_EnableGradientAnimation", background.gradientAnimation ? 1 : 0);
        backgroundMaterial.SetFloat("_GradientAnimSpeed", background.gradientAnimSpeed);
        backgroundMaterial.SetFloat("_GradientAnimAmp", background.gradientAnimAmplitude);
        backgroundMaterial.SetFloat("_ColorRampScrolling", background.gradientScrollingSpeed);
        backgroundMaterial.SetFloat("_UiShadowOpacity", background.uiShadowOpacity);
        backgroundMaterial.SetVector("_UiShadowOffset", background.uiShadowOffset);

        if (background.uiShadowOpacity > 0)
        {
            backgroundMaterial.EnableKeyword("_UI_SHADOW");
        }
        else
        {
            backgroundMaterial.DisableKeyword("_UI_SHADOW");
        }

        // Particles
        if (!string.IsNullOrEmpty(background.particleFile))
        {
            string particlePath = ThemeMetaUtils.GetAbsoluteFilePath(themeMeta, background.particleFile);
            if (File.Exists(particlePath))
            {
                ImageManager.LoadSpriteFromUri(particlePath, particleSprite =>
                {
                    particleSprite.texture.wrapMode = TextureWrapMode.Clamp;
                    particleMaterial.mainTexture = particleSprite.texture;
                });
            }
            else
            {
                Debug.LogError($"[THEME] Particle file can't be opened at path: {background.particleFile}");
            }
        }

        ParticleSystem.MainModule main = backgroundParticleSystem.main;
        main.startColor = new Color(1, 1, 1, background.particleOpacity);

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
        Destroy(userInterfaceRenderTexture);

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

        DestroyDynamicTextures();
        currentThemeMeta?.ThemeSettings?.songRatingIcons?.DestroyLoadedSprites();
    }

    public void SetCurrentTheme(ThemeMeta themeMeta)
    {
        SettingsManager.Instance.Settings.GraphicSettings.themeName = themeMeta.FileNameWithoutExtension;
        LoadTheme(GetCurrentTheme());
    }

    public ThemeMeta GetCurrentTheme()
    {
        return GetThemeByName(SettingsManager.Instance.Settings.GraphicSettings.themeName);
    }

    public ThemeMeta GetThemeByName(string themeName)
    {
        ThemeMeta themeMeta = GetThemeMetas()
            .FirstOrDefault(themeMeta => themeMeta.FileNameWithoutExtension == themeName);
        if (themeMeta == null)
        {
            Debug.Log($"No theme found with name {themeName}. Using default theme instead.");
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
            $"{Application.persistentDataPath}/{ThemeFolderName}",
            $"{Application.streamingAssetsPath}/{ThemeFolderName}",
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

        Debug.Log($"Found {themeMetas.Count} themes.");

        return themeMetas;
    }

    public void UpdateThemeSpecificStyleSheets()
    {
        if (SettingsManager.Instance.Settings.DeveloperSettings.disableDynamicThemes
            || currentThemeMeta == null
            || currentThemeMeta.ThemeSettings == null)
        {
            return;
        }

        UIDocument uiDocument = GameObjectUtils.FindComponentWithTag<UIDocument>("UIDocument");
        if (uiDocument == null)
        {
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        Color backgroundButtonColor = currentThemeMeta.ThemeSettings.buttonMainColor;
        Color backgroundButtonColorHover = Color.Lerp(backgroundButtonColor, Color.white, 0.2f);
        Color itemPickerBackgroundColor = UIUtils.ColorHSVOffset(backgroundButtonColor, 0, -0.1f, 0.01f);

        Color fontColorAll = currentThemeMeta.ThemeSettings.fontColor;
        bool useGlobalFontColor = fontColorAll != Color.clear;

        Color fontColorButtons = useGlobalFontColor ? fontColorAll : currentThemeMeta.ThemeSettings.fontColorButtons;
        Color fontColorLabels = useGlobalFontColor ? fontColorAll : currentThemeMeta.ThemeSettings.fontColorLabels;

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

            UIUtils.SetBackgroundStyleWithHover(button, backgroundButtonColor, backgroundButtonColorHover, fontColorButtons);

            VisualElement image = button.Q("image");
            if (image != null) image.style.unityBackgroundImageTintColor = fontColorButtons;
            VisualElement backImage = button.Q("backImage");
            if (backImage != null) backImage.style.unityBackgroundImageTintColor = fontColorButtons;
        });
        root.Query<VisualElement>(null, "unity-toggle__checkmark").ForEach(entry =>
        {
            if (alreadyProcessedVisualElements.Contains(entry))
            {
                return;
            }
            alreadyProcessedVisualElements.Add(entry);
            UIUtils.SetBackgroundStyleWithHover(entry, entry.parent, backgroundButtonColor, backgroundButtonColorHover, fontColorButtons);
        });
        root.Query<VisualElement>("songEntryUiRoot").ForEach(entry =>
        {
            if (alreadyProcessedVisualElements.Contains(entry))
            {
                return;
            }
            alreadyProcessedVisualElements.Add(entry);
            UIUtils.SetBackgroundStyleWithHover(entry, backgroundButtonColor, backgroundButtonColorHover, fontColorButtons);
        });

        UIUtils.ApplyFontColorForElements(root, new []{"Label", "titleImage", "sceneTitle", "sceneSubtitle"}, null, fontColorLabels);
        UIUtils.ApplyFontColorForElements(root, new []{"itemLabel"}, null, fontColorButtons);

        root.Query(null, "itemPickerItemLabel").ForEach(label => label.style.backgroundColor = itemPickerBackgroundColor);
        root.Query("titleImage").ForEach(image => image.style.unityBackgroundImageTintColor = fontColorLabels);
    }
}
