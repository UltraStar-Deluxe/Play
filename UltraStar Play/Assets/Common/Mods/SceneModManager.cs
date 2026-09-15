using System;
using System.Collections.Generic;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SceneModManager : AbstractSingletonBehaviour, INeedInjection
{
    public static SceneModManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<SceneModManager>();

    [Inject]
    private SceneNavigator sceneNavigator;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        UltraStarPlaySceneInjectionManager.SceneInjectionFinishedEventStream
            .Subscribe(evt => OnSceneInjectionFinished(evt));
    }

    private void OnSceneInjectionFinished(SceneInjectionFinishedEvent evt)
    {
        List<ISceneMod> sceneMods = ModManager.GetModObjects<ISceneMod>();
        foreach (ISceneMod sceneMod in sceneMods)
        {
            try
            {
                EScene currentScene = sceneNavigator.CurrentScene;
                SceneData sceneData = SceneNavigator.GetSceneData(currentScene);
                Injector sceneInjector = evt.SceneInjector;
                sceneMod.OnSceneEntered(new SceneEnteredContext(
                    currentScene,
                    sceneData,
                    sceneInjector));
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to run SceneMod: type '{sceneMod.GetType()}', error message: '{e.Message}'");
                Debug.LogException(e);
            }
        }
    }
}
