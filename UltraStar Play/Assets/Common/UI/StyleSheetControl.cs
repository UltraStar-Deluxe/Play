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
            Debug.Log($"Screen size (inches): {ApplicationUtils.GetPhysicalDiagonalScreenSizeInInches()}, DPI: {Screen.dpi}");
        }
        AddScreenSpecificStyleSheets();
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

        if (ApplicationUtils.IsLargeScreen())
        {
            uiDocument.rootVisualElement.styleSheets.Add(largeScreenStyleSheet);
        }
    }
}
