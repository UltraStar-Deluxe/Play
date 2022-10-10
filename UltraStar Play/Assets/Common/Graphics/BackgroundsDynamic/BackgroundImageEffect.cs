using UnityEngine;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

[ExecuteInEditMode]
public class BackgroundImageEffect : MonoBehaviour, INeedInjection
{
    public Material material;

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
}
