using UniInject;
using UnityEngine;

// This script must be placed next to a Camera component. Otherwise OnRenderImage is not called by Unity.
public class BackgroundShaderControl : AbstractSingletonBehaviour, INeedInjection
{
    public static BackgroundShaderControl Instance => DontDestroyOnLoadManager.FindComponentOrThrow<BackgroundShaderControl>();

    private static readonly int _ParticleTex = Shader.PropertyToID("_ParticleTex");
    private static readonly int _UiTex = Shader.PropertyToID("_UiTex");
    private static readonly int _BaseTex = Shader.PropertyToID("_BaseTex");
    private static readonly int _AdditiveLightTex = Shader.PropertyToID("_AdditiveLightTex");
    private static readonly int _TransitionTex = Shader.PropertyToID("_TransitionTex");
    private static readonly int _TransitionTime = Shader.PropertyToID("_TransitionTime");
    private static readonly int _TimeApplication = Shader.PropertyToID("_TimeApplication");

    [InjectedInInspector]
    public Material material;

    [InjectedInInspector]
    public RenderTexture baseTexture;
    
    [InjectedInInspector]
    public RenderTexture lightTexture;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        if (material == null)
        {
            Debug.LogError("Missing material on BackgroundShaderControl");
            gameObject.SetActive(false);
            return;
        }
        
        SetBaseTexture(baseTexture);
        SetLightTexture(lightTexture);
    }

    private void Update()
    {
        // Use that in shaders instead of _Time so that value doesn't reset on each scene change
        Shader.SetGlobalFloat(_TimeApplication, Time.time);
    }

    public void SetUiTextures(RenderTexture uiRenderTexture, RenderTexture particleRenderTexture, Texture transitionTexture)
    {
        material.SetTexture(_UiTex, uiRenderTexture);
        material.SetTexture(_ParticleTex, particleRenderTexture);
        material.SetTexture(_TransitionTex, transitionTexture);
    }

    public void SetLightTexture(Texture renderTexture)
    {
        material.SetTexture(_AdditiveLightTex, renderTexture);
    }

    public void SetBaseTexture(Texture renderTexture)
    {
        material.SetTexture(_BaseTex, renderTexture);
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
    
    public void SetSimpleBackgroundEnabled(bool enable)
    {
        if (enable)
        {
            material.EnableKeyword("_USE_SIMPLE_BACKGROUND");
        }
        else
        {
            material.DisableKeyword("_USE_SIMPLE_BACKGROUND");
        }
    }
    
    public void SetBaseTextureEnabled(bool enable)
    {
        if (enable)
        {
            material.EnableKeyword("_USE_BASE_TEXTURE");
        }
        else
        {
            material.DisableKeyword("_USE_BASE_TEXTURE");
        }
    }

    public void SetTransitionAnimationTime(float time)
    {
        material.SetFloat(_TransitionTime, time);
    }
}
