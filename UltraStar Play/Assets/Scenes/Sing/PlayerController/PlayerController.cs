using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniRx;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public PlayerUiController playerUiControllerPrefab;

    public SongMeta SongMeta { get; private set; }
    public PlayerProfile PlayerProfile { get; private set; }
    public MicProfile MicProfile { get; private set; }

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
    private List<Sentence> sortedSentences = new List<Sentence>();

    private PlayerUiArea playerUiArea;
    private PlayerUiController playerUiController;
    public PlayerNoteRecorder PlayerNoteRecorder { get; set; }
    public PlayerScoreController PlayerScoreController { get; set; }

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

    void Awake()
    {
        playerUiArea = FindObjectOfType<PlayerUiArea>();
        PlayerScoreController = GetComponentInChildren<PlayerScoreController>();
        PlayerNoteRecorder = GetComponentInChildren<PlayerNoteRecorder>();
    }

    void Start()
    {
        CreatePlayerUi();
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

    public void Init(SongMeta songMeta, PlayerProfile playerProfile, string voiceIdentifier, MicProfile micProfile)
    {
        this.SongMeta = songMeta;
        this.PlayerProfile = playerProfile;
        this.MicProfile = micProfile;

        Voice = LoadVoice(songMeta, voiceIdentifier);
        PlayerScoreController.Init(Voice);
        PlayerNoteRecorder.Init(this, playerProfile, micProfile);
    }

    private void CreatePlayerUi()
    {
        playerUiController = Instantiate(playerUiControllerPrefab, playerUiArea.transform);
        playerUiController.Init(PlayerProfile, MicProfile);
    }

    public void SetCurrentBeat(double currentBeat)
    {
        // Change the current sentence, when the current beat is over its last note.
        if (CurrentSentence != null && currentBeat >= (double)CurrentSentence.MaxBeat)
        {
            OnSentenceEnded();
        }
    }

    private Voice LoadVoice(SongMeta songMeta, string voiceIdentifier)
    {
        Dictionary<string, Voice> voices = SongMetaManager.GetVoices(songMeta);
        if (string.IsNullOrEmpty(voiceIdentifier))
        {
            Voice mergedVoice = CreateMergedVoice(voices);
            return mergedVoice;
        }
        else
        {
            if (voices.TryGetValue(voiceIdentifier, out Voice loadedVoice))
            {
                return loadedVoice;
            }
            else
            {
                throw new Exception($"The song does not contain a voice for {voiceIdentifier}");
            }
        }
    }

    private Voice CreateMergedVoice(Dictionary<string, Voice> voices)
    {
        if (voices.Count == 1)
        {
            return voices.Values.First();
        }

        MutableVoice mergedVoice = new MutableVoice();
        List<Sentence> allSentences = voices.Values.SelectMany(voice => voice.Sentences).ToList();
        List<Note> allNotes = allSentences.SelectMany(sentence => sentence.Notes).ToList();
        // Sort notes by start beat
        allNotes.Sort((note1, note2) => note1.StartBeat.CompareTo(note2.StartBeat));
        // Find sentence borders
        List<int> lineBreaks = allSentences.Select(sentence => sentence.LinebreakBeat).Where(lineBreak => lineBreak > 0).ToList();
        lineBreaks.Sort();
        int lineBreakIndex = 0;
        int nextLineBreakBeat = lineBreaks[lineBreakIndex];
        // Create sentences
        MutableSentence mutableSentence = new MutableSentence();
        foreach (Note note in allNotes)
        {
            if (!mutableSentence.GetNotes().IsNullOrEmpty()
                && (nextLineBreakBeat >= 0 && note.StartBeat > nextLineBreakBeat))
            {
                // Finish the last sentence
                mutableSentence.SetLinebreakBeat(nextLineBreakBeat);
                mergedVoice.Add((Sentence)mutableSentence);
                mutableSentence = new MutableSentence();

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
            mutableSentence.Add(note);
        }

        // Finish the last sentence
        mergedVoice.Add((Sentence)mutableSentence);
        return (Voice)mergedVoice;
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
