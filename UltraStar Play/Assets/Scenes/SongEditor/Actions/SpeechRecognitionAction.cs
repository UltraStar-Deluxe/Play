using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using Vosk;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SpeechRecognitionAction : INeedInjection
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

    private VoskRecognizer voskRecognizer;

    private string voskModelPath = @"F:\Dev\VoskModels\vosk-model-small-en-us-0.15";
    // private List<string> speechRecognitionPhrases = new() {"one", "two", "test"};
    private List<string> speechRecognitionPhrases = new();
    private int maxSpeechRecognitionAlternatives = 3;

    public void SetTextToAnalyzedSpeechAndNotify(IEnumerable<Note> selectedNotes)
    {
        SetTextToAnalyzedSpeech(selectedNotes);
        songMetaChangeEventStream.OnNext(new LyricsChangedEvent());
    }

    public void SetTextToAnalyzedSpeech(IEnumerable<Note> selectedNotes)
    {
        // For reading the audio samples, the AudioClip must not be streamed. All data must have been fully loaded.
        AudioClip audioClip = audioManager.LoadAudioClipFromUri(SongMetaUtils.GetAudioUri(songMeta), false);

        selectedNotes.ForEach(note =>
        {
            List<string> analyzedSpeech = AnalyzeNote(note, audioClip);
            Debug.Log($"Analyzed text of note '{note.Text}' from beat {note.StartBeat} to beat {note.EndBeat}: {analyzedSpeech.ToCsv()}");
            if (!analyzedSpeech.IsNullOrEmpty())
            {
                // Assume whole words. Thus, take first word and separate notes by space.
                string newText = analyzedSpeech.FirstOrDefault();
                string[] newWords = newText.Split(" ");
                if (!newWords.IsNullOrEmpty())
                {
                    string newWord = newWords[0];
                    note.SetText(newWord.Trim() + " ");
                }
            }
        });
    }

    private List<string> AnalyzeNote(Note note, AudioClip audioClip)
    {
        List<string> result = new();
        if (note == null
            || note.Length <= 0)
        {
            return result;
        }

        InitSpeechRecognizerIfNotDoneYet(audioClip);
        if (voskRecognizer == null)
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

        // WavFileWriter.WriteFile(Application.persistentDataPath + "/note-samples-stereo.wav", audioClip.frequency, audioClip.channels, noteSamplesStereo);
        // WavFileWriter.WriteFile(Application.persistentDataPath + "/note-samples-mono.wav", audioClip.frequency, 1, noteSamplesMono);

        short[] noteSamplesMonoShortArray = new short[noteSamplesMono.Length];
        for (int i = 0; i < noteSamplesMono.Length; i++)
        {
            noteSamplesMonoShortArray[i] = (short)Math.Floor(noteSamplesMono[i] * short.MaxValue);
        }

        voskRecognizer.AcceptWaveform(noteSamplesMonoShortArray, noteSamplesMonoShortArray.Length);
        string voskResultJsonString = voskRecognizer.FinalResult();
        Debug.Log("Vosk result: " + voskResultJsonString);
        if (voskResultJsonString.IsNullOrEmpty())
        {
            return result;
        }

        VoskResultJson voskResultJson = JsonConverter.FromJson<VoskResultJson>(voskResultJsonString);
        if (!voskResultJson.text.IsNullOrEmpty())
        {
            // There is one definitive result given by Vosk
            result.Add(voskResultJson.text);
            return result;
        }
        if (voskResultJson.alternatives.IsNullOrEmpty())
        {
            // Vosk did not find any plausible match
            return result;
        }

        // Take the alternative with highest confidence.
        VoskResultAlternativeJson bestVoskResultAlternativeJson;
        if (voskResultJson.alternatives.Count == 1)
        {
            bestVoskResultAlternativeJson = voskResultJson.alternatives.FirstOrDefault();
        }
        else
        {
            bestVoskResultAlternativeJson = voskResultJson.alternatives
                .Where(alternative => alternative != null && alternative.confidence > 0 && !alternative.text.IsNullOrEmpty())
                .ToList()
                .FindMaxElement(alternative => (float)alternative.confidence);
        }

        string bestResultText = bestVoskResultAlternativeJson.text.Trim();
        if (!bestResultText.IsNullOrEmpty())
        {
            result.Add(bestResultText);
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

    private void InitSpeechRecognizerIfNotDoneYet(AudioClip audioClip)
    {
        if (voskRecognizer != null
            || !songAudioPlayer.HasAudioClip)
        {
            return;
        }

        Model voskModel = new(voskModelPath);
        if (!speechRecognitionPhrases.IsNullOrEmpty())
        {
            string voskGrammar = JsonConverter.ToJson(speechRecognitionPhrases);
            voskRecognizer = new(voskModel, audioClip.frequency, voskGrammar);
        }
        else
        {
            voskRecognizer = new(voskModel, audioClip.frequency);
        }

        voskRecognizer.SetMaxAlternatives(maxSpeechRecognitionAlternatives);
        // voskRecognizer.SetWords();
    }

    private class VoskResultJson
    {
        public List<VoskResultAlternativeJson> alternatives;
        public string text;
    }

    private class VoskResultAlternativeJson
    {
        public double confidence;
        public string text;
    }
}
