using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

// Handles the loading, saving and application of themes for the app
// This includes the background material shader values, background particle effects, and UIToolkit colors/styles

public class ThemeManager : MonoBehaviour
{
    // the theme to load by default (filename without json extension)
    internal const string DEFAULT_THEME = "default_blue";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        Instance = null;
    }

    public static ThemeManager Instance { get; private set; }

    // ----------------------------------------------------------------

    [Serializable]
    internal class ThemeSettings
    {
        // These correspond to the possible JSON properties defined in a theme file.
        // Eventually a theme builder UI can be considered, but meanwhile this will
        // have to be written manually with a text editor.
        // See the files in the "themes" folder at the root of the project/build.

        public DynamicBackground dynamicBackground;
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
    internal class DynamicBackground
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

    public void LoadTheme(string filename)
    {
        if (!filename.EndsWith(".json")) filename += ".json";
        string fullPath = $"{Application.dataPath}/../themes/{filename}";
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"[THEME] Couldn't load theme at path: '{fullPath}'");
            return;
        }

        string jsonTheme = File.ReadAllText(fullPath);
        this.currentTheme = ThemeSettings.LoadFromJson(jsonTheme);

        this.StartCoroutine(Apply(this.currentTheme));
    }

    // ----------------------------------------------------------------

    public Material backgroundMaterial;
    public Material particleMaterial;
    public ParticleSystem backgroundParticleSystem;

    internal ThemeSettings currentTheme;

    Material originalBackgroundMaterial;
    Material originalParticleMaterial;
    readonly List<Texture2D> dynamicTextures = new();

    public PanelSettings panelSettings;
    RenderTexture renderTextureUserInterface;

    void Awake()
    {
        // UI is rendered into a RenderTexture, which is then blended into the screen using the background shader
        renderTextureUserInterface = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        panelSettings.targetTexture = renderTextureUserInterface;
        this.GetComponentInChildren<BackgroundImageEffect>().SetUiRenderTexture(renderTextureUserInterface);
    }

    void Start()
    {
        if (Instance != null)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        GameObjectUtils.SetTopLevelGameObjectAndDontDestroyOnLoad(this.gameObject);

        // Load default theme
        this.LoadTheme(DEFAULT_THEME);
    }

    void OnEnable()
    {
        // Create a copy of the original materials for restoration
        originalBackgroundMaterial = new Material(backgroundMaterial);
        originalParticleMaterial = new Material(particleMaterial);
    }

    void OnApplicationQuit()
    {
        // Reset original materials
        backgroundMaterial.CopyPropertiesFromMaterial(originalBackgroundMaterial);
        particleMaterial.CopyPropertiesFromMaterial(originalParticleMaterial);
        CleanupDynamicTextures();
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

    void CleanupDynamicTextures()
    {
        foreach (Texture2D texture2D in dynamicTextures) Destroy(texture2D);
        dynamicTextures.Clear();
    }

    IEnumerator Apply(ThemeSettings data)
    {
        // UIToolkit takes one frame to apply the style changes,
        // we wait so the background changes at the same frame
        yield return null;

        #region Dynamic background

        CleanupDynamicTextures();

        DynamicBackground background = data.dynamicBackground;

        // Material
        if (!string.IsNullOrEmpty(background.gradientRampFile))
        {
            string gradientPath = $"{Application.dataPath}/../themes/{background.gradientRampFile}";
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
            string patternPath = $"{Application.dataPath}/../themes/{background.patternFile}";
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
            string particlePath = $"{Application.dataPath}/../themes/{background.particleFile}";
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
}
