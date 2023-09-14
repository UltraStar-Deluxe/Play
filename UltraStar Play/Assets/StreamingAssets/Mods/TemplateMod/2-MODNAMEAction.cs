using UnityEngine;
using UniInject;
using UnityEngine.UIElements;
using UniRx;
using System;
using System.Collections.Generic;

// Open the mod folder with Visual Studio Code and installed C# Dev Kit for IDE features such as
// code completion, error markers, parameter hints, go to definition, etc.
// ---
// Mods must implement subtypes of special mod interfaces.
// Available interfaces can be found by executing 'mod.interfaces' in the game's console.
// ---
// IModAction is invoked on game start and when the list of enabled mods changed.
// Methods of other mod interfaces are called when needed, e.g.,
// when the mod is disabled or a highscore is requested.
public class MODNAMEAction : IModAction, IOnDisableMod
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

    public void Invoke()
    {
        Debug.Log("MODNAME - Invoke");

        // You can do anything here, for example ...

        // ... change audio clips
        // audioManager.defaultButtonSound = AudioManager.LoadAudioClipFromUriImmediately($"{modContext.ModFolder}/sounds/cartoon-jump-6462.mp3");

        // ... change UI elements
        // uiDocument.rootVisualElement.Query<VisualElement>().ForEach(element =>
        // {
        //     element.style.borderTopColor = new StyleColor(Color.red);
        //     element.style.borderTopWidth = 1;
        // });

        // ... react to scene changes
        // disposables.Add(sceneNavigator.SceneChangedEventStream.Subscribe(sceneChangedEvent =>
        // {
        //     UiManager.CreateNotification($"Welcome to {sceneChangedEvent.NewScene}!");
        // }));

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
        disposables.Clear();
    }
}
