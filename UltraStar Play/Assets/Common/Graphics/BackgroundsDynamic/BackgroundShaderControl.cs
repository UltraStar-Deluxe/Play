using UnityEngine;

// This script must be placed next to a Camera component. Otherwise OnRenderImage is not called by Unity.
public class BackgroundShaderControl : MonoBehaviour
{
    private static readonly int _UiTex = Shader.PropertyToID("_UiTex");
    private static readonly int _TransitionTex = Shader.PropertyToID("_TransitionTex");
    private static readonly int _TransitionTime = Shader.PropertyToID("_TransitionTime");
    private static readonly int _TimeApplication = Shader.PropertyToID("_TimeApplication");

    public Material material;

    private void Awake()
    {
        if (material == null)
        {
            enabled = false;
        }
    }

    private void Update()
    {
        // Use that in shaders instead of _Time so that value doesn't reset on each scene change
        Shader.SetGlobalFloat(_TimeApplication, Time.time);
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

    public void SetTransitionAnimationEnabled(bool enable)
    {
        if (enable)
        {
            material.EnableKeyword("_UI_TRANSITION_ANIM");
        }
        else
        {
            material.DisableKeyword("_UI_TRANSITION_ANIM");
        }
    }

    public void SetTransitionAnimationTime(float time)
    {
        material.SetFloat(_TransitionTime, time);
    }
}
