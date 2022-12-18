using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class AbstractAudioClipAction : INeedInjection
{
    [Inject]
    protected SongMeta songMeta;

    [Inject]
    protected Settings settings;

    [Inject]
    protected AudioManager audioManager;

    [Inject]
    protected SongEditorSampleRecorderControl songEditorSampleRecorderControl;

    protected AudioClip GetAudioClip()
    {
        if (settings.SongEditorSettings.UseRecordedSamples)
        {
            return songEditorSampleRecorderControl.AudioClip;
        }
        else
        {
            // Use the song's audio.
            // For reading the audio samples, the AudioClip must not be streamed. All data must have been fully loaded.
            return audioManager.LoadAudioClipFromUri(SongMetaUtils.GetAudioUri(songMeta), false);
        }
    }
}
