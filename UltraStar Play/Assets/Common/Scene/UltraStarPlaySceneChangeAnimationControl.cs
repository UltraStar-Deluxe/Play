using System;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UnityEngine.SceneManagement;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class UltraStarPlaySceneChangeAnimationControl : MonoBehaviour, INeedInjection
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

    [NonSerialized]
    public RenderTexture uiCopyRenderTexture;
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

        UltraStarPlaySceneChangeAnimationControl self = this;
        GameObjectUtils.TryInitSingleInstanceWithDontDestroyOnLoad(ref instance, ref self);

        // Start is not called again for this DontDestroyOnLoad-object
        DontDestroyOnLoad(gameObject);

        uiCopyRenderTexture = new RenderTexture(Screen.width, Screen.height, 24);
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
    }

    public void AnimateChangeToScene(Action doLoadSceneAction, Action doAnimateAction)
    {
        animateAction = doAnimateAction;

        // Take "screenshot" of "old" scene.
        UIDocument uiDocument = FindObjectOfType<UIDocument>();
        Graphics.CopyTexture(uiDocument.panelSettings.targetTexture, uiCopyRenderTexture);
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

        LeanTween.value(gameObject, 0, 1, 0.3f)
            .setOnStart(() =>
            {
                ThemeManager.Instance.BackgroundShaderControl.SetTransitionAnimationEnabled(true);
            })
            .setOnUpdate(animTimePercent =>
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
}
