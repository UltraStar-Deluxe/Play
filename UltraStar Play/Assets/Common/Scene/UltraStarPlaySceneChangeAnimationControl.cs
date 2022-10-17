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

        VisualElement newUiDocument = FindObjectOfType<UIDocument>().rootVisualElement.Q<VisualElement>("background");
        LeanTween.value(gameObject, 0, 1, 0.3f)
            .setOnUpdate((float animTimePercent) =>
            {
                // Scale and fade in the new UIDocument
                float tEaseOutSine = Mathf.Sin(animTimePercent * Mathf.PI / 2f);
                newUiDocument.style.opacity = tEaseOutSine;
                float scaleIn = Mathf.Lerp(0.8f, 1.0f, tEaseOutSine);
                newUiDocument.style.scale = new StyleScale(new Scale(new Vector3(scaleIn, scaleIn, 1)));

                // Scale and fade out the snapshot of the old UIDocument
                float tEaseInSine = 1f - Mathf.Cos(animTimePercent * Mathf.PI / 2f);
                visualElement.style.opacity = 1 - (tEaseInSine * 2);
                float scaleOut = Mathf.Lerp(1.0f, 1.0f / 0.4f, tEaseInSine);
                visualElement.style.scale = new StyleScale(new Scale(new Vector3(scaleOut, scaleOut, 1)));
            })
            .setOnComplete(visualElement.RemoveFromHierarchy);
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
