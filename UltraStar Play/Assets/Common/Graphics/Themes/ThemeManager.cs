using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

// Handles the loading, saving and application of themes for the app
// This includes the background material shader values, background particle effects, and UIToolkit colors/styles

public class ThemeManager : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    // the theme to load by default (filename without json extension)
    public const string DEFAULT_THEME_NAME = "default_blue";
    public const string THEME_FOLDER_NAME = "Themes";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        instance = null;
    }

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

    // ----------------------------------------------------------------

    [Inject]
    private Settings settings;

    [Serializable]
    public class ThemeSettings
    {
        // These correspond to the possible JSON properties defined in a theme file.
        // Eventually a theme builder UI can be considered, but meanwhile this will
        // have to be written manually with a text editor.
        // See the files in the "themes" folder at the root of the project/build.

        public DynamicBackground dynamicBackground;
        public SongRatingIcons songRatingIcons;
        public Color buttonMainColor;
        public Color fontColorButtons;
        public Color fontColorLabels;
        public Color fontColor;

        public static ThemeSettings LoadFromJson(string json)
        {
            json = PreprocessJson(json);
            ThemeSettings theme = JsonUtility.FromJson<ThemeSettings>(json);
            if (theme == null)
            {
                throw new Exception("Couldn't parse supplied JSON as ThemeSettings data.");
            }
            return theme;
        }

        public bool GetRatingIconFor(ThemeMeta themeMeta, SongRating.ESongRating songRating, out Sprite sprite)
        {
            sprite = songRatingIcons?.GetSpriteForRating(themeMeta, songRating);
            return sprite != null;
        }

        // Process JSON to parse certain values, e.g. allow hex colors and convert
        // them to proper color struct notation
        static string PreprocessJson(string input)
        {
            // Convert hex colors to proper JSON color struct
            Regex hexRgbaDouble = new Regex(@"""#([0-9A-Fa-f][0-9A-Fa-f])([0-9A-Fa-f][0-9A-Fa-f])([0-9A-Fa-f][0-9A-Fa-f])([0-9A-Fa-f][0-9A-Fa-f])""");
            Regex hexRgbDouble = new Regex(@"""#([0-9A-Fa-f][0-9A-Fa-f])([0-9A-Fa-f][0-9A-Fa-f])([0-9A-Fa-f][0-9A-Fa-f])""");
            Regex hexRgba = new Regex(@"""#([0-9A-Fa-f])([0-9A-Fa-f])([0-9A-Fa-f])([0-9A-Fa-f])""");
            Regex hexRgb = new Regex(@"""#([0-9A-Fa-f])([0-9A-Fa-f])([0-9A-Fa-f])""");

            int offset = 0;
            hexRgbaDouble.Matches(input).ForEach(match =>
            {
                string replacement = $"{{ \"r\":{HexToFloatStr(match.Groups[1].Value)}, \"g\":{HexToFloatStr(match.Groups[2].Value)}, \"b\":{HexToFloatStr(match.Groups[3].Value)}, \"a\":{HexToFloatStr(match.Groups[4].Value)} }}";
                input = input.Remove(match.Index + offset, match.Length).Insert(match.Index + offset, replacement);
                offset += replacement.Length - match.Length;
            });
            offset = 0;
            hexRgbDouble.Matches(input).ForEach(match =>
            {
                string replacement = $"{{ \"r\":{HexToFloatStr(match.Groups[1].Value)}, \"g\":{HexToFloatStr(match.Groups[2].Value)}, \"b\":{HexToFloatStr(match.Groups[3].Value)}, \"a\":1.0 }}";
                input = input.Remove(match.Index + offset, match.Length).Insert(match.Index + offset, replacement);
                offset += replacement.Length - match.Length;
            });
            offset = 0;
            hexRgba.Matches(input).ForEach(match =>
            {
                string replacement = $"{{ \"r\":{HexToFloatStr(match.Groups[1].Value, true)}, \"g\":{HexToFloatStr(match.Groups[2].Value, true)}, \"b\":{HexToFloatStr(match.Groups[3].Value, true)}, \"a\":{HexToFloatStr(match.Groups[4].Value, true)} }}";
                input = input.Remove(match.Index + offset, match.Length).Insert(match.Index + offset, replacement);
                offset += replacement.Length - match.Length;
            });
            offset = 0;
            hexRgb.Matches(input).ForEach(match =>
            {
                string replacement = $"{{ \"r\":{HexToFloatStr(match.Groups[1].Value, true)}, \"g\":{HexToFloatStr(match.Groups[2].Value, true)}, \"b\":{HexToFloatStr(match.Groups[3].Value, true)}, \"a\":1.0 }}";
                input = input.Remove(match.Index + offset, match.Length).Insert(match.Index + offset, replacement);
                offset += replacement.Length - match.Length;
            });

            return input;
        }

        static string HexToFloatStr(string hex, bool singleDigit = false)
        {
            return (Convert.ToInt32(singleDigit ? $"{hex}{hex}" : hex, 16)/255.0f).ToString(CultureInfo.InvariantCulture);
        }
    }

    [Serializable]
    public class SongRatingIcons
    {
        public string toneDeaf;
        public string amateur;
        public string wannabe;
        public string hopeful;
        public string risingStar;
        public string leadSinger;
        public string superstar;
        public string ultrastar;

        Dictionary<string, Sprite> loadedSprites = new Dictionary<string, Sprite>();

        public Sprite GetSpriteForRating(ThemeMeta themeMeta, SongRating.ESongRating songRating)
        {
            switch (songRating)
            {
                case SongRating.ESongRating.ToneDeaf: return GetSprite(themeMeta, toneDeaf);
                case SongRating.ESongRating.Amateur: return GetSprite(themeMeta, amateur);
                case SongRating.ESongRating.Wannabe: return GetSprite(themeMeta, wannabe);
                case SongRating.ESongRating.Hopeful: return GetSprite(themeMeta, hopeful);
                case SongRating.ESongRating.RisingStar: return GetSprite(themeMeta, risingStar);
                case SongRating.ESongRating.LeadSinger: return GetSprite(themeMeta, leadSinger);
                case SongRating.ESongRating.Superstar: return GetSprite(themeMeta, superstar);
                case SongRating.ESongRating.Ultrastar: return GetSprite(themeMeta, ultrastar);
                default:
                    throw new ArgumentOutOfRangeException(nameof(songRating), songRating, null);
            }
        }

        private Sprite GetSprite(ThemeMeta themeMeta, string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }

            if (loadedSprites.ContainsKey(filePath))
            {
                return loadedSprites[filePath];
            }

            string fullPath = ThemeMetaUtils.GetAbsoluteFilePath(themeMeta, filePath);
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"[THEME] Couldn't load image at path: '{fullPath}'");
                return null;
            }

            byte[] imageBytes = File.ReadAllBytes(fullPath);
            Texture2D texture = new(2, 2, TextureFormat.RGBA32, true)
            {
                alphaIsTransparency = true,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            texture.LoadImage(imageBytes, true);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        }

        internal void DestroyLoadedSprites()
        {
            foreach (KeyValuePair<string,Sprite> loadedSprite in loadedSprites)
            {
                Destroy(loadedSprite.Value);
            }
            loadedSprites.Clear();
        }
    }

    [Serializable]
    public class DynamicBackground
    {
        public enum GradientType
        {
            Radial = 0,
            RadialRepeated = 1,
            Linear = 2,
            Reflected = 3,
            Repeated = 4
        }

        // Material
        public string gradientRampFile = null;
        public string gradientType = "Radial";
        public float gradientScrollingSpeed = 0;
        public float gradientScale = 1.0f;
        public float gradientSmoothness = 1.0f;
        public float gradientAngle = 0.0f;
        public bool gradientAnimation = false;
        public float gradientAnimSpeed = 1.0f;
        public float gradientAnimAmplitude = 0.1f;
        public string patternFile;
        public Color patternColor = Color.white;
        public Vector2 patternScale = Vector2.one;
        public Vector2 patternScrolling = Vector2.zero;
        public float uiShadowOpacity = 0;
        public Vector2 uiShadowOffset;
        // Particles
        public string particleFile = null;
        public float particleOpacity = 0f;
        // TODO particle movement pattern, based on an enum that will correspond to different prefabs
    }

    // ----------------------------------------------------------------

    public void LoadTheme(ThemeMeta themeMeta)
    {
        // if (settings.DeveloperSettings.disableDynamicThemes)
        // {
        //     return;
        // }
        //
        // EScene currentScene = ESceneUtils.GetCurrentScene();
        // if (currentScene == EScene.SongEditorScene)
        // {
        //     // Song editor is out of scope for theming.
        //     return;
        // }

        if (themeMeta == null)
        {
            Debug.Log($"Cannot load theme. Theme is null.");
            return;
        }

        Debug.Log($"Loading theme '{themeMeta.FileNameWithoutExtension}'");
        string jsonTheme = File.ReadAllText(themeMeta.AbsoluteFilePath);
        currentThemeSettings = ThemeSettings.LoadFromJson(jsonTheme);

        StartCoroutine(Apply(themeMeta, currentThemeSettings));
    }

    // ----------------------------------------------------------------

    public Material backgroundMaterial;
    public Material particleMaterial;
    public ParticleSystem backgroundParticleSystem;
    public ThemeSettings currentThemeSettings;

    [Inject]
    private UIDocument uiDocument;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    public BackgroundImageControl backgroundImageControl { get; private set; }

    private readonly List<Texture2D> dynamicTextures = new();
    private Material backgroundMaterialCopy;
    private Material particleMaterialCopy;
    private RenderTexture userInterfaceRenderTexture;

    public void OnInjectionFinished()
    {
        // UI is rendered into a RenderTexture, which is then blended into the screen using the background shader
        userInterfaceRenderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        uiDocument.panelSettings.targetTexture = userInterfaceRenderTexture;
        backgroundImageControl.SetUiRenderTextures(
            userInterfaceRenderTexture,
            UltraStarPlaySceneChangeAnimationControl.Instance.uiCopyRenderTexture);

        backgroundMaterialCopy = new Material(backgroundMaterial);
        particleMaterialCopy = new Material(particleMaterial);
    }

    void Start()
    {
        if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        LoadTheme(GetCurrentTheme());
    }

    Texture2D LoadPng(string fullPath, bool alpha = true, TextureWrapMode wrapMode = TextureWrapMode.Clamp, bool mipMaps = true)
    {
        byte[] imageBytes = File.ReadAllBytes(fullPath);
        Texture2D texture = new (2, 2, alpha ? TextureFormat.RGBA32 : TextureFormat.RGB24, mipMaps)
        {
            alphaIsTransparency = true,
            wrapMode = wrapMode,
            filterMode = FilterMode.Bilinear
        };
        texture.LoadImage(imageBytes, true);

        dynamicTextures.Add(texture);

        return texture;
    }

    private void DestroyDynamicTextures()
    {
        foreach (Texture2D texture2D in dynamicTextures)
        {
            Destroy(texture2D);
        }
        dynamicTextures.Clear();
    }

    // Updating background colors might be called multiple times
    internal static readonly HashSet<VisualElement> AlreadyProcessedElements = new ();

    private IEnumerator Apply(ThemeMeta themeMeta, ThemeSettings themeSettings)
    {
        AlreadyProcessedElements.Clear();

        // UIToolkit takes one frame to apply the style changes,
        // we wait so the background changes at the same frame
        yield return null;

        this.currentThemeSettings?.songRatingIcons?.DestroyLoadedSprites();
        DestroyDynamicTextures();

        #region Dynamic background

        DynamicBackground background = themeSettings.dynamicBackground;

        // Material
        if (!string.IsNullOrEmpty(background.gradientRampFile))
        {
            string gradientPath = ThemeMetaUtils.GetAbsoluteFilePath(themeMeta, background.gradientRampFile);
            if (File.Exists(gradientPath))
            {
                Texture2D gradientTexture = LoadPng(gradientPath, false, background.gradientScrollingSpeed > 0 ? TextureWrapMode.Repeat : TextureWrapMode.Clamp, false);
                backgroundMaterial.SetTexture("_ColorRampTex", gradientTexture);
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
                Texture2D patternTexture = LoadPng(patternPath, true, TextureWrapMode.Repeat, true);
                backgroundMaterial.SetTexture("_PatternTex", patternTexture);
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

        if (background.uiShadowOpacity > 0) backgroundMaterial.EnableKeyword("_UI_SHADOW");
        else
            backgroundMaterial.DisableKeyword("_UI_SHADOW");

        // Particles
        if (!string.IsNullOrEmpty(background.particleFile))
        {
            string particlePath = ThemeMetaUtils.GetAbsoluteFilePath(themeMeta, background.particleFile);
            if (File.Exists(particlePath))
            {
                Texture2D particleTexture = LoadPng(particlePath, true, TextureWrapMode.Clamp, true);
                particleMaterial.mainTexture = particleTexture;
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

        #endregion
    }

    static readonly int _TimeApplication = Shader.PropertyToID("_TimeApplication");
    void Update()
    {
        // Use that in shaders instead of _Time so that value doesn't reset on each scene change
        Shader.SetGlobalFloat(_TimeApplication, Time.time);
    }

    private void OnDestroy()
    {
        // Destroy all instantiated assets
        Destroy(userInterfaceRenderTexture);

        backgroundMaterial.CopyPropertiesFromMaterial(backgroundMaterialCopy);
        particleMaterial.CopyPropertiesFromMaterial(particleMaterialCopy);
        Destroy(backgroundMaterialCopy);
        Destroy(particleMaterialCopy);

        DestroyDynamicTextures();
        currentThemeSettings?.songRatingIcons?.DestroyLoadedSprites();
    }

    public void SetCurrentTheme(ThemeMeta themeMeta)
    {
        settings.GraphicSettings.themeName = themeMeta.FileNameWithoutExtension;
        LoadTheme(GetCurrentTheme());
    }

    public ThemeMeta GetCurrentTheme()
    {
        return GetThemeByName(settings.GraphicSettings.themeName);
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
            .FirstOrDefault(themeMeta => themeMeta.FileNameWithoutExtension == DEFAULT_THEME_NAME);

        if (defaultThemeMeta == null)
        {
            string availableThemeMetasCsv = GetThemeMetas().Select(themeMeta => themeMeta.FileNameWithoutExtension).ToCsv();
            Debug.LogError($"Default theme '{DEFAULT_THEME_NAME}' not found. Available themes: {availableThemeMetasCsv}");
        }

        return defaultThemeMeta;
    }

    private readonly List<ThemeMeta> themeMetas = new();

    public List<ThemeMeta> GetThemeMetas()
    {
        if (!themeMetas.IsNullOrEmpty())
        {
            return themeMetas;
        }

        List<string> themeFolders = new List<string>
        {
            $"{Application.persistentDataPath}/{THEME_FOLDER_NAME}",
            $"{Application.streamingAssetsPath}/{THEME_FOLDER_NAME}",
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
        if (SettingsManager.Instance.Settings.DeveloperSettings.disableDynamicThemes)
        {
            return;
        }

        if (uiDocument == null)
        {
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        Color backgroundButtonColor = currentThemeSettings.buttonMainColor;
        Color backgroundButtonColorHover = Color.Lerp(backgroundButtonColor, Color.white, 0.2f);
        Color itemPickerBackgroundColor = UIUtils.ColorHSVOffset(backgroundButtonColor, 0, -0.1f, 0.01f);

        Color fontColorAll = currentThemeSettings.fontColor;
        bool useGlobalFontColor = fontColorAll != Color.clear;

        Color fontColorButtons = useGlobalFontColor ? fontColorAll : currentThemeSettings.fontColorButtons;
        Color fontColorLabels = useGlobalFontColor ? fontColorAll : currentThemeSettings.fontColorLabels;

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

            if (ThemeManager.AlreadyProcessedElements.Contains(button))
            {
                return;
            }
            ThemeManager.AlreadyProcessedElements.Add(button);

            UIUtils.SetBackgroundStyleWithHover(button, backgroundButtonColor, backgroundButtonColorHover, fontColorButtons);

            VisualElement image = button.Q("image");
            if (image != null) image.style.unityBackgroundImageTintColor = fontColorButtons;
            VisualElement backImage = button.Q("backImage");
            if (backImage != null) backImage.style.unityBackgroundImageTintColor = fontColorButtons;
        });
        root.Query<VisualElement>(null, "unity-toggle__checkmark").ForEach(entry =>
        {
            if (ThemeManager.AlreadyProcessedElements.Contains(entry))
            {
                return;
            }
            ThemeManager.AlreadyProcessedElements.Add(entry);
            UIUtils.SetBackgroundStyleWithHover(entry, entry.parent, backgroundButtonColor, backgroundButtonColorHover, fontColorButtons);
        });
        root.Query<VisualElement>("songEntryUiRoot").ForEach(entry =>
        {
            if (ThemeManager.AlreadyProcessedElements.Contains(entry))
            {
                return;
            }
            ThemeManager.AlreadyProcessedElements.Add(entry);
            UIUtils.SetBackgroundStyleWithHover(entry, backgroundButtonColor, backgroundButtonColorHover, fontColorButtons);
        });

        UIUtils.ApplyFontColorForElements(root, new []{"Label", "titleImage", "sceneTitle", "sceneSubtitle"}, null, fontColorLabels);
        UIUtils.ApplyFontColorForElements(root, new []{"itemLabel"}, null, fontColorButtons);

        root.Query(null, "itemPickerItemLabel").ForEach(label => label.style.backgroundColor = itemPickerBackgroundColor);
        root.Query("titleImage").ForEach(image => image.style.unityBackgroundImageTintColor = fontColorLabels);
    }
}
