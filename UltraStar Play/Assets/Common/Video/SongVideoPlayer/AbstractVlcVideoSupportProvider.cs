using System;
using LibVLCSharp;
using UniInject;
using UnityEngine;

public abstract class AbstractVlcVideoSupportProvider : AbstractVideoSupportProvider, INeedInjection
{
    public override EVideoSupportProvider VideoSupportProvider => EVideoSupportProvider.Vlc;

    protected MediaPlayer mediaPlayer;
    protected Texture2D vlcTexture;
    protected RenderTexture targetTexture;
    protected bool vlcFlipHorizontal = true;
    protected bool vlcFlipVertical = true;

    protected virtual void Update()
    {
        if (IsPlaying)
        {
            UpdateVlcTextures();
        }
    }

    protected virtual void OnDestroy()
    {
        Destroy(vlcTexture);
    }

    public override void SetTargetTexture(RenderTexture renderTexture)
    {
        targetTexture = renderTexture;
    }

    private void UpdateVlcTextures()
    {
        try
        {
            if (mediaPlayer == null
                || !mediaPlayer.IsPlaying
                || targetTexture == null)
            {
                return;
            }

            VlcManager.UpdateVlcTextures(mediaPlayer, ref vlcTexture);

            if (vlcTexture == null)
            {
                return;
            }

            IntPtr texPtr = mediaPlayer.GetTexture((uint)vlcTexture.width, (uint)vlcTexture.height, out bool updated);
            if (!updated)
            {
                return;
            }

            vlcTexture.UpdateExternalTexture(texPtr);

            // Copy the vlc texture into the target RenderTexture
            Vector2 scale = new Vector2(vlcFlipHorizontal ? -1 : 1, vlcFlipVertical ? -1 : 1);
            Graphics.Blit(vlcTexture, targetTexture, scale, Vector2.zero);
        }
        catch (VLCException ex)
        {
            Debug.LogWarning($"Failed to update VLC textures: {ex.Message}");
        }
    }
}
