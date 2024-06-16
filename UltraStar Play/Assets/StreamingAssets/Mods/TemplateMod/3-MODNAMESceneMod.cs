using UniInject;
using UnityEngine;

// Mod interface to do something when a scene is loaded.
// Available scenes are found in the EScene enum.
public class MODNAMESceneMod : ISceneMod
{
    // Get common objects from the app environment via Inject attribute.
    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private SceneNavigator sceneNavigator;

    // Mod settings implement IAutoBoundMod, which makes an instance available via Inject attribute
    [Inject]
    private MODNAMEModSettings modSettings;

    private readonly List<IDisposable> disposables = new List<IDisposable>();

    public void OnSceneEntered(SceneEnteredContext sceneEnteredContext)
    {
        // You can do anything here, for example ...

        // ... show a message
        NotificationManager.CreateNotification(Translation.Of($"Welcome to {sceneEnteredContext.Scene}!"));

        // ... change UI elements
        // uiDocument.rootVisualElement.Query<VisualElement>().ForEach(element =>
        // {
        //     element.style.borderTopColor = new StyleColor(Color.red);
        //     element.style.borderTopWidth = 1;
        // });

        // ... create new Unity GameObjects with custom behaviour.
        GameObject gameObject = new GameObject();
        gameObject.name = nameof(MODNAMEMonoBehaviour);
        MODNAMEMonoBehaviour behaviour = gameObject.AddComponent<MODNAMEMonoBehaviour>();
        sceneEnteredContext.SceneInjector.Inject(behaviour);
    }
}

public class MODNAMEMonoBehaviour : MonoBehaviour, INeedInjection
{
    // Awake is called once after instantiation
    private void Awake()
    {
        Debug.Log($"{nameof(MODNAMEMonoBehaviour)}.Awake");
    }

    // Start is called once before Update
    private void Start()
    {
        Debug.Log($"{nameof(MODNAMEMonoBehaviour)}.Start");
    }

    // Update is called once per frame
    private void Update()
    {
    }

    private void OnDestroy()
    {
        Debug.Log($"{nameof(MODNAMEMonoBehaviour)}.OnDestroy");
        // GameObjects are destroyed before the next scene is loaded.
        // To persist a GameObject across scene changes, make it a child of DontDestroyOnLoadManager.
    }
}