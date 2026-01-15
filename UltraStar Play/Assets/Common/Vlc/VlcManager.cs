using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp;
using Serilog;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class VlcManager : AbstractSingletonBehaviour, INeedInjection
{
    public static VlcManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<VlcManager>();

    private static Serilog.Core.Logger logger;

    [Inject]
    private Settings settings;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        DisposeLibVlc();
    }

    /**
     * The LibVLC class is mainly used for making MediaPlayer and Media objects. You should only have one LibVLC instance.
     */
    private static LibVLC libVLC;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void OnDestroySingleton()
    {
        DisposeLibVlc();
        DisposeVlcLogger();
    }

    public MediaPlayer CreateMediaPlayer()
    {
        InitVlcIfNotDoneYet();
        return new MediaPlayer(libVLC);
    }

    public static void DestroyMediaPlayer(MediaPlayer mediaPlayer)
    {
        if (libVLC == null
            || mediaPlayer == null)
        {
            return;
        }

        IntPtr mediaPlayerNativeReference = mediaPlayer.NativeReference;
        string mediaUrl = mediaPlayer.Media?.Mrl;
        Log.Debug(() => $"Disposing Vlc MediaPlayer (Media: '{mediaUrl}', NativeReference: {mediaPlayerNativeReference})");

        // Dispose on a background thread to avoid blocking the main thread.
        Task.Run(() =>
        {
            if (libVLC == null)
            {
                // libVLC has already been disposed, probably by closing the app.
                return;
            }
            mediaPlayer.Dispose();
            Log.Debug(() => $"Successfully disposed Vlc MediaPlayer (Media: '{mediaUrl}', NativeReference: {mediaPlayerNativeReference})");
        });
    }

    private void InitVlcIfNotDoneYet()
    {
        if (this != Instance)
        {
            throw new SingletonNotFoundException("This is not the singleton instance.");
        }
        
        if (libVLC != null)
        {
            return;
        }

        if (settings.VlcApiUsage is EApiUsage.Disabled)
        {
            throw new Exception("Not creating libVLC instance because VLC is not selected for media file playback.");
        }

        Debug.Log($"Creating LibVLC instance. options: {JsonConverter.ToJson(settings.VlcOptions)}");
        DisposeLibVlc();

        Core.Initialize(Application.dataPath);
        libVLC = new LibVLC(enableDebugLogs: true, settings.VlcOptions.ToArray());

        Debug.Log($"Initialized libVLC, changeset: '{libVLC.Changeset}', libVLC version: '{libVLC.Version}', libVLC assembly version: '{typeof(LibVLC).Assembly.GetName().Version}'");
        
        // Initialize Serilog logger for VLC output
        InitVlcLogger();

        // Setup Error Logging
        libVLC.Log += (s, e) =>
        {
            // Always use try/catch in LibVLC events.
            // LibVLC can freeze Unity if an exception goes unhandled inside an event handler.
            try
            {
                if (settings.LogVlcOutput && e?.FormattedLog != null)
                {
                    logger?.Information(e.FormattedLog);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Exception caught in libVLC.Log: {ex.Message}");
            }
        };
    }

    private static void DisposeLibVlc()
    {
        if (libVLC == null)
        {
            return;
        }

        Debug.Log("Disposing libVLC instance");
        libVLC.Dispose();
        libVLC = null;
    }

    private static void InitVlcLogger()
    {
        if (logger != null)
        {
            return;
        }

        string logFilePath = $"{Log.logFileFolder}/vlc-{DateTime.Now.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture)}.log";
        logger = new LoggerConfiguration()
            .WriteTo.File(
                path: logFilePath,
                outputTemplate: Log.outputTemplate)
            .CreateLogger();

        Debug.Log($"Initialized Serilog logger for VLC output. Path: {logFilePath}");
    }

    private static void DisposeVlcLogger()
    {
        if (logger == null)
        {
            return;
        }

        Debug.Log("Disposing Serilog VLC logger");
        logger.Dispose();
        logger = null;
    }

    public static void UpdateVlcTextures(MediaPlayer mediaPlayer, ref Texture2D vlcTexture)
    {
        uint height = 0;
        uint width = 0;
        mediaPlayer.Size(0, ref width, ref height);

        // Update textures if size changes
        if (vlcTexture == null
            || vlcTexture.width != width
            || vlcTexture.height != height)
        {
            Debug.Log($"Creating new VLC textures. Width: {width}, Height: {height}");

            // Destroy old textures
            Destroy(vlcTexture);

            // Create new textures
            CreateVlcTextures(mediaPlayer, width, height, out vlcTexture);
        }
    }

    /**
	 * Resize the output textures to the size of the video
	 */
    private static void CreateVlcTextures(
        MediaPlayer mediaPlayer,
        uint px,
        uint py,
        out Texture2D vlcTexture)
    {
        IntPtr texPtr = mediaPlayer.GetTexture(px, py, out bool updated);
        if (px <= 0
            || py <= 0
            || !updated
            || texPtr == IntPtr.Zero)
        {
            throw new VLCException("Could not get texture pointer");
        }

        // If the currently playing video uses the Bottom Right orientation, we have to do this to avoid stretching it.
        if (GetVideoOrientation(mediaPlayer) == VideoOrientation.BottomRight)
        {
            uint swap = px;
            px = py;
            py = swap;
        }

        // Make a texture of the proper size for the video to output to
        vlcTexture = Texture2D.CreateExternalTexture((int)px, (int)py, TextureFormat.RGBA32, false, true, texPtr);
    }

    /**
     * This returns the video orientation for the currently playing video, if there is one
     */
    private static VideoOrientation? GetVideoOrientation(MediaPlayer mediaPlayer)
    {
        MediaTrackList tracks = mediaPlayer?.Tracks(TrackType.Video);
        if (tracks == null || tracks.Count == 0)
        {
            return null;
        }

        // At the moment we're assuming the track we're playing is the first track
        VideoOrientation? orientation = tracks[0]?.Data.Video.Orientation;
        return orientation;
    }

    public void DisableMediaPlayerAudioOutput(MediaPlayer mediaPlayer)
    {
        if (mediaPlayer == null)
        {
            return;
        }

        mediaPlayer.SetAudioCallbacks(OnVlcAudioPlayMuted, OnVlcAudioPauseMuted, OnVlcAudioResumeMuted, OnVlcAudioFlushMuted, OnVlcAudioDrainMuted);
    }

    private void OnVlcAudioDrainMuted(IntPtr data)
    {
        // Muted => do nothing
    }

    private void OnVlcAudioFlushMuted(IntPtr data, long pts)
    {
        // Muted => do nothing
    }

    private void OnVlcAudioResumeMuted(IntPtr data, long pts)
    {
        // Muted => do nothing
    }

    private void OnVlcAudioPauseMuted(IntPtr data, long pts)
    {
        // Muted => do nothing
    }

    private void OnVlcAudioPlayMuted(IntPtr data, IntPtr samples, uint count, long pts)
    {
        // Muted => do nothing
    }
}
