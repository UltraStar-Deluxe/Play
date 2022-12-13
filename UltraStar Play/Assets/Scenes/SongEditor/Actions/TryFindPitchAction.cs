using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class TryFindPitchAction : INeedInjection
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

    private IAudioSamplesAnalyzer audioSamplesAnalyzer;

    public void TryFindPitch(IEnumerable<Note> selectedNotes)
    {
        // For reading the audio samples, the AudioClip must not be streamed. All data must have been fully loaded.
        AudioClip audioClip = audioManager.LoadAudioClipFromUri(SongMetaUtils.GetAudioUri(songMeta), false);

        selectedNotes.ForEach(note =>
        {
            List<int> relativePitches = AnalyzeNote(note, audioClip);
            if (relativePitches.IsNullOrEmpty())
            {
                // The note may be too long. Try again by analyzing each beat individually.
                relativePitches = AnalyzeBeatsOfNote(note, audioClip);
            }

            Debug.Log($"Relative pitches of note '{note.Text}' from beat {note.StartBeat} to beat {note.EndBeat}: {relativePitches.ToCsv()}");
            if (!relativePitches.IsNullOrEmpty())
            {
                int relativeMidiNoteMedian = NumberUtils.Median(relativePitches);
                int newAbsoluteMidiNote = relativeMidiNoteMedian + (12 * (1 + MidiUtils.GetOctave(note.MidiNote)));
                note.SetMidiNote(newAbsoluteMidiNote);
            }
        });
    }

    private List<int> AnalyzeNote(Note note, AudioClip audioClip)
    {
        List<int> result = new();
        if (note == null
            || note.Length <= 0)
        {
            return result;
        }

        InitAudioSamplesAnalyzerIfNotDoneYet(audioClip);
        if (audioSamplesAnalyzer == null)
        {
            return result;
        }

        int samplesPerSecondMono = audioClip.frequency;
        int samplesPerSecond = samplesPerSecondMono * audioClip.channels;
        int maxSample = audioClip.samples * audioClip.channels;
        double beatLengthInMillis = BpmUtils.MillisecondsPerBeat(songMeta);
        double noteLengthInMillis = beatLengthInMillis * note.Length;
        double noteLengthInSamplesStereo = noteLengthInMillis / 1000.0 * samplesPerSecond;

        float[] noteSamplesStereo = new float[(int)noteLengthInSamplesStereo];

        double startBeatInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, note.StartBeat);
        int startBeatInSamplesMono = (int) (startBeatInMillis / 1000.0 * samplesPerSecondMono);
        startBeatInSamplesMono = NumberUtils.Limit(startBeatInSamplesMono, 0, maxSample);
        // Note that GetData always takes the offset in MONO samples, even if there are more channels.
        audioClip.GetData(noteSamplesStereo, startBeatInSamplesMono);
        float[] noteSamplesMono = GetMonoAudioSamples(noteSamplesStereo, audioClip.channels);

        // Debug.Log($"Start in ms: {startBeatInMillis}, length in ms: {noteLengthInMillis}, end in ms: {startBeatInMillis + noteLengthInMillis}, start in samples: {startBeatInSamplesMono}, length in samples: {noteLengthInSamplesStereo}, end in samples: {startBeatInSamplesMono + noteLengthInSamplesStereo}");

        WavFileWriter.WriteFile(Application.persistentDataPath + "/note-samples-stereo.wav", audioClip.frequency, audioClip.channels, noteSamplesStereo);
        WavFileWriter.WriteFile(Application.persistentDataPath + "/note-samples-mono.wav", audioClip.frequency, 1, noteSamplesMono);

        if (audioSamplesAnalyzer is DywaAudioSamplesAnalyzer dywaAudioSamplesAnalyzer)
        {
            // This is note the common use case of the Dynamic Wavelet algorithm. Here, each analysis is independent of the previous.
            dywaAudioSamplesAnalyzer.ClearPitchHistory();
        }

        PitchEvent pitchEvent = audioSamplesAnalyzer.ProcessAudioSamples(noteSamplesMono, 0, noteSamplesMono.Length, 1, 0);
        if (pitchEvent != null)
        {
            result.Add(MidiUtils.GetRelativePitch(pitchEvent.MidiNote));
        }
        return result;
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

        InitAudioSamplesAnalyzerIfNotDoneYet(audioClip);
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

    public void TryFindPitchAndNotify(IEnumerable<Note> selectedNotes)
    {
        TryFindPitch(selectedNotes);
        songMetaChangeEventStream.OnNext(new NotesChangedEvent());
    }

    private void InitAudioSamplesAnalyzerIfNotDoneYet(AudioClip audioClip)
    {
        if (audioSamplesAnalyzer != null
            || !songAudioPlayer.HasAudioClip)
        {
            return;
        }

        audioSamplesAnalyzer = AbstractMicPitchTracker.CreateAudioSamplesAnalyzer(
            settings.PitchDetectionAlgorithm,
            audioClip.frequency);
    }
}
