using System;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class UltraStarPlaySceneChangeAnimationControl : AbstractSingletonBehaviour, INeedInjection
{
    public static UltraStarPlaySceneChangeAnimationControl Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<UltraStarPlaySceneChangeAnimationControl>();

    private RenderTexture uiCopyRenderTexture;
    public RenderTexture UiCopyRenderTexture
    {
        get
        {
            if (uiCopyRenderTexture != null
                && (uiCopyRenderTexture.width != Screen.width
                    || uiCopyRenderTexture.height != Screen.height))
            {
                Debug.Log("Recreate uiCopyRenderTexture because of screen size change.");
                Destroy(uiCopyRenderTexture);
                uiCopyRenderTexture = null;
            }
            
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

    [Inject]
    private UIDocument uiDocument;
    
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
        themeManager.UpdateSceneTextures(UiCopyRenderTexture);

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

    private void OnDestroy()
    {
        Destroy(uiCopyRenderTexture);
    }

    private VisualElement GetBackgroundVisualElement()
    {
        return uiDocument.rootVisualElement.Q(R.UxmlNames.background);
    }
}
