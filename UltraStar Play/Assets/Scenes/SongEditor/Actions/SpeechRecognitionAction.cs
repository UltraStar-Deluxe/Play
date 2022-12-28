using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using Vosk;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SpeechRecognitionAction : AbstractAudioClipAction
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void StaticInit()
    {
        speechRecognitionProcessCount = 0;
    }
    private static object lockObject = new();
    private static int speechRecognitionProcessCount;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SpeechRecognitionManager speechRecognitionManager;

    [Inject]
    private SongEditorLayerManager songEditorLayerManager;

    [Inject]
    private EditorNoteDisplayer editorNoteDisplayer;

    [Inject(UxmlName = R.UxmlNames.speechRecognitionModelPathTextField)]
    private TextField speechRecognitionModelPathTextField;

    private readonly EnglishSyllableSplitter englishSyllableSplitter = new();

    public void SetTextToAnalyzedSpeech(List<Note> selectedNotes, bool notify)
    {
        DoSpeechRecognition(SongMetaUtils.MinBeat(selectedNotes), SongMetaUtils.LengthInBeats(selectedNotes))
            // Execute on Background thread
            .SubscribeOn(Scheduler.ThreadPool)
            // Notify on Main thread
            .ObserveOnMainThread()
            .CatchIgnore((Exception ex) => Debug.LogError(ex))
            .Subscribe(voskResultJson =>
            {
                EditorNoteLyricsInputControl.MapTextToNotes(voskResultJson?.text, selectedNotes, englishSyllableSplitter);
                if (notify)
                {
                    songMetaChangeEventStream.OnNext(new LyricsChangedEvent());
                }
            });
    }

    public void CreateNotes(int startBeat, int lengthInBeats, bool notify)
    {
        // Remove old notes
        songEditorLayerManager.GetEnumLayerNotes(ESongEditorLayer.SpeechRecognition)
            .Where(oldNote =>
                oldNote.StartBeat >= startBeat && oldNote.EndBeat <= startBeat + lengthInBeats)
            .ForEach(oldNote =>
            {
                editorNoteDisplayer.RemoveNoteControl(oldNote);
                songEditorLayerManager.RemoveNoteFromAllEnumLayers(oldNote);
            });

        DoSpeechRecognition(startBeat, lengthInBeats)
            // Execute on Background thread
            .SubscribeOn(Scheduler.ThreadPool)
            // Notify on Main thread
            .ObserveOnMainThread()
            .CatchIgnore((Exception ex) => Debug.LogError(ex))
            .Subscribe(voskResultJson =>
            {
                CreateNotesOfVoskResult(startBeat, voskResultJson.result);
                if (notify)
                {
                    songMetaChangeEventStream.OnNext(new NotesChangedEvent());
                }
            });
    }

    private void CreateNotesOfVoskResult(int offsetInBeats, List<VoskResultWordJson> resultList)
    {
        double beatsPerSeconds = BpmUtils.GetBeatsPerSecond(songMeta);
        resultList.ForEach(resultEntry =>
        {
            int noteStartInBeats = offsetInBeats + (int)(resultEntry.start * beatsPerSeconds);
            int noteEndInBeats = offsetInBeats + (int)(resultEntry.end * beatsPerSeconds);
            int noteLengthInBeats = noteEndInBeats - noteStartInBeats;
            string text = resultEntry.word + " ";
            int midiNote = settings.SongEditorSettings.MidiNoteForSpeechRecognition;
            Note newNote = new Note(ENoteType.Normal, noteStartInBeats, noteLengthInBeats, MidiUtils.GetUltraStarTxtPitch(midiNote), text);
            newNote.IsEditable = songEditorLayerManager.IsEnumLayerEditable(ESongEditorLayer.SpeechRecognition);
            songEditorLayerManager.AddNoteToEnumLayer(ESongEditorLayer.SpeechRecognition, newNote);
        });
    }

    private IObservable<VoskResultJson> DoSpeechRecognition(int startBeat, int lengthInBeats)
    {
        if (speechRecognitionProcessCount > 0)
        {
            uiManager.CreateNotificationVisualElement("Already performing speech recognition");
            return Observable.Throw<VoskResultJson>(new IllegalStateException("Already performing speech recognition"));
        }

        if (GetSpeechRecognitionModelPath().IsNullOrEmpty()
            || !Directory.Exists(GetSpeechRecognitionModelPath()))
        {
            uiManager.CreateNotificationVisualElement("Invalid speech recognition model path. Check the settings.");
            return Observable.Throw<VoskResultJson>(new IllegalStateException("Invalid speech recognition model path"));
        }

        // AudioClip can only be accessed on the main thread.
        // Thus, read all needed data before creating the observable that will be executed on a background thread.
        AudioClip audioClip = GetAudioClip(settings.SongEditorSettings.SpeechRecognitionSamplesSource);
        if (audioClip == null)
        {
            return Observable.Throw<VoskResultJson>(new IllegalStateException("No AudioClip"));
        }
        VoskModelParameters speechRecognizerParameters = CreateSpeechRecognizerParameters(audioClip.frequency);
        short[] audioSamplesForSpeechRecognition = GetAudioSamplesForSpeechRecognition(startBeat, lengthInBeats, audioClip);

        // Do speech recognition in an observable. The observable's code may be executed on a background thread.
        return Observable.Create<VoskResultJson>(o =>
        {
            lock (lockObject)
            {
                try
                {
                    speechRecognitionProcessCount++;
                    VoskResultJson voskResultJson = AnalyzeBeats(audioSamplesForSpeechRecognition,
                        speechRecognitionManager.GetSpeechRecognizer(speechRecognizerParameters));
                    Debug.Log($"Analyzed text from beat {startBeat} to beat {startBeat + lengthInBeats}. Result: {voskResultJson?.text}");
                    o.OnNext(voskResultJson);
                }
                catch (Exception ex)
                {
                    o.OnError(ex);
                    return Disposable.Empty;
                }
                finally
                {
                    speechRecognitionProcessCount--;
                }

                o.OnCompleted();
                return Disposable.Empty;
            }
        });
    }

    private VoskModelParameters CreateSpeechRecognizerParameters(int sampleRate)
    {
        return new VoskModelParameters(sampleRate, GetSpeechRecognitionModelPath(), GetSpeechRecognitionPhrases());
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

    private VoskResultJson AnalyzeBeats(short[] monoSamplesArray, VoskRecognizer voskRecognizer)
    {
        using (new DisposableStopwatch("Speech recognition took <ms>"))
        {
            voskRecognizer.AcceptWaveform(monoSamplesArray, monoSamplesArray.Length);
            string voskResultJsonString = voskRecognizer.FinalResult();
            Debug.Log($"Raw speech recognition result: {voskResultJsonString}");
            if (voskResultJsonString.IsNullOrEmpty())
            {
                return null;
            }
            VoskResultJson voskResultJson = JsonConverter.FromJson<VoskResultJson>(voskResultJsonString);
            return voskResultJson;
        }
    }

    private string GetSpeechRecognitionModelPath()
    {
        return speechRecognitionModelPathTextField.text;
    }

    private short[] GetAudioSamplesForSpeechRecognition(int startBeat, int lengthInBeats, AudioClip audioClip)
    {
        if (lengthInBeats <= 0)
        {
            return null;
        }

        double startBeatInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, startBeat);
        double singleBeatLengthInMillis = BpmUtils.MillisecondsPerBeat(songMeta);
        double lengthInMillis = singleBeatLengthInMillis * lengthInBeats;

        float[] monoAudioSamples = AudioUtils.GetAudioSamples(startBeatInMillis, lengthInMillis, audioClip, true);
        short[] monoAudioSamplesAsShorts = AudioUtils.ToShortSampleArray(monoAudioSamples);
        return monoAudioSamplesAsShorts;
    }
}
