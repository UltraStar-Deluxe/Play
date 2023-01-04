using UnityEngine;

// [ExecuteInEditMode]
// This script must be placed next to a Camera component. Otherwise OnRenderImage is not called by Unity.
public class BackgroundImageControl : MonoBehaviour
{
    [SerializeField]
    private Material material;
    private static readonly int _UiTex = Shader.PropertyToID("_UiTex");
    private static readonly int _TransitionTex = Shader.PropertyToID("_TransitionTex");
    private static readonly int _TransitionTime = Shader.PropertyToID("_TransitionTime");

    private void OnEnable()
    {
        if (material == null)
        {
            this.enabled = false;
        }
    }

    /**
     * Event function that Unity calls after a Camera has finished rendering, that allows you to modify the Camera's final image.
     * Therefor, this script must be placed next to a Camera component.
     */
    private void OnRenderImage(RenderTexture src, RenderTexture dest)
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
