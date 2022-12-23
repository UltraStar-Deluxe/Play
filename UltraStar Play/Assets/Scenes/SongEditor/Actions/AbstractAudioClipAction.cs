using System.IO;
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
    protected UiManager uiManager;

    [Inject]
    protected SongEditorSampleRecorderControl songEditorSampleRecorderControl;

    protected AudioClip GetAudioClip(ESongEditorSamplesSource samplesSource)
    {
        if (samplesSource == ESongEditorSamplesSource.Recording)
        {
            if (!songEditorSampleRecorderControl.HasRecordedAudio)
            {
                uiManager.CreateNotificationVisualElement("No recorded audio found. Use a microphone to record audio first.");
                return null;
            }
            return songEditorSampleRecorderControl.AudioClip;
        }
        else if (samplesSource == ESongEditorSamplesSource.Vocals)
        {
            if (!FileUtils.Exists(SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.VocalsAudio)))
            {
                uiManager.CreateNotificationVisualElement("No vocals audio found. Separate the audio first.");
                return null;
            }
            return audioManager.LoadAudioClipFromUri(SongMetaUtils.GetVocalsAudioUri(songMeta), false);
        }
        else if (samplesSource == ESongEditorSamplesSource.Instrumental)
        {
            if (!FileUtils.Exists(SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.InstrumentalAudio)))
            {
                uiManager.CreateNotificationVisualElement("No instrumental audio found. Separate the audio first.");
                return null;
            }
            return audioManager.LoadAudioClipFromUri(SongMetaUtils.GetInstrumentalAudioUri(songMeta), false);
        }

        // Use the song's audio.
        // For reading the audio samples, the AudioClip must not be streamed. All data must have been fully loaded.
        return audioManager.LoadAudioClipFromUri(SongMetaUtils.GetAudioUri(songMeta), false);
    }
}
