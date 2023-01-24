using System;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UnityEngine.SceneManagement;

public class UltraStarPlaySceneChangeAnimationControl : MonoBehaviour
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
    private bool isInitialized;

    private void Init()
    {
        // TODO: Custom initialization because OnSceneLoaded works around the current SceneInjection flow. Do SceneInjection earlier?
        isInitialized = true;
        settings = FindObjectOfType<SettingsManager>().Settings;
        audioSource = GetComponentInChildren<AudioSource>();
    }

    void Start()
    {
        if (this != Instance)
        {
            Destroy(this);
            return;
        }

        if (!isInitialized)
        {
            Init();
        }
    }

    void OnEnable()
    {
        if (this != Instance)
        {
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        if (this != Instance)
        {
            return;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (this != Instance)
        {
            return;
        }

        if (!isInitialized)
        {
            Init();
        }

        if (!settings.GraphicSettings.AnimateSceneChange)
        {
            return;
        }

        animateAction?.Invoke();

        ThemeManager.Instance.UpdateSceneTextures(UiCopyRenderTexture);
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
