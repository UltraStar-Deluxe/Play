using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;
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

    [InjectedInInspector]
    public StyleSheet redThemeStyleSheet;

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

        settings.GraphicSettings.ObserveEveryValueChanged(graphicSettings => graphicSettings.themeName)
            .Subscribe(_ => UpdateThemeSpecificStyleSheets())
            .AddTo(gameObject);
    }

    private void UpdateThemeSpecificStyleSheets()
    {
        if (uiDocument == null)
        {
            return;
        }

        Dictionary<string, StyleSheet> themeNameToStyleSheet = new Dictionary<string, StyleSheet>();
        themeNameToStyleSheet.Add("RedTheme", redThemeStyleSheet);

        themeNameToStyleSheet.ForEach(entry =>
        {
            string themeName = entry.Key;
            StyleSheet styleSheet = entry.Value;
            if (settings.GraphicSettings.themeName == themeName)
            {
                if (!uiDocument.rootVisualElement.styleSheets.Contains(styleSheet))
                {
                    uiDocument.rootVisualElement.styleSheets.Add(styleSheet);
                }
            }
            else
            {
                uiDocument.rootVisualElement.styleSheets.Remove(styleSheet);
            }
        });
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
