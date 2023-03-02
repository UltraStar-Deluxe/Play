using UnityEngine;

// This script must be placed next to a Camera component. Otherwise OnRenderImage is not called by Unity.
public class BackgroundShaderControl : MonoBehaviour
{
    private static readonly int _ParticleTex = Shader.PropertyToID("_ParticleTex");
    private static readonly int _UiTex = Shader.PropertyToID("_UiTex");
    private static readonly int _TransitionTex = Shader.PropertyToID("_TransitionTex");
    private static readonly int _TransitionTime = Shader.PropertyToID("_TransitionTime");
    private static readonly int _TimeApplication = Shader.PropertyToID("_TimeApplication");

    public Material material;
    public RenderTexture particleRenderTexture;

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

    public void SetUiRenderTextures(RenderTexture uiRenderTexture, Texture transitionTexture)
    {
        material.SetTexture(_UiTex, uiRenderTexture);
        material.SetTexture(_TransitionTex, transitionTexture);
        material.SetTexture(_ParticleTex, particleRenderTexture);
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

    public void DisableShader()
    {
        material.SetFloat("_EnableGradientAnimation", 0);
        material.SetFloat("_UI_SHADOW", 0);
    }
}
