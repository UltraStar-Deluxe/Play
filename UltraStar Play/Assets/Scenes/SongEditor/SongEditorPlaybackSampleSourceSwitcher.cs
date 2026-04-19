using UniInject;
using UniRx;
using UnityEngine;

/**
 * Automatically changes the playback source.
 * - Switch to 'vocals' audio when vocals isolation has finished.
 * - Switch to 'original' audio when no vocals isolation is present yet.
 */
public class SongEditorPlaybackSampleSourceSwitcher : INeedInjection, IInjectionFinishedListener
{

    [Inject]
    private GameObject gameObject;
    
    [Inject]
    private Settings settings;
    
    [Inject]
    private SongMeta songMeta;
    
    [Inject]
    private AudioSeparationManager audioSeparationManager;
    
    public void OnInjectionFinished()
    {
        // Delay by 1 frame such that the song editor can initialize first
        _ = AwaitableUtils.ExecuteAfterDelayInFramesAsync(1, SwitchPlaybackSourceIfNeeded);
    }

    private void SwitchPlaybackSourceIfNeeded()
    {
        // Switch to original audio
        if (settings.SongEditorSettings.PlaybackSamplesSource is ESongEditorSamplesSource.Vocals
            && !SongMetaUtils.VocalsAudioResourceExists(songMeta))
        {
            settings.SongEditorSettings.PlaybackSamplesSource = ESongEditorSamplesSource.OriginalMusic;
            Log.WithClassContext().Information(() => "Switched playback to original audio");
            NotificationManager.CreateNotification(Translation.Of("No vocals audio found.\nSwitched playback to original audio."));
        }

        // Switch to vocals
        audioSeparationManager.AudioSeparationFinishedEventStream
            .Subscribe(_ =>
            {
                if (settings.SongEditorSettings.PlaybackSamplesSource is ESongEditorSamplesSource.OriginalMusic)
                {
                    settings.SongEditorSettings.PlaybackSamplesSource = ESongEditorSamplesSource.Vocals;
                    Log.WithClassContext().Information(() => "Switched playback to vocals audio");
                    NotificationManager.CreateNotification(Translation.Of("Switched playback to vocals audio."));
                }
            })
            .AddTo(gameObject);
    }
}
