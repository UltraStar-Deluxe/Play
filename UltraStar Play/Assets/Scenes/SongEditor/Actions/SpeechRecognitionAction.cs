using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;
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

    [Inject(UxmlName = R.UxmlNames.speechRecognitionPhrasesTextField)]
    private TextField speechRecognitionPhrasesTextField;

    private EnglishSyllableSplitter englishSyllableSplitter = new();

    private string voskModelPath = @"F:\Dev\VoskModels\vosk-model-small-en-us-0.15";
    private Model voskModel;
    private int maxSpeechRecognitionAlternatives = 3;

    public void SetTextToAnalyzedSpeechAndNotify(IEnumerable<Sentence> selectedSentences)
    {
        SetTextToAnalyzedSpeech(selectedSentences);
        songMetaChangeEventStream.OnNext(new LyricsChangedEvent());
    }

    public void SetTextToAnalyzedSpeech(IEnumerable<Sentence> selectedSentences)
    {
        // For reading the audio samples, the AudioClip must not be streamed. All data must have been fully loaded.
        AudioClip audioClip = audioManager.LoadAudioClipFromUri(SongMetaUtils.GetAudioUri(songMeta), false);

        using VoskRecognizer voskRecognizer = CreateSpeechRecognizer(audioClip);
        if (voskRecognizer == null)
        {
            return;
        }

        selectedSentences.ForEach(sentence =>
        {
            int sentenceLengthInBeats = sentence.ExtendedMaxBeat - sentence.MinBeat;
            List<string> analyzedSpeech = AnalyzeBeats(sentence.MinBeat, sentenceLengthInBeats, audioClip, voskRecognizer);
            Debug.Log($"Analyzed text from beat {sentence.MinBeat} to beat {sentence.ExtendedMaxBeat}: {analyzedSpeech.ToCsv()}");
            if (!analyzedSpeech.IsNullOrEmpty())
            {
                // Assume whole words. Thus, take first word and separate notes by space.
                EditorNoteLyricsInputControl.MapTextToNotes(analyzedSpeech.FirstOrDefault(), sentence.Notes.ToList(), englishSyllableSplitter);
            }
        });
    }

    public void SetTextToAnalyzedSpeechAndNotify(List<Note> selectedNotes)
    {
        SetTextToAnalyzedSpeech(selectedNotes);
        songMetaChangeEventStream.OnNext(new LyricsChangedEvent());
    }

    public void SetTextToAnalyzedSpeech(List<Note> selectedNotes)
    {
        // For reading the audio samples, the AudioClip must not be streamed. All data must have been fully loaded.
        AudioClip audioClip = audioManager.LoadAudioClipFromUri(SongMetaUtils.GetAudioUri(songMeta), false);

        using VoskRecognizer voskRecognizer = CreateSpeechRecognizer(audioClip);
        if (voskRecognizer == null)
        {
            return;
        }

        int minBeat = selectedNotes.Select(note => note.StartBeat).Min();
        int maxBeat = selectedNotes.Select(note => note.EndBeat).Max();
        int lengthInBeats = maxBeat - minBeat;
        List<string> analyzedSpeech = AnalyzeBeats(minBeat, lengthInBeats, audioClip, voskRecognizer);
        Debug.Log($"Analyzed text from beat {minBeat} to beat {maxBeat}: {analyzedSpeech.ToCsv()}");
        if (!analyzedSpeech.IsNullOrEmpty())
        {
            EditorNoteLyricsInputControl.MapTextToNotes(analyzedSpeech.FirstOrDefault(), selectedNotes, englishSyllableSplitter);
        }
    }

    private List<string> GetSpeechRecognitionPhrases()
    {
        if (speechRecognitionPhrasesTextField.text.Trim().IsNullOrEmpty())
        {
            return new List<string>();
        }

        HashSet<string> wordsOfSong = new();
        string[] wordsOfVoice = speechRecognitionPhrasesTextField.text.Split(new string[]{" ", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        wordsOfVoice.ForEach(word =>
        {
            string normalizedWord = word.Replace("~", "")
                .Replace("?", "")
                .Replace("!", "")
                .Replace(".", "")
                .Replace("-", "")
                .Trim();
            wordsOfSong.Add(normalizedWord);
        });
        return wordsOfSong.ToList();
    }

    private List<string> AnalyzeBeats(int startBeat, int lengthInBeats, AudioClip audioClip, VoskRecognizer voskRecognizer)
    {
        List<string> result = new();
        if (lengthInBeats <= 0
            || voskRecognizer == null)
        {
            return result;
        }

        int samplesPerSecondMono = audioClip.frequency;
        int samplesPerSecond = samplesPerSecondMono * audioClip.channels;
        int maxSample = audioClip.samples * audioClip.channels;
        double singleBeatLengthInMillis = BpmUtils.MillisecondsPerBeat(songMeta);
        double lengthInMillis = singleBeatLengthInMillis * lengthInBeats;
        double lengthInSamplesStereo = lengthInMillis / 1000.0 * samplesPerSecond;

        float[] beatSamplesStereo = new float[(int)lengthInSamplesStereo];

        double startBeatInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, startBeat);
        int startBeatInSamplesMono = (int) (startBeatInMillis / 1000.0 * samplesPerSecondMono);
        startBeatInSamplesMono = NumberUtils.Limit(startBeatInSamplesMono, 0, maxSample);
        // Note that GetData always takes the offset in MONO samples, even if there are more channels.
        audioClip.GetData(beatSamplesStereo, startBeatInSamplesMono);
        float[] beatSamplesMono = GetMonoAudioSamples(beatSamplesStereo, audioClip.channels);

        WavFileWriter.WriteFile(Application.persistentDataPath + "/speech-recognition-samples-stereo.wav", audioClip.frequency, audioClip.channels, beatSamplesStereo);
        WavFileWriter.WriteFile(Application.persistentDataPath + "/speech-recognition-samples-mono.wav", audioClip.frequency, 1, beatSamplesMono);

        short[] beatSamplesMonoShortArray = new short[beatSamplesMono.Length];
        for (int i = 0; i < beatSamplesMono.Length; i++)
        {
            beatSamplesMonoShortArray[i] = (short)Math.Floor(beatSamplesMono[i] * short.MaxValue);
        }

        voskRecognizer.AcceptWaveform(beatSamplesMonoShortArray, beatSamplesMonoShortArray.Length);
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

    private VoskRecognizer CreateSpeechRecognizer(AudioClip audioClip)
    {
        if (audioClip == null
            || audioClip.samples <= 0
            || audioClip.frequency <= 0)
        {
            return null;
        }

        if (voskModel == null)
        {
            voskModel = new(voskModelPath);
        }

        VoskRecognizer voskRecognizer;
        List<string> speechRecognitionPhrases = GetSpeechRecognitionPhrases();
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

        return voskRecognizer;
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
