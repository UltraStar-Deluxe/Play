using UnityEngine;
using UniInject;
using UniRx;
using System;
using System.Collections.Generic;
using WindowsInput;
using WindowsInput.Native;

public class TriggerKeyStrokeToToggleMicWhenSingingLifeCycle : IOnLoadMod, IOnDisableMod, IDisposable, IOnModInstanceBecomesObsolete
{
    private static readonly string shortcutName = "CTRL+F9";

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private TriggerKeyStrokeToToggleMicWhenSingingModSettings modSettings;

    private List<IDisposable> disposables = new List<IDisposable>();

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
    }

    private void OnBeforeSceneChanged(BeforeSceneChangeEvent evt)
    {
        if (evt.NextScene == EScene.SingScene
            || sceneNavigator.CurrentScene == EScene.SingScene)
        {
            TriggerKeyStroke();
        }
    }

    private void TriggerKeyStroke()
    {
        Debug.Log($"{nameof(TriggerKeyStrokeToToggleMicWhenSingingLifeCycle)}.TriggerKeyStroke");
        InputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.LCONTROL, VirtualKeyCode.F9);

        if (modSettings.showNotificationOnTriggerKeyStroke)
        {
            UiManager.CreateNotification($"Triggered shortcut {shortcutName}");
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
