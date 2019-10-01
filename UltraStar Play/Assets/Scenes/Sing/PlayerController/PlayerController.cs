using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public PlayerUiController playerUiControllerPrefab;

    private SongMeta songMeta;
    private PlayerProfile playerProfile;
    private Voice voice;

    private PlayerUiArea playerUiArea;
    private PlayerUiController playerUiController;
    private PlayerScoreController playerScoreController;
    private PlayerNoteRecorder playerNoteRecorder;

    private int sentenceIndex;
    public Sentence CurrentSentence { get; set; }
    public Sentence NextSentence { get; set; }

    public int Score { get; private set; }

    public RecordedSentence RecordedSentence { get; set; }

    private LyricsDisplayer lyricsDisplayer;
    public LyricsDisplayer LyricsDisplayer
    {
        set
        {
            lyricsDisplayer = value;
            UpdateLyricsDisplayer();
        }
    }

    public void Init(SongMeta songMeta, PlayerProfile playerProfile, string voiceIdentifier)
    {
        this.songMeta = songMeta;
        this.playerProfile = playerProfile;

        voice = LoadVoice(songMeta, voiceIdentifier);

        playerUiArea = FindObjectOfType<PlayerUiArea>();

        playerScoreController = GetComponentInChildren<PlayerScoreController>();
        playerScoreController.Init(voice);

        playerNoteRecorder = GetComponentInChildren<PlayerNoteRecorder>();
        playerNoteRecorder.Init(this);

        CreatePlayerUi();

        sentenceIndex = 0;
        UpdateSentences(sentenceIndex);
    }

    private void CreatePlayerUi()
    {
        playerUiController = GameObject.Instantiate(playerUiControllerPrefab);
        RectTransform playerUiAreaTransform = playerUiArea.GetComponent<RectTransform>();
        playerUiController.GetComponent<RectTransform>().SetParent(playerUiAreaTransform);
        playerUiController.Init(songMeta, voice, playerProfile);
    }

    public void SetPositionInSongInMillis(double positionInSongInMillis)
    {
        // Change the current sentence, when the current beat is over its last note.
        double currentBeat = BpmUtils.MillisecondInSongToBeat(songMeta, positionInSongInMillis);
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

    private void OnSentenceEnded()
    {
        int scoreForSentence = playerScoreController.CalculateScoreForSentence(CurrentSentence, RecordedSentence);
        Score += scoreForSentence;

        SentenceRating sentenceRating = playerScoreController.GetSentenceRating(CurrentSentence, scoreForSentence);
        if (sentenceRating != null)
        {
            playerUiController.ShowSentenceRating(sentenceRating, scoreForSentence);
        }

        sentenceIndex++;
        UpdateSentences(sentenceIndex);
    }

    private void UpdateSentences(int currentSentenceIndex)
    {
        CurrentSentence = GetSentence(currentSentenceIndex);
        NextSentence = GetSentence(currentSentenceIndex + 1);

        // Update the UI
        playerUiController.SetCurrentSentence(CurrentSentence);
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
        Sentence sentence = (index < voice.Sentences.Count - 1) ? voice.Sentences[index] : null;
        return sentence;
    }
}
