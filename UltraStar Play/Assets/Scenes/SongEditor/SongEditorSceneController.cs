using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UI;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorSceneController : MonoBehaviour, IBinder, INeedInjection
{
    [InjectedInInspector]
    public string defaultSongName;

    [TextArea(3, 8)]
    [Tooltip("Convenience text field to paste and copy song names when debugging.")]
    public string defaultSongNamePasteBin;

    [InjectedInInspector]
    public SongAudioPlayer songAudioPlayer;

    [InjectedInInspector]
    public SongVideoPlayer songVideoPlayer;

    [InjectedInInspector]
    public SongEditorNoteRecorder songEditorNoteRecorder;

    [InjectedInInspector]
    public SongEditorSelectionController selectionController;

    [InjectedInInspector]
    public RectTransform uiNoteContainer;

    [InjectedInInspector]
    public AudioWaveFormVisualizer audioWaveFormVisualizer;

    [InjectedInInspector]
    public NoteArea noteArea;

    [InjectedInInspector]
    public NoteAreaDragHandler noteAreaDragHandler;

    [InjectedInInspector]
    public EditorNoteDisplayer editorNoteDisplayer;

    [InjectedInInspector]
    public MicrophonePitchTracker microphonePitchTracker;

    [InjectedInInspector]
    public Canvas canvas;

    [InjectedInInspector]
    public GraphicRaycaster graphicRaycaster;

    [InjectedInInspector]
    public SongEditorHistoryManager historyManager;

    private bool lastIsPlaying;
    private double positionInSongInMillisWhenPlaybackStarted;

    private readonly Dictionary<Voice, Color> voiceToColorMap = new Dictionary<Voice, Color>();

    private readonly SongEditorLayerManager songEditorLayerManager = new SongEditorLayerManager();

    private bool audioWaveFormInitialized;

    public SongMeta SongMeta
    {
        get
        {
            return SceneData.SelectedSongMeta;
        }
    }

    private SongEditorSceneData sceneData;
    public SongEditorSceneData SceneData
    {
        get
        {
            if (sceneData == null)
            {
                sceneData = SceneNavigator.Instance.GetSceneData<SongEditorSceneData>(CreateDefaultSceneData());
            }
            return sceneData;
        }
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new BindingBuilder();
        // Note that the SceneData and SongMeta are loaded on access here if not done yet.
        bb.BindExistingInstance(SceneData);
        bb.BindExistingInstance(SongMeta);
        bb.BindExistingInstance(songAudioPlayer);
        bb.BindExistingInstance(songVideoPlayer);
        bb.BindExistingInstance(noteArea);
        bb.BindExistingInstance(noteAreaDragHandler);
        bb.BindExistingInstance(songEditorLayerManager);
        bb.BindExistingInstance(microphonePitchTracker);
        bb.BindExistingInstance(songEditorNoteRecorder);
        bb.BindExistingInstance(selectionController);
        bb.BindExistingInstance(editorNoteDisplayer);
        bb.BindExistingInstance(canvas);
        bb.BindExistingInstance(graphicRaycaster);
        bb.BindExistingInstance(historyManager);
        bb.BindExistingInstance(this);
        return bb.GetBindings();
    }

    void Awake()
    {
        Debug.Log($"Start editing of '{SceneData.SelectedSongMeta.Title}' at {SceneData.PositionInSongInMillis} ms.");
        songAudioPlayer.Init(SongMeta);
        songVideoPlayer.Init(SongMeta, songAudioPlayer);

        songAudioPlayer.PositionInSongInMillis = SceneData.PositionInSongInMillis;
    }

    void Update()
    {
        // Jump to last position in song when playback stops
        if (songAudioPlayer.IsPlaying)
        {
            if (!lastIsPlaying)
            {
                positionInSongInMillisWhenPlaybackStarted = songAudioPlayer.PositionInSongInMillis;
            }
        }
        else
        {
            if (lastIsPlaying)
            {
                songAudioPlayer.PositionInSongInMillis = positionInSongInMillisWhenPlaybackStarted;
            }
        }
        lastIsPlaying = songAudioPlayer.IsPlaying;

        // Create the audio waveform image if not done yet.
        if (!audioWaveFormInitialized && songAudioPlayer.HasAudioClip && songAudioPlayer.AudioClip.samples > 0)
        {
            using (new DisposableStopwatch($"Created audio waveform in <millis> ms"))
            {
                audioWaveFormInitialized = true;
                audioWaveFormVisualizer.DrawWaveFormMinAndMaxValues(songAudioPlayer.AudioClip);
            }
        }
    }

    public Color GetColorForVoice(Voice voice)
    {
        if (voiceToColorMap.TryGetValue(voice, out Color color))
        {
            return color;
        }
        else
        {
            // Define colors for the voices.
            CreateVoiceToColorMap();
            return voiceToColorMap[voice];
        }
    }

    private void CreateVoiceToColorMap()
    {
        List<Color> colors = new List<Color> { Colors.beige, Colors.lightSeaGreen };
        int index = 0;
        foreach (Voice v in SongMeta.GetVoices())
        {
            if (index < colors.Count)
            {
                voiceToColorMap[v] = colors[index];
            }
            else
            {
                // fallback color
                voiceToColorMap[v] = Colors.beige;
            }
            index++;
        }
    }

    public List<Note> GetFollowingNotes(List<Note> notes)
    {
        int maxBeat = notes.Select(it => it.EndBeat).Max();
        List<Note> result = GetAllSentences()
            .SelectMany(sentence => sentence.Notes)
            .Where(note => note.StartBeat >= maxBeat)
            .ToList();
        return result;
    }

    // Returns the notes in the song as well as the notes in the layers in no particular order.
    public List<Note> GetAllNotes()
    {
        List<Note> result = new List<Note>();
        List<Note> notesInVoices = GetAllSentences().SelectMany(sentence => sentence.Notes).ToList();
        List<Note> notesInLayers = songEditorLayerManager.GetAllNotes();
        result.AddRange(notesInVoices);
        result.AddRange(notesInLayers);
        return result;
    }

    public List<Sentence> GetAllSentences()
    {
        List<Sentence> result = new List<Sentence>();
        List<Sentence> sentencesInVoices = SongMeta.GetVoices().SelectMany(voice => voice.Sentences).ToList();
        List<Note> notesInLayers = songEditorLayerManager.GetAllNotes();
        result.AddRange(sentencesInVoices);
        return result;
    }

    public Sentence GetNextSentence(Sentence sentence)
    {
        List<Sentence> sentencesOfVoice = SongMeta.GetVoices().Where(voice => voice == sentence.Voice)
            .SelectMany(voiceIdToVoiceMap => voiceIdToVoiceMap.Sentences).ToList();
        sentencesOfVoice.Sort(Sentence.comparerByStartBeat);
        Sentence lastSentence = null;
        foreach (Sentence s in sentencesOfVoice)
        {
            if (lastSentence == sentence)
            {
                return s;
            }
            lastSentence = s;
        }
        return null;
    }

    public Sentence GetPreviousSentence(Sentence sentence)
    {
        List<Sentence> sentencesOfVoice = SongMeta.GetVoices().Where(voice => voice == sentence.Voice)
            .SelectMany(voiceIdToVoiceMap => voiceIdToVoiceMap.Sentences).ToList();
        sentencesOfVoice.Sort(Sentence.comparerByStartBeat);
        Sentence lastSentence = null;
        foreach (Sentence s in sentencesOfVoice)
        {
            if (s == sentence)
            {
                return lastSentence;
            }
            lastSentence = s;
        }
        return null;
    }

    public Voice GetOrCreateVoice(string voiceName)
    {
        Voice matchingVoice = SongMeta.GetVoices()
            .Where(it => it.Name == voiceName || (voiceName.IsNullOrEmpty() && it.Name == Voice.soloVoiceName))
            .FirstOrDefault();
        if (matchingVoice != null)
        {
            return matchingVoice;
        }

        // Create new voice.
        // Set voice identifier for solo voice because this is not a solo song anymore.
        Voice soloVoice = SongMeta.GetVoices().Where(it => it.Name == Voice.soloVoiceName).FirstOrDefault();
        if (soloVoice != null)
        {
            soloVoice.SetName(Voice.firstVoiceName);
        }

        Voice newVoice = new Voice(voiceName);
        SongMeta.AddVoice(newVoice);

        return newVoice;
    }

    public Sentence GetSentenceForNote(Note note, Voice voice)
    {
        foreach (Sentence sentence in voice.Sentences)
        {
            if (sentence.MinBeat <= note.StartBeat && note.EndBeat <= sentence.ExtendedMaxBeat)
            {
                return sentence;
            };
        }
        return null;
    }

    public List<Sentence> GetSentencesAtBeat(int beat)
    {
        return SongMeta.GetVoices().SelectMany(voice => voice.Sentences)
            .Where(sentence => IsBeatInSentence(sentence, beat)).ToList();
    }

    public bool IsBeatInSentence(Sentence sentence, int beat)
    {
        return sentence.MinBeat <= beat && beat <= Math.Max(sentence.MaxBeat, sentence.LinebreakBeat);
    }

    public void OnNotesChanged()
    {
        editorNoteDisplayer.ReloadSentences();
        editorNoteDisplayer.UpdateNotesAndSentences();

        historyManager.AddUndoState();
    }

    public void DeleteNote(Note note)
    {
        note.SetSentence(null);
        songEditorLayerManager.RemoveNoteFromAllLayers(note);
        editorNoteDisplayer.DeleteNote(note);
    }

    public void DeleteNotes(IReadOnlyCollection<Note> notes)
    {
        foreach (Note note in new List<Note>(notes))
        {
            DeleteNote(note);
        }
    }

    public void DeleteSentence(Sentence sentence)
    {
        DeleteNotes(sentence.Notes);
        sentence.SetVoice(null);
        editorNoteDisplayer.ReloadSentences();
    }

    public void TogglePlayPause()
    {
        if (songAudioPlayer.IsPlaying)
        {
            songAudioPlayer.PauseAudio();
        }
        else
        {
            songAudioPlayer.PlayAudio();
        }
    }

    public void OnBackButtonClicked()
    {
        ContinueToSingScene();
    }

    public void OnSaveButtonClicked()
    {
        SaveSong();
    }

    private void SaveSong()
    {
        // Create backup of original file if not done yet.
        string songFile = SongMeta.Directory + Path.DirectorySeparatorChar + SongMeta.Filename;
        string backupFile = SongMeta.Directory + Path.DirectorySeparatorChar + SongMeta.Filename.Replace(".txt", ".txt.bak");
        if (!File.Exists(backupFile))
        {
            File.Copy(songFile, backupFile);
        }
        // Write the song data structure to the file.
        UltraStarSongFileWriter.WriteFile(songFile, SongMeta);
    }

    private void ContinueToSingScene()
    {
        if (sceneData.PreviousSceneData is SingSceneData)
        {
            SingSceneData singSceneData = sceneData.PreviousSceneData as SingSceneData;
            singSceneData.PositionInSongInMillis = songAudioPlayer.PositionInSongInMillis;
        }
        SceneNavigator.Instance.LoadScene(sceneData.PreviousScene, sceneData.PreviousSceneData);
    }

    private SongEditorSceneData CreateDefaultSceneData()
    {
        SongEditorSceneData defaultSceneData = new SongEditorSceneData();
        defaultSceneData.PositionInSongInMillis = 0;
        defaultSceneData.SelectedSongMeta = SongMetaManager.Instance.FindSongMeta(defaultSongName);

        // Set up PreviousSceneData to directly start the SingScene.
        defaultSceneData.PreviousScene = EScene.SingScene;

        SingSceneData singSceneData = new SingSceneData();
        singSceneData.SelectedSongMeta = defaultSceneData.SelectedSongMeta;
        PlayerProfile playerProfile = SettingsManager.Instance.Settings.PlayerProfiles[0];
        List<PlayerProfile> playerProfiles = new List<PlayerProfile>();
        playerProfiles.Add(playerProfile);
        singSceneData.SelectedPlayerProfiles = playerProfiles;

        defaultSceneData.PreviousSceneData = singSceneData;

        return defaultSceneData;
    }
}
