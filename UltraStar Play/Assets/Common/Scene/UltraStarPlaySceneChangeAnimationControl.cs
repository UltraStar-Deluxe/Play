using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SceneChangeAnimations;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;
using UnityEngine.SceneManagement;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class UltraStarPlaySceneChangeAnimationControl : SceneChangeAnimationControl, INeedInjection
{
    private static UltraStarPlaySceneChangeAnimationControl instance;
    public static UltraStarPlaySceneChangeAnimationControl Instance
    {
        get
        {
            if (instance == null)
            {
                UltraStarPlaySceneChangeAnimationControl instanceInScene = GameObjectUtils.FindComponentWithTag<UltraStarPlaySceneChangeAnimationControl>("SceneChangeAnimationControl");
                if (instanceInScene != null)
                {
                    GameObjectUtils.TryInitSingleInstanceWithDontDestroyOnLoad(ref instance, ref instanceInScene);
                }
            }
            return instance;
        }
    }

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private AudioSource audioSource;

    [Inject]
    private Settings settings;

    public override void Start()
    {
        UltraStarPlaySceneChangeAnimationControl self = this;
        GameObjectUtils.TryInitSingleInstanceWithDontDestroyOnLoad(ref instance, ref self);

        if (instance == this)
        {
            base.Start();
        }
    }

    public void StartSceneChangeAnimation(VisualElement visualElement, EScene currentScene, EScene nextScene)
    {
        bool skipSceneChangeAnimationSound = nextScene == EScene.SingScene
                                             || currentScene == EScene.SingingResultsScene;
        if (!skipSceneChangeAnimationSound)
        {
            PlaySceneChangeAnimationSound();
        }

        LeanTween.value(gameObject, 0, 1, 0.3f)
            .setOnUpdate((float animTimePercent) =>
            {
                visualElement.style.opacity = 1 - (animTimePercent * 2);

                float scale = 1 + animTimePercent * 1.5f;
                visualElement.style.scale = new StyleScale(new Scale(new Vector3(scale, scale, 1)));
            })
            .setOnComplete(() => visualElement.RemoveFromHierarchy())
            .setEaseInSine();
    }

    private void PlaySceneChangeAnimationSound()
    {
        audioSource.volume = settings.AudioSettings.SceneChangeSoundVolumePercent / 100f;
        audioSource.Play();
    }

    protected override void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (!settings.GraphicSettings.AnimateSceneChange)
        {
            return;
        }
        
        base.OnSceneLoaded(scene, loadSceneMode);
    }
}
