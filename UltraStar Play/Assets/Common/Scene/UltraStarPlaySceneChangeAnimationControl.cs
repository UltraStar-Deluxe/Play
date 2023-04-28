using System;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class UltraStarPlaySceneChangeAnimationControl : AbstractSingletonBehaviour, INeedInjection
{
    public static UltraStarPlaySceneChangeAnimationControl Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<UltraStarPlaySceneChangeAnimationControl>();

    private const string UiCopyRenderTextureName = "SceneChangeAnimationControl.UiCopyRenderTexture";

    private Action animateAction;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private AudioSource audioSource;

    [Inject]
    private Settings settings;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private ThemeManager themeManager;

    [Inject]
    private UIDocument uiDocument;
    
    [Inject]
    private RenderTextureManager renderTextureManager;
    
    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        sceneNavigator.SceneChangedEventStream
            .Subscribe(_ => OnSceneChanged())
            .AddTo(gameObject);
        UpdateSceneTexturesAndTransition();
    }

    private void OnSceneChanged()
    {
        UpdateSceneTexturesAndTransition();

        if (SettingsUtils.ShouldAnimateSceneChange(settings)
            && settings.GraphicSettings.sceneChangeAnimation is ESceneChangeAnimation.Fade)
        {
            GetBackgroundVisualElement().style.opacity = 0;
        }
    }
    
    private void UpdateSceneTexturesAndTransition()
    {
        renderTextureManager.GetOrCreateScreenSizedRenderTexture(UiCopyRenderTextureName,
            renderTexture => themeManager.UpdateSceneTextures(renderTexture));

        if (SettingsUtils.ShouldAnimateSceneChange(settings))
        {
            animateAction?.Invoke();
        }
    }

    public void AnimateChangeToScene(Action doLoadSceneAction, Action doAnimateAction)
    {
        animateAction = doAnimateAction;
        
        if (settings.GraphicSettings.sceneChangeAnimation is ESceneChangeAnimation.Zoom)
        {
            // Take "screenshot" of "old" scene.
            RenderTexture uiRenderTexture = renderTextureManager.GetExistingScreenSizedRenderTexture(ThemeManager.UiRenderTextureName);
            RenderTexture uiCopyRenderTexture = renderTextureManager.GetExistingScreenSizedRenderTexture(UiCopyRenderTextureName);
            
            if (uiRenderTexture == null)
            {
                Debug.LogWarning($"uiRenderTexture of ThemeManager is null. Not animating scene transition.");
            }
            else if (uiCopyRenderTexture == null)
            {
                Debug.LogWarning($"UiCopyRenderTexture is null. Not animating scene transition.");
            }
            else
            {
                Graphics.CopyTexture(uiRenderTexture, uiCopyRenderTexture);
            }
        }

        if (settings.GraphicSettings.sceneChangeAnimation is ESceneChangeAnimation.Zoom)
        {
            doLoadSceneAction();
        }
        else if (settings.GraphicSettings.sceneChangeAnimation is ESceneChangeAnimation.Fade)
        {
            float animationTimeInSeconds = settings.GraphicSettings.sceneChangeDurationInSeconds;
            if (animationTimeInSeconds <= 0)
            {
                doLoadSceneAction();
                return;
            }

            VisualElement background = GetBackgroundVisualElement();
            LeanTween.value(gameObject, 1, 0, animationTimeInSeconds)
                .setOnUpdate((float interpolatedValue) => background.style.opacity = interpolatedValue)
                .setOnComplete(() => doLoadSceneAction());
        }
    }

    public void StartSceneChangeAnimation(EScene currentScene, EScene nextScene)
    {
        bool skipSceneChangeAnimationSound = nextScene == EScene.SingScene
                                             || currentScene == EScene.SingingResultsScene;
        if (!skipSceneChangeAnimationSound)
        {
            PlaySceneChangeAnimationSound();
        }

        float animationTimeInSeconds = settings.GraphicSettings.sceneChangeDurationInSeconds;
        if (animationTimeInSeconds <= 0)
        {
            return;
        }

        VisualElement background = GetBackgroundVisualElement();
        LeanTween.value(gameObject, 0, 1, animationTimeInSeconds)
            .setOnStart(() =>
            {
                if (settings.GraphicSettings.sceneChangeAnimation is ESceneChangeAnimation.Zoom)
                {
                    themeManager.backgroundShaderControl.SetTransitionAnimationEnabled(true);
                }
            })
            .setOnUpdate((float interpolatedValue) =>
            {
                if (settings.GraphicSettings.sceneChangeAnimation is ESceneChangeAnimation.Zoom)
                {
                    // Scale and fade out the snapshot of the old UIDocument.
                    // Handled by the background shader to get correct premultiplied
                    // blending and avoid the one-frame flicker issue.
                    themeManager.backgroundShaderControl.SetTransitionAnimationTime(interpolatedValue);
                }
                else if (settings.GraphicSettings.sceneChangeAnimation is ESceneChangeAnimation.Fade)
                {
                    background.style.opacity = interpolatedValue;
                }
            })
            .setEaseInSine()
            .setOnComplete(() =>
            {
                if (settings.GraphicSettings.sceneChangeAnimation is ESceneChangeAnimation.Zoom)
                {
                    themeManager.backgroundShaderControl.SetTransitionAnimationEnabled(false);
                }
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

    private VisualElement GetBackgroundVisualElement()
    {
        return uiDocument.rootVisualElement.Q(R.UxmlNames.background);
    }
}
