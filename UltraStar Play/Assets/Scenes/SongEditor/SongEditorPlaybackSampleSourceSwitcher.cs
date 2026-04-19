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
        // Switch to original audio
        if (settings.SongEditorSettings.PlaybackSamplesSource is ESongEditorSamplesSource.Vocals
            && !SongMetaUtils.VocalsAudioResourceExists(songMeta))
        {
            settings.SongEditorSettings.PlaybackSamplesSource = ESongEditorSamplesSource.OriginalMusic;
        }
        
        // Switch to vocals
        audioSeparationManager.AudioSeparationFinishedEventStream
            .Subscribe(evt =>
            {
                if (settings.SongEditorSettings.PlaybackSamplesSource is ESongEditorSamplesSource.OriginalMusic)
                {
                    settings.SongEditorSettings.PlaybackSamplesSource = ESongEditorSamplesSource.Vocals;
                }
            })
            .AddTo(gameObject);
    }
}
