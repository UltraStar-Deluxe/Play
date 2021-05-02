using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.EventSystems;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorCopyPasteManager : MonoBehaviour, INeedInjection
{
    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private EditorNoteDisplayer editorNoteDisplayer;

    [Inject]
    private SongEditorSelectionController selectionController;

    [Inject]
    private SongEditorLayerManager layerManager;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private EventSystem eventSystem;

    private Voice copiedVoice;

    public List<Note> CopiedNotes
    {
        get
        {
            return layerManager.GetNotes(ESongEditorLayer.CopyPaste);
        }
    }

    void Start()
    {
        songAudioPlayer.PositionInSongEventStream.Subscribe(newMillis => MoveCopiedNotesToMillisInSong(newMillis));

        // Copy action
        InputManager.GetInputAction(R.InputActions.songEditor_copy).PerformedAsObservable()
            .Where(_ => !GameObjectUtils.InputFieldHasFocus(eventSystem))
            .Subscribe(_ => CopySelectedNotes());
        
        // Paste action
        InputManager.GetInputAction(R.InputActions.songEditor_paste).PerformedAsObservable()
            .Where(_ => !GameObjectUtils.InputFieldHasFocus(eventSystem))
            .Subscribe(_ => PasteCopiedNotes());
        
        // Cancel copy
        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(SongEditorSceneInputControl.cancelCopyPriority)
            .Where(_ => !GameObjectUtils.InputFieldHasFocus(eventSystem))
            .Where(_ => HasCopiedNotes())
            .Subscribe(_ =>
            {
                ClearCopiedNotes();
                InputManager.GetInputAction(R.InputActions.usplay_back).CancelNotifyForThisFrame();
            });
    }

    private void MoveCopiedNotesToMillisInSong(double newMillis)
    {
        if (CopiedNotes.IsNullOrEmpty() || songAudioPlayer.IsPlaying)
        {
            return;
        }

        int newBeat = (int)Math.Round(BpmUtils.MillisecondInSongToBeat(songMeta, newMillis));
        int minBeat = CopiedNotes.Select(it => it.StartBeat).Min();
        int distanceInBeats = newBeat - minBeat;
        if (distanceInBeats == 0)
        {
            return;
        }

        foreach (Note note in CopiedNotes)
        {
            note.MoveHorizontal(distanceInBeats);
        }

        editorNoteDisplayer.UpdateNotes();
    }

    private void ClearCopiedNotes()
    {
        List<Note> notes = layerManager.GetNotes(ESongEditorLayer.CopyPaste);
        notes.ForEach(note => editorNoteDisplayer.DeleteNote(note));
        layerManager.ClearLayer(ESongEditorLayer.CopyPaste);
    }

    public bool HasCopiedNotes()
    {
        return !layerManager.GetNotes(ESongEditorLayer.CopyPaste).IsNullOrEmpty();
    }
    
    public void PasteCopiedNotes()
    {
        if (!HasCopiedNotes())
        {
            return;
        }
        
        int minBeat = CopiedNotes.Select(it => it.StartBeat).Min();
        Sentence sentenceAtBeatWithVoice = SongMetaUtils.GetSentencesAtBeat(songMeta, minBeat)
            .Where(it => it.Voice != null).FirstOrDefault();

        // Find voice to insert the notes into
        Voice voice;
        if (copiedVoice != null)
        {
            voice = copiedVoice;
        }
        else if (sentenceAtBeatWithVoice != null)
        {
            voice = sentenceAtBeatWithVoice.Voice;
        }
        else
        {
            voice = songMeta.GetVoices().FirstOrDefault();
        }

        // Add the notes to the voice
        foreach (Note note in CopiedNotes)
        {
            InsertNote(note, voice);
        }
        ClearCopiedNotes();

        songMetaChangeEventStream.OnNext(new NotesAddedEvent());
    }

    private void InsertNote(Note note, Voice voice)
    {
        Sentence sentenceAtBeatOfVoice = SongMetaUtils.GetSentencesAtBeat(songMeta, note.StartBeat)
            .Where(sentence => sentence.Voice == voice).FirstOrDefault();
        if (sentenceAtBeatOfVoice == null)
        {
            // Add sentence with note
            Sentence newSentence = new Sentence(new List<Note> { note }, note.EndBeat);
            newSentence.SetVoice(voice);
        }
        else
        {
            // Add note to existing sentence
            note.SetSentence(sentenceAtBeatOfVoice);
        }
    }

    public void CopySelectedNotes()
    {
        // Remove any old copied notes from the Ui.
        foreach (Note note in CopiedNotes)
        {
            editorNoteDisplayer.DeleteNote(note);
        }

        ClearCopiedNotes();

        List<Note> selectedNotes = selectionController.GetSelectedNotes();
        foreach (Note note in selectedNotes)
        {
            copiedVoice = note.Sentence?.Voice;
            Note noteCopy = note.Clone();
            noteCopy.SetSentence(null);
            CopiedNotes.Add(noteCopy);
            layerManager.AddNoteToLayer(ESongEditorLayer.CopyPaste, noteCopy);
        }

        selectionController.SetSelection(CopiedNotes);

        editorNoteDisplayer.UpdateNotes();
    }
}
