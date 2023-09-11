using UnityEngine;
using UniInject;
using UnityEngine.UIElements;
using Groomgy.HelloWorld;

// Use Visual Studio Code with installed C# Dev Kit for IDE features such as
// code completion, error markers, go to definition, etc.

// A mod must implement subtypes of IRuntimeLoadedScript.
// A runnable is called on game start or when the mod is enabled.
// Other interface methods are called when needed, e.g., when the mod is disabled or a highscore of a song is requested.
public class MODNAMERunnable : IRuntimeLoadedRunnable, IDisableModHandler
{
    // Get common objects from the app environment via Inject attribute.
    [Inject]
    private AudioManager audioManager;

    [Inject]
    private UIDocument uiDocument;

    // The ModContext object is the same for every script in the mod folder.
    [Inject]
    private ModContext modContext;

    // IModSettings implement IAutoBoundRuntimeLoadedScript, which makes an instance available via Inject attribute
    [Inject]
    private MODNAMEModSettings demoModSettings;

    public void Run()
    {
        // You can do anything here, for example ...
        // ... change audio clips
        audioManager.defaultButtonSound = audioManager.LoadAudioClipFromUriImmediately($"{modContext.ModFolder}/sounds/cartoon-jump-6462.mp3", false);

        // ... change UI elements
        uiDocument.rootVisualElement.Query().ForEach(it => 
        {
            it.style.borderTopColor = new StyleColor(Color.red);
            it.style.borderTopWidth = 1;
        });

        // ... create new Unity GameObjects with custom behaviour
        // gameObject = new GameObject();
        // gameObject.AddComponent<MyMonoBehaviour>();
    }

    public void OnDisableMod()
    {
        // This method can be used to undo the changes of the mod, e.g. destroy added GameObjects.
        Debug.Log("Mod is disabled");
    }
}
