using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;
using Vosk;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SpeechRecognitionAction : AbstractAudioClipAction
{
    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private SpeechRecognitionManager speechRecognitionManager;

    [Inject(UxmlName = R.UxmlNames.speechRecognitionModelPathTextField)]
    private TextField speechRecognitionModelPathTextField;

    private readonly EnglishSyllableSplitter englishSyllableSplitter = new();

    public void SetTextToAnalyzedSpeechAndNotify(IEnumerable<Sentence> selectedSentences)
    {
        SetTextToAnalyzedSpeech(selectedSentences);
        songMetaChangeEventStream.OnNext(new LyricsChangedEvent());
    }

    public void SetTextToAnalyzedSpeech(IEnumerable<Sentence> selectedSentences)
    {
        if (GetSpeechRecognitionModelPath().IsNullOrEmpty()
            || !Directory.Exists(GetSpeechRecognitionModelPath()))
        {
            uiManager.CreateNotificationVisualElement("Invalid speech recognition model path. Check the settings.");
            return;
        }

        AudioClip audioClip = GetAudioClip();
        selectedSentences.ForEach(sentence =>
        {
            int sentenceLengthInBeats = sentence.ExtendedMaxBeat - sentence.MinBeat;
            List<string> analyzedSpeech = AnalyzeBeats(
                sentence.MinBeat,
                sentenceLengthInBeats,
                audioClip,
                speechRecognitionManager.GetSpeechRecognizer(CreateSpeechRecognizerParameters(audioClip)));
            Debug.Log($"Analyzed text from beat {sentence.MinBeat} to beat {sentence.ExtendedMaxBeat}: {analyzedSpeech.ToCsv()}");
            if (!analyzedSpeech.IsNullOrEmpty())
            {
                // Assume whole words. Thus, take first word and separate notes by space.
                EditorNoteLyricsInputControl.MapTextToNotes(analyzedSpeech.FirstOrDefault(), sentence.Notes.ToList(),
                    settings.SongEditorSettings.SplitSyllables
                        ? englishSyllableSplitter
                        : null);
            }
        });
    }

    private string GetSpeechRecognitionModelPath()
    {
        return speechRecognitionModelPathTextField.text;
    }

    public void SetTextToAnalyzedSpeechAndNotify(List<Note> selectedNotes)
    {
        SetTextToAnalyzedSpeech(selectedNotes);
        songMetaChangeEventStream.OnNext(new LyricsChangedEvent());
    }

    public void SetTextToAnalyzedSpeech(List<Note> selectedNotes)
    {
        AudioClip audioClip = GetAudioClip();

        int minBeat = selectedNotes.Select(note => note.StartBeat).Min();
        int maxBeat = selectedNotes.Select(note => note.EndBeat).Max();
        int lengthInBeats = maxBeat - minBeat;
        List<string> analyzedSpeech = AnalyzeBeats(
            minBeat,
            lengthInBeats,
            audioClip,
            speechRecognitionManager.GetSpeechRecognizer(CreateSpeechRecognizerParameters(audioClip)));
        Debug.Log($"Analyzed text from beat {minBeat} to beat {maxBeat}: {analyzedSpeech.ToCsv()}");
        if (!analyzedSpeech.IsNullOrEmpty())
        {
            EditorNoteLyricsInputControl.MapTextToNotes(analyzedSpeech.FirstOrDefault(), selectedNotes, englishSyllableSplitter);
        }
    }

    private VoskModelParameters CreateSpeechRecognizerParameters(AudioClip audioClip)
    {
        return new VoskModelParameters(audioClip.frequency, GetSpeechRecognitionModelPath(), GetSpeechRecognitionPhrases());
    }

    private List<string> GetSpeechRecognitionPhrases()
    {
        if (settings.SongEditorSettings.SpeechRecognitionPhrases.Trim().IsNullOrEmpty())
        {
            return new List<string>();
        }

        HashSet<string> wordsHashSet = new();
        string[] words = settings.SongEditorSettings.SpeechRecognitionPhrases.Split(new string[]{" ", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        words.ForEach(word =>
        {
            string normalizedWord = word.Replace("~", "")
                .Replace("?", "")
                .Replace("!", "")
                .Replace(".", "")
                .Replace("-", "")
                .Trim();
            wordsHashSet.Add(normalizedWord);
        });
        return wordsHashSet.ToList();
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
        float[] beatSamplesMono = AudioUtils.GetMonoAudioSamples(beatSamplesStereo, audioClip.channels);

        WavFileWriter.WriteFile(Application.persistentDataPath + "/speech-recognition-samples-stereo.wav", audioClip.frequency, audioClip.channels, beatSamplesStereo);
        WavFileWriter.WriteFile(Application.persistentDataPath + "/speech-recognition-samples-mono.wav", audioClip.frequency, 1, beatSamplesMono);

        short[] beatSamplesMonoShortArray = new short[beatSamplesMono.Length];
        for (int i = 0; i < beatSamplesMono.Length; i++)
        {
            beatSamplesMonoShortArray[i] = (short)Math.Floor(beatSamplesMono[i] * short.MaxValue);
        }

        DisposableStopwatch stopwatch = new("");
        voskRecognizer.AcceptWaveform(beatSamplesMonoShortArray, beatSamplesMonoShortArray.Length);
        string voskResultJsonString = voskRecognizer.FinalResult();
        Debug.Log($"Speech recognition took {stopwatch.ElapsedMilliseconds}");

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
}
