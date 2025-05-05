using System;
using System.IO;
using LibVLCSharp;
using UniInject;
using UnityEngine;

public abstract class AbstractVlcVideoSupportProvider : AbstractVideoSupportProvider, INeedInjection
{
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

    public override void Unload()
    {
        Destroy(vlcTexture);
    }

    public override bool IsSupported(string videoUri, bool videoEqualsAudio)
    {
        return !WebRequestUtils.IsHttpOrHttpsUri(videoUri)
               && settings.VlcToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Never
               && ApplicationUtils.IsVlcSupportedVideoFormat(Path.GetExtension(videoUri));
    }

    public override void SetTargetTexture(RenderTexture renderTexture)
    {
        targetTexture = renderTexture;
    }

    private void UpdateVlcTextures()
    {
        try
        {
            if (this == null
                || targetTexture == null
                || mediaPlayer == null
                || mediaPlayer.NativeReference == IntPtr.Zero
                || !mediaPlayer.IsPlaying)
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
