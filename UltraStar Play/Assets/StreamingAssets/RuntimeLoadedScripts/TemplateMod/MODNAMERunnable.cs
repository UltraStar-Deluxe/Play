using UnityEngine;
using UniInject;
using UnityEngine.UIElements;
using UniRx;
using System;
using System.Collections.Generic;

// Open the mod folder with Visual Studio Code and installed C# Dev Kit for IDE features such as
// code completion, error markers, parameter hints, go to definition, etc.
// ---
// A mod must implement subtypes of IRuntimeLoadedScript.
// This example mod implements two subtypes in this file and mod settings in another file.
// Other available interfaces can be found by executing 'mod.interfaces' in the game's console.
// ---
// IRuntimeLoadedRunnable.Run is called on game start or when the mod is enabled.
// Methods of other IRuntimeLoadedScript subtypes are called when needed, e.g.,
// when the mod is disabled or a highscore is requested.
public class MODNAMERunnable : IRuntimeLoadedRunnable, IOnDisableMod
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

    // IModSettings implement IAutoBoundRuntimeLoadedScript, which makes an instance available via Inject attribute
    [Inject]
    private MODNAMEModSettings demoModSettings;

    private readonly List<IDisposable> disposables = new List<IDisposable>();

    public void Run()
    {
        Debug.Log("MODNAME - Run");

        // You can do anything here, for example ...
        // ... change audio clips
        // audioManager.defaultButtonSound = audioManager.LoadAudioClipFromUriImmediately($"{modContext.ModFolder}/sounds/cartoon-jump-6462.mp3", false);

        // ... change UI elements
        uiDocument.rootVisualElement.Query<VisualElement>().ForEach(element =>
        {
            element.style.borderTopColor = new StyleColor(Color.red);
            element.style.borderTopWidth = 1;
        });

        // ... react to scene changes
        disposables.Add(sceneNavigator.SceneChangedEventStream.Subscribe(sceneChangedEvent =>
        {
            UiManager.CreateNotification($"Welcome to {sceneChangedEvent.NewScene}!");
        }));

        // ... create new Unity GameObjects with custom behaviour
        // GameObject gameObject = new GameObject();
        // gameObject.AddComponent<MyMonoBehaviour>();
    }

    public void OnDisableMod()
    {
        // This method can be used to undo the changes of the mod when the mod is disabled.
        // For example, the method could destroy added GameObjects and dispose subscriptions.
        Debug.Log("MODNAME - OnDisableMod");
        disposables.ForEach(disposable => disposable.Dispose());
    }
}
