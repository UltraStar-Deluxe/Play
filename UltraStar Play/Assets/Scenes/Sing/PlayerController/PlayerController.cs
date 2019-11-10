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
    public Voice Voice { get; private set; }

    private PlayerUiArea playerUiArea;
    private PlayerUiController playerUiController;
    public PlayerNoteRecorder PlayerNoteRecorder { get; set; }
    public PlayerScoreController PlayerScoreController { get; set; }

    private int sentenceIndex;
    public Sentence CurrentSentence { get; set; }
    public Sentence NextSentence { get; set; }

    private Difficulty Difficulty
    {
        get
        {
            return PlayerProfile.Difficulty;
        }
    }

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

    public void Init(SongMeta songMeta, PlayerProfile playerProfile, string voiceIdentifier)
    {
        this.SongMeta = songMeta;
        this.PlayerProfile = playerProfile;

        Voice = LoadVoice(songMeta, voiceIdentifier);

        playerUiArea = FindObjectOfType<PlayerUiArea>();

        PlayerScoreController = GetComponentInChildren<PlayerScoreController>();
        PlayerScoreController.Init(Voice);

        PlayerNoteRecorder = GetComponentInChildren<PlayerNoteRecorder>();
        if (PlayerNoteRecorder == null)
        {
            throw new NullReferenceException("PlayerNoteRecorder is null!");
        }
        PlayerNoteRecorder.Init(this, playerProfile.Difficulty.RoundingDistance);

        CreatePlayerUi();

        // Create effect when there are at least two perfect sentences in a row.
        // Therefor, consider the currently finished sentence and its predecessor.
        sentenceRatingStream.Buffer(2, 1)
            // All elements (i.e. the currently finished and its predecessor) must have been "perfect"
            .Where(xs => xs.All(x => x == SentenceRating.Perfect))
            // Create an effect for these.
            .Subscribe(xs => playerUiController.CreatePerfectSentenceEffect());

        sentenceIndex = 0;
        UpdateSentences(sentenceIndex);
    }

    private void CreatePlayerUi()
    {
        playerUiController = GameObject.Instantiate(playerUiControllerPrefab);
        RectTransform playerUiAreaTransform = playerUiArea.GetComponent<RectTransform>();
        playerUiController.GetComponent<RectTransform>().SetParent(playerUiAreaTransform);
        playerUiController.Init();
    }

    public void SetPositionInSongInMillis(double positionInSongInMillis)
    {
        // Change the current sentence, when the current beat is over its last note.
        double currentBeat = BpmUtils.MillisecondInSongToBeat(SongMeta, positionInSongInMillis);
        if (CurrentSentence != null && currentBeat > (double)CurrentSentence.EndBeat)
        {
            OnSentenceEnded();
        }
    }

    private Voice LoadVoice(SongMeta songMeta, string voiceIdentifier)
    {
        string filePath = songMeta.Directory + Path.DirectorySeparatorChar + songMeta.Filename;
        Debug.Log($"Loading voice of {filePath}");
        Dictionary<string, Voice> voices = VoicesBuilder.ParseFile(filePath, songMeta.Encoding, new List<string>());
        if (string.IsNullOrEmpty(voiceIdentifier))
        {
            return voices.Values.First();
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

    public void OnRecordedNoteEnded(RecordedNote lastRecordedNote)
    {
        CheckPerfectlySungNote(CurrentSentence, lastRecordedNote);
    }

    private void CheckPerfectlySungNote(Sentence sentence, RecordedNote lastRecordedNote)
    {
        if (sentence == null || lastRecordedNote == null)
        {
            return;
        }
        Note perfectlySungNote = sentence.Notes.Where(note =>
               note.MidiNote == lastRecordedNote.RoundedMidiNote
            && note.StartBeat >= lastRecordedNote.StartBeat
            && note.EndBeat <= lastRecordedNote.EndBeat).FirstOrDefault();
        if (perfectlySungNote != null)
        {
            playerUiController.CreatePerfectNoteEffect(perfectlySungNote);
        }
    }

    private void OnSentenceEnded()
    {
        List<RecordedNote> recordedNotes = PlayerNoteRecorder.GetRecordedNotes(CurrentSentence);
        SentenceRating sentenceRating = PlayerScoreController.CalculateScoreForSentence(CurrentSentence, recordedNotes);
        playerUiController.ShowTotalScore((int)PlayerScoreController.TotalScore);
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
        playerUiController.DisplayRecordedNotes(null);
        UpdateLyricsDisplayer();
    }

    public void DisplayRecordedNotes(List<RecordedNote> recordedNotes)
    {
        playerUiController.DisplayRecordedNotes(recordedNotes);
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
        Sentence sentence = (index < Voice.Sentences.Count) ? Voice.Sentences[index] : null;
        return sentence;
    }

    public double GetNextStartBeat()
    {
        if (CurrentSentence == null)
        {
            return double.MaxValue;
        }
        return CurrentSentence.StartBeat;
    }
}
