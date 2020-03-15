using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlayerController : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public PlayerUiController playerUiControllerPrefab;

    public PlayerProfile PlayerProfile { get; private set; }
    public MicProfile MicProfile { get; private set; }

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    public PlayerNoteRecorder PlayerNoteRecorder { get; private set; }

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    public PlayerScoreController PlayerScoreController { get; private set; }

    private Voice voice;
    public Voice Voice
    {
        get
        {
            return voice;
        }
        private set
        {
            voice = value;
            sortedSentences = voice.Sentences.ToList();
            sortedSentences.Sort(Sentence.comparerByStartBeat);
        }
    }

    // The sorted sentences of the Voice
    private List<Sentence> sortedSentences = new List<Sentence>();

    [Inject]
    private Injector injector;

    // An injector with additional bindings, such as the PlayerProfile and the MicProfile.
    private Injector childrenInjector;

    [Inject]
    private PlayerUiArea playerUiArea;

    // The PlayerUiController is instantiated by the PlayerController as a child of the PlayerUiArea.
    private PlayerUiController playerUiController;

    [Inject]
    private SongMeta songMeta;

    private int displaySentenceIndex;
    private int recordingSentenceIndex;

    private LyricsDisplayer lyricsDisplayer;
    public LyricsDisplayer LyricsDisplayer
    {
        get
        {
            return lyricsDisplayer;
        }
        set
        {
            lyricsDisplayer = value;
            UpdateLyricsDisplayer(GetSentence(displaySentenceIndex), GetSentence(displaySentenceIndex + 1));
        }
    }

    private readonly Subject<SentenceRating> sentenceRatingStream = new Subject<SentenceRating>();

    void Start()
    {
        // Create effect when there are at least two perfect sentences in a row.
        // Therefor, consider the currently finished sentence and its predecessor.
        sentenceRatingStream.Buffer(2, 1)
            // All elements (i.e. the currently finished and its predecessor) must have been "perfect"
            .Where(xs => xs.AllMatch(x => x == SentenceRating.Perfect))
            // Create an effect for these.
            .Subscribe(xs => playerUiController.CreatePerfectSentenceEffect());
    }

    public void Init(PlayerProfile playerProfile, string voiceName, MicProfile micProfile)
    {
        this.PlayerProfile = playerProfile;
        this.MicProfile = micProfile;
        this.Voice = GetVoice(songMeta, voiceName);
        this.playerUiController = Instantiate(playerUiControllerPrefab, playerUiArea.transform);
        this.childrenInjector = CreateChildrenInjectorWithAdditionalBindings();

        // Inject all
        foreach (INeedInjection childThatNeedsInjection in GetComponentsInChildren<INeedInjection>())
        {
            childrenInjector.Inject(childThatNeedsInjection);
        }
        childrenInjector.Inject(playerUiController);

        // Init instances
        playerUiController.Init(PlayerProfile, MicProfile);
        PlayerScoreController.Init(Voice);

        SetDisplaySentenceIndex(0);
        recordingSentenceIndex = 0;
    }

    private Injector CreateChildrenInjectorWithAdditionalBindings()
    {
        Injector newInjector = UniInjectUtils.CreateInjector(injector);
        newInjector.AddBindingForInstance(PlayerProfile);
        newInjector.AddBindingForInstance(MicProfile);
        newInjector.AddBindingForInstance(PlayerNoteRecorder);
        newInjector.AddBindingForInstance(PlayerScoreController);
        newInjector.AddBindingForInstance(playerUiController);
        newInjector.AddBindingForInstance(newInjector);
        newInjector.AddBindingForInstance(this);
        return newInjector;
    }

    public void SetCurrentBeat(double currentBeat)
    {
        // Change the current display sentence, when the current beat is over its last note.
        if (displaySentenceIndex < sortedSentences.Count && currentBeat >= GetDisplaySentence().LinebreakBeat)
        {
            Sentence nextDisplaySentence = GetUpcomingSentenceForBeat(currentBeat);
            int nextDisplaySentenceIndex = sortedSentences.IndexOf(nextDisplaySentence);
            if (nextDisplaySentenceIndex >= 0)
            {
                SetDisplaySentenceIndex(nextDisplaySentenceIndex);
            }
        }

        // Score a sentence, when the current beat minus the mic delay is over its last note.
        int micDelayInMillis = (MicProfile == null) ? 0 : MicProfile.DelayInMillis;
        double micDelayInBeats = BpmUtils.MillisecondInSongToBeatWithoutGap(songMeta, micDelayInMillis);
        double currentBeatConsideringMicDelay = (micDelayInBeats > 0) ? currentBeat - micDelayInBeats : currentBeat;
        // The last sentence in the song should be scored as soon as possible such that one can continue to the next scene without waiting for the song to end.
        bool isOverLastRecordingSentence = (recordingSentenceIndex == sortedSentences.Count - 1) && currentBeatConsideringMicDelay >= GetRecordingSentence().MaxBeat;
        if (isOverLastRecordingSentence
            || (recordingSentenceIndex >= 0 && recordingSentenceIndex < sortedSentences.Count && currentBeatConsideringMicDelay >= GetRecordingSentence().LinebreakBeat))
        {
            FinishRecordingSentence(recordingSentenceIndex);
            Sentence nextRecordingSentence = GetUpcomingSentenceForBeat(currentBeatConsideringMicDelay);
            recordingSentenceIndex = sortedSentences.IndexOf(nextRecordingSentence);
        }
    }

    private Voice GetVoice(SongMeta songMeta, string voiceName)
    {
        IReadOnlyCollection<Voice> voices = songMeta.GetVoices();
        if (string.IsNullOrEmpty(voiceName) || voiceName == Voice.soloVoiceName)
        {
            Voice mergedVoice = CreateMergedVoice(voices);
            return mergedVoice;
        }
        else
        {
            Voice matchingVoice = voices.Where(it => it.Name == voiceName).FirstOrDefault();
            if (matchingVoice != null)
            {
                return matchingVoice;
            }
            else
            {
                string voiceNameCsv = voices.Select(it => it.Name).ToCsv();
                throw new UnityException($"The song data does not contain a voice with name {voiceName}."
                    + $" Available voices: {voiceNameCsv}");
            }
        }
    }

    private Voice CreateMergedVoice(IReadOnlyCollection<Voice> voices)
    {
        if (voices.Count == 1)
        {
            return voices.First();
        }

        Voice mergedVoice = new Voice("");
        List<Sentence> allSentences = voices.SelectMany(voice => voice.Sentences).ToList();
        List<Note> allNotes = allSentences.SelectMany(sentence => sentence.Notes).ToList();
        // Sort notes by start beat
        allNotes.Sort((note1, note2) => note1.StartBeat.CompareTo(note2.StartBeat));
        // Find sentence borders
        List<int> lineBreaks = allSentences.Select(sentence => sentence.LinebreakBeat).Where(lineBreak => lineBreak > 0).ToList();
        lineBreaks.Sort();
        int lineBreakIndex = 0;
        int nextLineBreakBeat = lineBreaks[lineBreakIndex];
        // Create sentences
        Sentence mutableSentence = new Sentence();
        foreach (Note note in allNotes)
        {
            if (!mutableSentence.Notes.IsNullOrEmpty()
                && (nextLineBreakBeat >= 0 && note.StartBeat > nextLineBreakBeat))
            {
                // Finish the last sentence
                mutableSentence.SetLinebreakBeat(nextLineBreakBeat);
                mergedVoice.AddSentence(mutableSentence);
                mutableSentence = new Sentence();

                lineBreakIndex++;
                if (lineBreakIndex < lineBreaks.Count)
                {
                    nextLineBreakBeat = lineBreaks[lineBreakIndex];
                }
                else
                {
                    lineBreakIndex = -1;
                }
            }
            mutableSentence.AddNote(note);
        }

        // Finish the last sentence
        mergedVoice.AddSentence(mutableSentence);
        return mergedVoice;
    }

    public void OnRecordedNoteEnded(RecordedNote recordedNote)
    {
        CheckPerfectlySungNote(recordedNote);
        DisplayRecordedNote(recordedNote);
    }

    public void OnRecordedNoteContinued(RecordedNote recordedNote, bool updateUi)
    {
        if (updateUi)
        {
            DisplayRecordedNote(recordedNote);
        }
    }

    private void CheckPerfectlySungNote(RecordedNote lastRecordedNote)
    {
        if (lastRecordedNote == null || lastRecordedNote.TargetNote == null)
        {
            return;
        }

        Note targetNote = lastRecordedNote.TargetNote;
        int targetMidiNoteRelative = MidiUtils.GetRelativePitch(targetNote.MidiNote);
        int recordedMidiNoteRelative = MidiUtils.GetRelativePitch(lastRecordedNote.RoundedMidiNote);
        bool isPerfect = ((targetMidiNoteRelative == recordedMidiNoteRelative)
            && (targetNote.StartBeat >= lastRecordedNote.StartBeat)
            && (targetNote.EndBeat <= lastRecordedNote.EndBeat));
        if (isPerfect)
        {
            playerUiController.CreatePerfectNoteEffect(targetNote);
        }
    }

    private void FinishRecordingSentence(int sentenceIndex)
    {
        PlayerNoteRecorder.OnSentenceEnded();

        Sentence recordingSentence = GetSentence(sentenceIndex);
        if (recordingSentence == null)
        {
            return;
        }

        List<RecordedNote> recordedNotes = PlayerNoteRecorder.GetRecordedNotes(recordingSentence);
        PlayerNoteRecorder.RemoveRecordedNotes(recordingSentence);
        SentenceRating sentenceRating = PlayerScoreController.CalculateScoreForSentence(recordingSentence, recordedNotes);
        playerUiController.ShowTotalScore(PlayerScoreController.TotalScore);
        if (sentenceRating != null)
        {
            playerUiController.ShowSentenceRating(sentenceRating);
            sentenceRatingStream.OnNext(sentenceRating);
        }
    }

    private void SetDisplaySentenceIndex(int newValue)
    {
        displaySentenceIndex = newValue;

        Sentence current = GetSentence(displaySentenceIndex);
        Sentence next = GetSentence(displaySentenceIndex + 1);

        // Update the UI
        playerUiController.DisplaySentence(current);
        UpdateLyricsDisplayer(current, next);
    }

    public void DisplayRecordedNote(RecordedNote recordedNote)
    {
        playerUiController.DisplayRecordedNote(recordedNote);
    }

    private void UpdateLyricsDisplayer(Sentence current, Sentence next)
    {
        if (lyricsDisplayer == null)
        {
            return;
        }

        lyricsDisplayer.SetCurrentSentence(current);
        lyricsDisplayer.SetNextSentence(next);
    }

    private Sentence GetSentence(int index)
    {
        Sentence sentence = (index >= 0 && index < sortedSentences.Count) ? sortedSentences[index] : null;
        return sentence;
    }

    public double GetNextStartBeat()
    {
        if (GetDisplaySentence() == null)
        {
            return -1d;
        }
        return GetDisplaySentence().MinBeat;
    }

    public Sentence GetUpcomingSentenceForBeat(double currentBeat)
    {
        Sentence result = Voice.Sentences
            .Where(sentence => currentBeat < sentence.LinebreakBeat)
            .FirstOrDefault();
        return result;
    }

    public Sentence GetDisplaySentence()
    {
        return GetSentence(displaySentenceIndex);
    }

    public Sentence GetRecordingSentence()
    {
        return GetSentence(recordingSentenceIndex);
    }

    public Note GetLastNote()
    {
        return sortedSentences.Last().Notes.OrderBy(note => note.EndBeat).Last();
    }
}
