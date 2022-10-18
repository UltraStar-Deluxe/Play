using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

[ExecuteInEditMode]
public class BackgroundImageEffect : MonoBehaviour, INeedInjection
{
    [SerializeField] Material material;
    static readonly int _UiTex = Shader.PropertyToID("_UiTex");
    static readonly int _TransitionTex = Shader.PropertyToID("_TransitionTex");
    static readonly int _TransitionTime = Shader.PropertyToID("_TransitionTime");

    public static BackgroundImageEffect Instance;

    void Awake()
    {
        if (Instance != null)
        {
            // Can happen when the parent singleton is loaded with a new scene, this will be deleted with it
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        if (material == null)
        {
            this.enabled = false;
        }
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Graphics.Blit(src, dest, material);
    }

    public void SetUiRenderTextures(RenderTexture uiRenderTexture, RenderTexture transitionRenderTexture)
    {
        material.SetTexture(_UiTex, uiRenderTexture);
        material.SetTexture(_TransitionTex, transitionRenderTexture);
    }

    public void EnableTransition(bool enable)
    {
        if (enable) material.EnableKeyword("_UI_TRANSITION_ANIM");
        else material.DisableKeyword("_UI_TRANSITION_ANIM");
    }

    public void SetTransitionTime(float time)
    {
        material.SetFloat(_TransitionTime, time);
    }
}
