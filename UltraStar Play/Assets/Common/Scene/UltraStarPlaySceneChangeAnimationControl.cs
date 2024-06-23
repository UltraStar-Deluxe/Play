using System;
using System.Collections;
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
    
    [Inject]
    private ApplicationManager applicationManager;
    
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
            && settings.SceneChangeAnimation is ESceneChangeAnimation.Fade)
        {
            if (TryGetBackgroundVisualElementOrIsIrrelevant(out VisualElement background))
            {
                background.style.opacity = 0;
            }
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
        
        if (settings.SceneChangeAnimation is ESceneChangeAnimation.Zoom)
        {
            // Take "screenshot" of "old" scene.
            RenderTexture uiRenderTexture = renderTextureManager.GetExistingRenderTexture(ThemeManager.UiRenderTextureName);
            RenderTexture uiCopyRenderTexture = renderTextureManager.GetExistingRenderTexture(UiCopyRenderTextureName);
            
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

        if (settings.SceneChangeAnimation is ESceneChangeAnimation.Zoom)
        {
            doLoadSceneAction();
        }
        else if (settings.SceneChangeAnimation is ESceneChangeAnimation.Fade)
        {
            float animationTimeInSeconds = settings.SceneChangeDurationInSeconds;
            if (animationTimeInSeconds <= 0)
            {
                doLoadSceneAction();
                return;
            }

            // Fade-out is done here. It takes half of the total animation time.
            if (TryGetBackgroundVisualElementOrIsIrrelevant(out VisualElement background))
            {
                LeanTween.value(gameObject, 1, 0, animationTimeInSeconds / 2)
                    .setOnUpdate((float interpolatedValue) =>
                    {
                        background.style.opacity = interpolatedValue;
                    })
                    .setOnComplete(() => doLoadSceneAction());
            }
            else
            {
                doLoadSceneAction();
            }
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

        float animationTimeInSeconds = settings.SceneChangeDurationInSeconds;
        if (animationTimeInSeconds <= 0)
        {
            return;
        }

        if (settings.SceneChangeAnimation is ESceneChangeAnimation.Zoom)
        {
            // Only the fade-in is done here. Thus, it takes only half of the total animation time.
            animationTimeInSeconds /= 2;
        }

        if (TryGetBackgroundVisualElementOrIsIrrelevant(out VisualElement background))
        {
            StopAllCoroutines();
            StartCoroutine(SceneChangeAnimationCoroutine(animationTimeInSeconds, background));
        }
    }

    private IEnumerator SceneChangeAnimationCoroutine(
        float animationTimeInSeconds,
        VisualElement background,
        Action onComplete = null)
    {
        if (animationTimeInSeconds <= 0)
        {
            onComplete?.Invoke();
            yield break;
        }

        if (settings.SceneChangeAnimation is ESceneChangeAnimation.Zoom)
        {
            themeManager.backgroundShaderControl.SetTransitionAnimationEnabled(true);
        }   
        
        float timeInSeconds = 0;
        float timeInPercent = 0;
        while(timeInSeconds < 1
              && timeInPercent < 1)
        {
            float interpolatedValue = LeanTween.easeInSine(0, 1, timeInPercent);
            
            if (settings.SceneChangeAnimation is ESceneChangeAnimation.Zoom)
            {
                // Scale and fade out the snapshot of the old UIDocument.
                // Handled by the background shader to get correct premultiplied
                // blending and avoid the one-frame flicker issue.
                themeManager.backgroundShaderControl.SetTransitionAnimationTime(interpolatedValue);
            }
            else if (settings.SceneChangeAnimation is ESceneChangeAnimation.Fade
                     && background != null)
            {
                background.style.opacity = interpolatedValue;
            }

            // Force a slow animation, even if the FPS is low.
            float maxDeltaTimeInSeconds = 1f / ApplicationUtils.CurrentFrameRate;
            float deltaTimeInSeconds = Mathf.Min(Time.deltaTime, maxDeltaTimeInSeconds);
            timeInSeconds += deltaTimeInSeconds;
            timeInPercent = timeInSeconds / animationTimeInSeconds;
            yield return new WaitForEndOfFrame();
        }
        
        if (settings != null
            && themeManager != null
            && settings.SceneChangeAnimation is ESceneChangeAnimation.Zoom)
        {
            themeManager.backgroundShaderControl.SetTransitionAnimationEnabled(false);
        }
        
        onComplete?.Invoke();
    }

    private void PlaySceneChangeAnimationSound()
    {
        audioSource.volume = settings.SceneChangeSoundVolumePercent / 100f;
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    private bool TryGetBackgroundVisualElementOrIsIrrelevant(out VisualElement background)
    {
        background = uiDocument.rootVisualElement.Q(R.UxmlNames.background);
        return background != null
               || settings.SceneChangeAnimation is ESceneChangeAnimation.Zoom;
    }
}
