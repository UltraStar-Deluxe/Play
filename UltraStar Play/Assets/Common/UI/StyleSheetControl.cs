using UniInject;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class StyleSheetControl : AbstractSingletonBehaviour, INeedInjection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        printScreenSize = true;
    }

    public static StyleSheetControl Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<StyleSheetControl>();

    private static bool printScreenSize = true;

    [InjectedInInspector]
    public StyleSheet largeScreenStyleSheet;

    [InjectedInInspector]
    public StyleSheet smallScreenStyleSheet;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private Settings settings;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        if (printScreenSize)
        {
            printScreenSize = false;
            Debug.Log($"Screen size (inches): {ApplicationUtils.GetPhysicalDiagonalScreenSizeInInches()}, DPI: {Screen.dpi}");
        }
        AddScreenSpecificStyleSheets();
    }

    protected override void OnEnableSingleton()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    protected override void OnDisableSingleton()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        AddScreenSpecificStyleSheets();
    }

    private void AddScreenSpecificStyleSheets()
    {
        if (Screen.dpi < 20 || Screen.dpi > 1000)
        {
            // Unlikely DPI value. Do nothing.
        }

        if (ApplicationUtils.IsSmallScreen()
            && smallScreenStyleSheet != null
            && !PlatformUtils.IsStandalone)
        {
            uiDocument.rootVisualElement.styleSheets.Add(smallScreenStyleSheet);
        }
        else if (ApplicationUtils.IsLargeScreen()
            && largeScreenStyleSheet != null)
        {
            // Large screen styles are the default. Thus, do nothing.
        }
    }
}
