using UniInject;
using UnityEngine;

// Mod interface to do something when a scene is loaded.
// Available scenes are found in the EScene enum.
public class MODNAMESceneMod : ISceneMod
{
    // Get common objects from the app environment via Inject attribute.
    [Inject]
    private AudioManager audioManager;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private SceneNavigator sceneNavigator;

    // The ModContext object is the same for every script in the mod folder.
    [Inject]
    private ModContext modContext;

    // Mod settings implement IAutoBoundMod, which makes an instance available here via Inject attribute
    [Inject]
    private MODNAMEModSettings modSettings;

    private readonly List<IDisposable> disposables = new List<IDisposable>();

    public void OnSceneEntered(SceneEnteredContext sceneEnteredContext)
    {
        UiManager.CreateNotification($"Welcome to {sceneEnteredContext.Scene}!");

        // Add Unity GameObject with MonoBehaviour
        GameObject gameObject = new GameObject();
        gameObject.name = "MODNAMEMonoBehaviour";
        MODNAMEMonoBehaviour behaviour = gameObject.AddComponent<MODNAMEMonoBehaviour>();
        sceneEnteredContext.SceneInjector.Inject(behaviour);
    }
}

public class MODNAMEMonoBehaviour : MonoBehaviour, INeedInjection
{
    // Awake is called once after instantiation
    private void Awake()
    {
        Debug.Log($"{GetType().Name}.Awake");
    }

    // Start is called once before Update
    private void Start()
    {
        Debug.Log($"{GetType().Name}.Start");
    }

    // Update is called once per frame
    private void Update()
    {
    }
}