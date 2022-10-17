using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class StyleSheetControl : MonoBehaviour, INeedInjection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        printScreenSize = true;
    }

    public static StyleSheetControl Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<StyleSheetControl>("StyleSheetControl");
        }
    }

    private static bool printScreenSize = true;

    [InjectedInInspector]
    public StyleSheet largeScreenStyleSheet;

    [Inject(Optional = true)]
    private UIDocument uiDocument;

    [Inject]
    private Settings settings;

    void Start()
    {
        if (printScreenSize)
        {
            printScreenSize = false;
            Debug.Log($"Screen size (inches): {GetPhysicalDiagonalScreenSizeInInches()}, DPI: {Screen.dpi}");
        }
        AddScreenSpecificStyleSheets();
        UpdateThemeSpecificStyleSheets();

        settings.GraphicSettings.ObserveEveryValueChanged(graphicSettings => graphicSettings.CurrentThemeName)
            .Subscribe(_ => UpdateThemeSpecificStyleSheets())
            .AddTo(gameObject);
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

        ThemeManager.ThemeSettings theme = ThemeManager.Instance.currentTheme;
        VisualElement root = uiDocument.rootVisualElement;

        Color backgroundButtonColor = theme.buttonMainColor;
        Color backgroundButtonColorHover = Color.Lerp(backgroundButtonColor, Color.white, 0.2f);
        Color itemPickerBackgroundColor = UIUtils.ColorHSVOffset(backgroundButtonColor, 0, -0.1f, 0.01f);

        Color fontColorAll = theme.fontColor;
        bool useGlobalFontColor = fontColorAll != Color.clear;

        Color fontColorButtons = useGlobalFontColor ? fontColorAll : theme.fontColorButtons;
        Color fontColorLabels = useGlobalFontColor ? fontColorAll : theme.fontColorLabels;

        // Change color of UXML elements:
        root.Query(null, "currentNoteLyrics", "previousNoteLyrics")
            .ForEach(el => el.style.color = backgroundButtonColor);

        root.Query<Button>().ForEach(button =>
        {
            if (button.ClassListContains("transparentBackgroundColor"))
                return;

            UIUtils.SetBackgroundStyleWithHover(button, backgroundButtonColor, backgroundButtonColorHover, fontColorButtons);

            VisualElement image = button.Q("image");
            if (image != null) image.style.unityBackgroundImageTintColor = fontColorButtons;
            VisualElement backImage = button.Q("backImage");
            if (backImage != null) backImage.style.unityBackgroundImageTintColor = fontColorButtons;
        });
        root.Query<VisualElement>(null, "unity-toggle__checkmark").ForEach(entry =>
        {
            UIUtils.SetBackgroundStyleWithHover(entry, entry.parent, backgroundButtonColor, backgroundButtonColorHover, fontColorButtons);
        });
        root.Query<VisualElement>("songEntryUiRoot").ForEach(entry =>
        {
            UIUtils.SetBackgroundStyleWithHover(entry, backgroundButtonColor, backgroundButtonColorHover, fontColorButtons);
        });

        UIUtils.ApplyFontColorForElements(root, new []{"Label", "titleImage", "sceneTitle", "sceneSubtitle"}, null, fontColorLabels);
        UIUtils.ApplyFontColorForElements(root, new []{"itemLabel"}, null, fontColorButtons);

        root.Query(null, "itemPickerItemLabel").ForEach(label => label.style.backgroundColor = itemPickerBackgroundColor);
        root.Query("titleImage").ForEach(image => image.style.unityBackgroundImageTintColor = fontColorLabels);
    }

    private void AddScreenSpecificStyleSheets()
    {
        if (uiDocument == null)
        {
            return;
        }

        if (Screen.dpi < 20 || Screen.dpi > 1000)
        {
            // Unlikely DPI value. Do nothing.
        }

        float physicalDiagonalScreenSizeInInches = GetPhysicalDiagonalScreenSizeInInches();
        if (physicalDiagonalScreenSizeInInches > 10)
        {
            uiDocument.rootVisualElement.styleSheets.Add(largeScreenStyleSheet);
        }
    }

    private float GetPhysicalDiagonalScreenSizeInInches()
    {
        // Get diagonal of right-angled triangle via Pythagoras theorem
        float widthInPixels = Screen.width * Screen.width;
        float heightInPixels = Screen.height * Screen.height;
        float diagonalInPixels = Mathf.Sqrt(widthInPixels + heightInPixels);
        float diagonalInInches = diagonalInPixels / Screen.dpi;
        return diagonalInInches;
    }
}
