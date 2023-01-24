using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UltraStarPlaySceneChangeAnimationControl : AbstractSingletonBehaviour
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

    private AudioSource audioSource;
    private Settings settings;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void AwakeSingleton()
    {
        settings = FindObjectOfType<SettingsManager>().Settings;
        audioSource = GetComponentInChildren<AudioSource>();
    }

    protected override void OnEnableSingleton()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    protected override void OnDisableSingleton()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        ThemeManager.Instance.UpdateSceneTextures(UiCopyRenderTexture);

        if (!settings.GraphicSettings.AnimateSceneChange)
        {
            return;
        }

        animateAction?.Invoke();
    }

    public void AnimateChangeToScene(Action doLoadSceneAction, Action doAnimateAction)
    {
        // Take "screenshot" of "old" scene.
        RenderTexture uiRenderTexture = ThemeManager.Instance.UiRenderTexture;
        if (uiRenderTexture == null)
        {
            Debug.LogWarning($"uiRenderTexture of ThemeManager is null. Not animating scene transition.");
        }
        else if (UiCopyRenderTexture == null)
        {
            Debug.LogWarning($"UiCopyRenderTexture is null. Not animating scene transition.");
        }
        else
        {
            Graphics.CopyTexture(uiRenderTexture, UiCopyRenderTexture);
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

        float sceneChangeAnimationTimeInSeconds = ThemeManager.Instance.GetSceneChangeAnimationTimeInSeconds();
        if (sceneChangeAnimationTimeInSeconds <= 0)
        {
            return;
        }

        LeanTween.value(gameObject, 0, 1, sceneChangeAnimationTimeInSeconds)
            .setOnStart(() =>
            {
                ThemeManager.Instance.BackgroundShaderControl.SetTransitionAnimationEnabled(true);
            })
            .setOnUpdate((float animTimePercent) =>
            {
                // Scale and fade out the snapshot of the old UIDocument.
                // Handled by the background shader to get correct premultiplied
                // blending and avoid the one-frame flicker issue.
                ThemeManager.Instance.BackgroundShaderControl.SetTransitionAnimationTime(animTimePercent);
            })
            .setEaseInSine()
            .setOnComplete(() =>
            {
                ThemeManager.Instance.BackgroundShaderControl.SetTransitionAnimationEnabled(false);
            });
    }

    private void PlaySceneChangeAnimationSound()
    {
        audioSource.volume = settings.AudioSettings.SceneChangeSoundVolumePercent / 100f;
        audioSource.Play();
    }

    private void OnDestroy()
    {
        Destroy(uiCopyRenderTexture);
    }
}
