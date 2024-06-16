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
    protected SongEditorMicSampleRecorder songEditorMicSampleRecorder;

    protected AudioClip GetAudioClip(ESongEditorSamplesSource samplesSource)
    {
        if (samplesSource == ESongEditorSamplesSource.Recording)
        {
            if (!songEditorMicSampleRecorder.HasRecordedAudio)
            {
                NotificationManager.CreateNotification(Translation.Get(R.Messages.songEditor_error_missingRecordedAudio));
                return null;
            }
            return songEditorMicSampleRecorder.AudioClip;
        }
        else if (samplesSource == ESongEditorSamplesSource.Vocals)
        {
            if (!FileUtils.Exists(SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.VocalsAudio)))
            {
                NotificationManager.CreateNotification(Translation.Get(R.Messages.songEditor_error_missingVocalsAudio));
                return null;
            }
            return AudioManager.LoadAudioClipFromUriImmediately(SongMetaUtils.GetVocalsAudioUri(songMeta), false);
        }
        else if (samplesSource == ESongEditorSamplesSource.Instrumental)
        {
            if (!FileUtils.Exists(SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.InstrumentalAudio)))
            {
                NotificationManager.CreateNotification(Translation.Get(R.Messages.songEditor_error_missingInstrumentalAudio));
                return null;
            }
            return AudioManager.LoadAudioClipFromUriImmediately(SongMetaUtils.GetInstrumentalAudioUri(songMeta), false);
        }

        // Use the song's audio.
        // For reading the audio samples, the AudioClip must not be streamed. All data must have been fully loaded.
        return AudioManager.LoadAudioClipFromUriImmediately(SongMetaUtils.GetAudioUri(songMeta), false);
    }
}
