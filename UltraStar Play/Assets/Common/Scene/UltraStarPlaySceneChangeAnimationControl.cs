using System;
using UniInject;
using UniRx;
using UnityEngine;

public class UltraStarPlaySceneChangeAnimationControl : AbstractSingletonBehaviour, INeedInjection
{
    public static UltraStarPlaySceneChangeAnimationControl Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<UltraStarPlaySceneChangeAnimationControl>();

    private RenderTexture uiCopyRenderTexture;
    public RenderTexture UiCopyRenderTexture
    {
        get
        {
            if (uiCopyRenderTexture == null)
            {
                uiCopyRenderTexture = new RenderTexture(Screen.width, Screen.height, 24);
            }

            return uiCopyRenderTexture;
        }
    }

    private Action animateAction;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private AudioSource audioSource;

    [Inject]
    private Settings settings;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private ThemeManager themeManager;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        sceneNavigator.SceneChangedEventStream
            .Subscribe(_ => UpdateSceneTexturesAndTransition())
            .AddTo(gameObject);
        UpdateSceneTexturesAndTransition();
    }

    private void UpdateSceneTexturesAndTransition()
    {
        themeManager.UpdateSceneTextures(UiCopyRenderTexture);

        if (settings.GraphicSettings.AnimateSceneChange)
        {
            animateAction?.Invoke();
        }
    }

    public void AnimateChangeToScene(Action doLoadSceneAction, Action doAnimateAction)
    {
        // Take "screenshot" of "old" scene.
        if (themeManager.UiRenderTexture == null)
        {
            Debug.LogWarning($"uiRenderTexture of ThemeManager is null. Not animating scene transition.");
        }
        else if (UiCopyRenderTexture == null)
        {
            Debug.LogWarning($"UiCopyRenderTexture is null. Not animating scene transition.");
        }
        else
        {
            Graphics.CopyTexture(themeManager.UiRenderTexture, UiCopyRenderTexture);
        }
        animateAction = doAnimateAction;
        doLoadSceneAction();
    }

    public void StartSceneChangeAnimation(EScene currentScene, EScene nextScene)
    {
        bool skipSceneChangeAnimationSound = nextScene == EScene.SingScene
                                             || currentScene == EScene.SingingResultsScene;
        if (!skipSceneChangeAnimationSound)
        {
            PlaySceneChangeAnimationSound();
        }

        float sceneChangeAnimationTimeInSeconds = themeManager.GetSceneChangeAnimationTimeInSeconds();
        if (sceneChangeAnimationTimeInSeconds <= 0)
        {
            return;
        }

        LeanTween.value(gameObject, 0, 1, sceneChangeAnimationTimeInSeconds)
            .setOnStart(() =>
            {
                themeManager.backgroundShaderControl.SetTransitionAnimationEnabled(true);
            })
            .setOnUpdate((float animTimePercent) =>
            {
                // Scale and fade out the snapshot of the old UIDocument.
                // Handled by the background shader to get correct premultiplied
                // blending and avoid the one-frame flicker issue.
                themeManager.backgroundShaderControl.SetTransitionAnimationTime(animTimePercent);
            })
            .setEaseInSine()
            .setOnComplete(() =>
            {
                themeManager.backgroundShaderControl.SetTransitionAnimationEnabled(false);
            });
    }

    private void PlaySceneChangeAnimationSound()
    {
        audioSource.volume = settings.AudioSettings.SceneChangeSoundVolumePercent / 100f;
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    private void OnDestroy()
    {
        Destroy(uiCopyRenderTexture);
    }
}
