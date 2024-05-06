using UnityEngine;
using UniInject;
using UniRx;
using System;
using System.Collections.Generic;
using WindowsInput;
using WindowsInput.Native;

public class TriggerKeyStrokeToToggleMicWhenSingingLifeCycle : IOnLoadMod, IOnDisableMod, IDisposable, IOnModInstanceBecomesObsolete, IAutoBoundMod
{
    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private TriggerKeyStrokeToToggleMicWhenSingingModSettings modSettings;

    private List<IDisposable> disposables = new List<IDisposable>();

    private string ShortcutName
    {
        get
        {
            if (modSettings.requireControlModifier)
            {
                return $"CTRL+{modSettings.keyCode}";
            }
            return $"{modSettings.keyCode}";
        }
    }

    private InputSimulator inputSimulator;
    private InputSimulator InputSimulator {
        get
        {
            if (inputSimulator == null)
            {
                inputSimulator = new InputSimulator();
            }

            return inputSimulator;
        }
    }

    public void OnLoadMod()
    {
        Debug.Log($"{nameof(TriggerKeyStrokeToToggleMicWhenSingingLifeCycle)}.OnLoadMod");
        disposables.Add(sceneNavigator.BeforeSceneChangeEventStream
            .Subscribe(evt => OnBeforeSceneChanged(evt)));

        modSettings.OnTriggerShortcut = TriggerKeyStroke;
    }

    private void OnBeforeSceneChanged(BeforeSceneChangeEvent evt)
    {
        if (evt.NextScene == EScene.SingScene
            || sceneNavigator.CurrentScene == EScene.SingScene)
        {
            TriggerKeyStroke();
        }
    }

    public void TriggerKeyStroke()
    {
        Debug.Log($"{nameof(TriggerKeyStrokeToToggleMicWhenSingingLifeCycle)}.TriggerKeyStroke '{ShortcutName}'");
        if (modSettings.requireControlModifier)
        {
            InputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.LCONTROL, modSettings.keyCode);
        }
        else
        {
            InputSimulator.Keyboard.ModifiedKeyStroke(new List<VirtualKeyCode>(), modSettings.keyCode);
        }

        if (modSettings.showNotificationOnTriggerKeyStroke)
        {
            NotificationManager.CreateNotification(Translation.Of($"Triggered shortcut '{ShortcutName}'"));
        }
    }

    public void OnDisableMod()
    {
        Debug.Log($"{nameof(TriggerKeyStrokeToToggleMicWhenSingingLifeCycle)}.OnDisableMod");
        Dispose();
    }

    public void OnModInstanceBecomesObsolete()
    {
        Debug.Log($"{nameof(TriggerKeyStrokeToToggleMicWhenSingingLifeCycle)}.OnModInstanceBecomesObsolete");
        Dispose();
    }

    public void Dispose()
    {
        disposables.ForEach(it => it.Dispose());
    }
}
