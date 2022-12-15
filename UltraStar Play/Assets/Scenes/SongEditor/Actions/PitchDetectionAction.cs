using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PitchDetectionAction : INeedInjection
{
    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private Settings settings;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private AudioManager audioManager;

    [Inject]
    private SongEditorLayerManager songEditorLayerManager;

    [Inject]
    private EditorNoteDisplayer editorNoteDisplayer;

    private IAudioSamplesAnalyzer audioSamplesAnalyzer;
    private EPitchDetectionAlgorithm audioSamplesAnalyzerPitchDetectionAlgorithm;

    public void DetectPitchAndNotify(int startBeatInclusive, int lengthInBeats)
    {
        DetectPitch(startBeatInclusive, lengthInBeats);
        editorNoteDisplayer.UpdateNotes();
    }

    public void DetectPitch(int startBeatInclusive, int lengthInBeats)
    {
        // For reading the audio samples, the AudioClip must not be streamed. All data must have been fully loaded.
        AudioClip audioClip = audioManager.LoadAudioClipFromUri(SongMetaUtils.GetAudioUri(songMeta), false);

        // Remove old analyzed notes
        songEditorLayerManager.GetNotes(ESongEditorLayer.PitchDetection)
            .Where(oldNote =>
                oldNote.StartBeat >= startBeatInclusive && oldNote.EndBeat <= oldNote.StartBeat + lengthInBeats)
            .ForEach(oldNote =>
            {
                editorNoteDisplayer.RemoveNoteControl(oldNote);
                songEditorLayerManager.RemoveNoteFromAllLayers(oldNote);
            });

        Note lastAnalyzedNote = null;
        int endBeatExclusive = startBeatInclusive + lengthInBeats;
        for (int beat = startBeatInclusive; beat < endBeatExclusive; beat++)
        {
            PitchEvent pitchEvent = AnalyzeBeat(beat, audioClip);
            if (pitchEvent == null)
            {
                continue;
            }

            if (lastAnalyzedNote != null
                && lastAnalyzedNote.MidiNote == pitchEvent.MidiNote)
            {
                // Extend previously generated note to this beat
                lastAnalyzedNote.SetLength(lastAnalyzedNote.Length + 1);
            }
            else
            {
                Note analyzedNote = new Note(ENoteType.Normal, beat, 1, MidiUtils.GetUltraStarTxtPitch(pitchEvent.MidiNote), "");
                analyzedNote.IsEditable = false;
                songEditorLayerManager.AddNoteToLayer(ESongEditorLayer.PitchDetection, analyzedNote);
                lastAnalyzedNote = analyzedNote;
            }
        }

        if (audioSamplesAnalyzer is DywaAudioSamplesAnalyzer dywaAudioSamplesAnalyzer)
        {
            // This is not the common use case of the Dynamic Wavelet algorithm. The next analysis will be independent of the previous one.
            dywaAudioSamplesAnalyzer.ClearPitchHistory();
        }
    }

    private PitchEvent AnalyzeBeat(int beat, AudioClip audioClip)
    {
        CreateOrUpdateAudioSamplesAnalyzer(audioClip);
        if (audioSamplesAnalyzer == null)
        {
            return null;
        }

        int samplesPerSecondMono = audioClip.frequency;
        int samplesPerSecond = samplesPerSecondMono * audioClip.channels;
        int maxSample = audioClip.samples * audioClip.channels;
        double beatLengthInMillis = BpmUtils.MillisecondsPerBeat(songMeta);
        double beatLengthInSamplesStereo = beatLengthInMillis / 1000.0 * samplesPerSecond;

        float[] beatSamplesStereo = new float[(int)beatLengthInSamplesStereo];

        double startBeatInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, beat);
        int startBeatInSamplesMono = (int) (startBeatInMillis / 1000.0 * samplesPerSecondMono);
        startBeatInSamplesMono = NumberUtils.Limit(startBeatInSamplesMono, 0, maxSample);
        // Note that GetData always takes the offset in MONO samples, even if there are more channels.
        audioClip.GetData(beatSamplesStereo, startBeatInSamplesMono);
        float[] beatSamplesMono = GetMonoAudioSamples(beatSamplesStereo, audioClip.channels);

        // Debug.Log($"Start in ms: {startBeatInMillis}, length in ms: {noteLengthInMillis}, end in ms: {startBeatInMillis + noteLengthInMillis}, start in samples: {startBeatInSamplesMono}, length in samples: {noteLengthInSamplesStereo}, end in samples: {startBeatInSamplesMono + noteLengthInSamplesStereo}");
        // WavFileWriter.WriteFile(Application.persistentDataPath + "/note-samples-stereo.wav", audioClip.frequency, audioClip.channels, beatSamplesStereo);
        // WavFileWriter.WriteFile(Application.persistentDataPath + "/note-samples-mono.wav", audioClip.frequency, 1, beatSamplesMono);

        PitchEvent pitchEvent = audioSamplesAnalyzer.ProcessAudioSamples(beatSamplesMono, 0, beatSamplesMono.Length, 1, 0);
        return pitchEvent;
    }

    private float[] GetMonoAudioSamples(float[] originalSamples, int channelCount)
    {
        if (channelCount <= 1)
        {
            return originalSamples;
        }

        // Stereo to mono => take the average of the channels
        float[] monoSamples = new float[originalSamples.Length / channelCount];
        int monoSampleIndex = 0;
        for (int stereoSampleIndex = 0; stereoSampleIndex < originalSamples.Length && monoSampleIndex < monoSamples.Length; stereoSampleIndex += channelCount)
        {
            float sampleSum = 0;
            for (int channelIndex = 0; channelIndex < channelCount && (stereoSampleIndex + channelIndex) < originalSamples.Length; channelIndex++)
            {
                sampleSum += originalSamples[stereoSampleIndex + channelIndex];
            }

            float sampleAverage = sampleSum / channelCount;
            monoSamples[monoSampleIndex] = sampleAverage;
            monoSampleIndex++;
        }

        return monoSamples;
    }

    private List<int> AnalyzeBeatsOfNote(Note note, AudioClip audioClip)
    {
        List<int> result = new();
        if (note == null
            || note.Length <= 0)
        {
            return result;
        }

        CreateOrUpdateAudioSamplesAnalyzer(audioClip);
        if (audioSamplesAnalyzer == null)
        {
            return result;
        }

        double beatLengthInMillis = BpmUtils.MillisecondsPerBeat(songMeta);
        double beatLengthInSamples = beatLengthInMillis * audioClip.frequency / 1000;

        float[] audioSamplesOfBeat = new float[(int)beatLengthInSamples];
        for (int beat = note.StartBeat; beat < note.StartBeat + note.Length; beat++)
        {
            double beatStartInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, beat);
            int beatStartInSamples = (int) beatStartInMillis * audioClip.frequency / 1000;
            beatStartInSamples = NumberUtils.Limit(beatStartInSamples, 0, audioClip.samples);
            audioClip.GetData(audioSamplesOfBeat, beatStartInSamples);

            PitchEvent pitchEvent = audioSamplesAnalyzer.ProcessAudioSamples(audioSamplesOfBeat, 0, audioSamplesOfBeat.Length, 1, 0);
            if (pitchEvent != null)
            {
                result.Add(MidiUtils.GetRelativePitch(pitchEvent.MidiNote));
            }
        }
        return result;
    }

    private void CreateOrUpdateAudioSamplesAnalyzer(AudioClip audioClip)
    {
        if ((audioSamplesAnalyzer != null
                && audioSamplesAnalyzerPitchDetectionAlgorithm == settings.SongEditorSettings.PitchDetectionAlgorithm)
            || !songAudioPlayer.HasAudioClip)
        {
            return;
        }

        audioSamplesAnalyzer = AbstractMicPitchTracker.CreateAudioSamplesAnalyzer(
            settings.SongEditorSettings.PitchDetectionAlgorithm,
            audioClip.frequency);
        audioSamplesAnalyzerPitchDetectionAlgorithm = settings.SongEditorSettings.PitchDetectionAlgorithm;
    }
}
