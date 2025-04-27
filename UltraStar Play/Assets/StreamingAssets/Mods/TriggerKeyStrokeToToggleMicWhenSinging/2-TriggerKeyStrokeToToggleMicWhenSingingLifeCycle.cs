using UnityEngine;
using UniInject;
using UniRx;
using System;
using System.Collections.Generic;
using WindowsInput;
using WindowsInput.Events;

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
            Simulate.Events().Click(WindowsInput.Events.KeyCode.LControl, modSettings.keyCode).Invoke();
        }
        else
        {
            Simulate.Events().Click(modSettings.keyCode).Invoke();
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
