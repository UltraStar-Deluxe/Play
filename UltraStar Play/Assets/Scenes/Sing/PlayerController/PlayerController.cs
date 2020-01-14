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

    private int sentenceIndex;
    public Sentence CurrentSentence { get; set; }
    public Sentence NextSentence { get; set; }

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
            UpdateLyricsDisplayer();
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

        sentenceIndex = 0;
        UpdateSentences(sentenceIndex);
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
    }

    private Injector CreateChildrenInjectorWithAdditionalBindings()
    {
        Injector newInjector = UniInjectUtils.CreateInjector(injector);
        newInjector.AddBindingForInstance(PlayerProfile);
        newInjector.AddBindingForInstance(MicProfile);
        newInjector.AddBindingForInstance(PlayerNoteRecorder);
        newInjector.AddBindingForInstance(PlayerScoreController);
        newInjector.AddBindingForInstance(playerUiController);
        newInjector.AddBindingForInstance(this);
        return newInjector;
    }

    public void SetCurrentBeat(double currentBeat)
    {
        // Change the current sentence, when the current beat is over its last note.
        if (CurrentSentence != null && currentBeat >= (double)CurrentSentence.MaxBeat)
        {
            OnSentenceEnded();
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

    public void OnRecordedNoteContinued(RecordedNote recordedNote)
    {
        DisplayRecordedNote(recordedNote);
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

    private void OnSentenceEnded()
    {
        PlayerNoteRecorder.OnSentenceEnded();
        List<RecordedNote> recordedNotes = PlayerNoteRecorder.GetRecordedNotes(CurrentSentence);
        SentenceRating sentenceRating = PlayerScoreController.CalculateScoreForSentence(CurrentSentence, recordedNotes);
        playerUiController.ShowTotalScore(PlayerScoreController.TotalScore);
        if (sentenceRating != null)
        {
            playerUiController.ShowSentenceRating(sentenceRating);
            sentenceRatingStream.OnNext(sentenceRating);
        }

        sentenceIndex++;
        UpdateSentences(sentenceIndex);
    }

    private void UpdateSentences(int currentSentenceIndex)
    {
        Sentence lastSentence = CurrentSentence;
        CurrentSentence = GetSentence(currentSentenceIndex);
        NextSentence = GetSentence(currentSentenceIndex + 1);

        if (lastSentence != CurrentSentence && CurrentSentence == null)
        {
            Debug.Log("Finished last sentence");
        }
        if (lastSentence == null && CurrentSentence == null)
        {
            Debug.Log("Song contains no sentences");
        }

        // Update the UI
        playerUiController.DisplaySentence(CurrentSentence);
        UpdateLyricsDisplayer();
    }

    public void DisplayRecordedNote(RecordedNote recordedNote)
    {
        playerUiController.DisplayRecordedNote(recordedNote);
    }

    private void UpdateLyricsDisplayer()
    {
        if (lyricsDisplayer != null)
        {
            lyricsDisplayer.SetCurrentSentence(CurrentSentence);
            lyricsDisplayer.SetNextSentence(NextSentence);
        }
    }

    private Sentence GetSentence(int index)
    {
        Sentence sentence = (index < sortedSentences.Count) ? sortedSentences[index] : null;
        return sentence;
    }

    public double GetNextStartBeat()
    {
        if (CurrentSentence == null)
        {
            return -1d;
        }
        return CurrentSentence.MinBeat;
    }
}
