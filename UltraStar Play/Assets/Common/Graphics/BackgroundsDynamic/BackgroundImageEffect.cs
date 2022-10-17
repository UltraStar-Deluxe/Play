using UnityEngine;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

[ExecuteInEditMode]
public class BackgroundImageEffect : MonoBehaviour, INeedInjection
{
    public Material material;
    static readonly int _UiTex = Shader.PropertyToID("_UiTex");

    void OnEnable()
    {
        if (material == null)
        {
            this.enabled = false;
        }
    }

    public void SetUiRenderTexture(RenderTexture uiRenderTexture)
    {
        material.SetTexture(_UiTex, uiRenderTexture);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Graphics.Blit(src, dest, material);
    }
}
