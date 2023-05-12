using System;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine;
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
    private SongEditorSelectionControl selectionControl;

    [Inject]
    private SongEditorLayerManager layerManager;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject(Optional = true)]
    private EventSystem eventSystem;

    [Inject]
    private MoveNotesToOtherVoiceAction moveNotesToOtherVoiceAction;

    [Inject]
    private DeleteNotesAction deleteNotesAction;

    [Inject]
    private SongEditorSceneInputControl songEditorSceneInputControl;

    
    public bool HasCopiedNotes => copyPasteData != null && !copyPasteData.copiedNotes.IsNullOrEmpty();

    private SongEditorCopyPasteData copyPasteData;

    private void Start()
    {
        // Copy action
        InputManager.GetInputAction(R.InputActions.songEditor_copy).PerformedAsObservable()
            .Where(_ => !songEditorSceneInputControl.AnyInputFieldHasFocus())
            .Subscribe(_ => CopySelectedNotes());

        // Cut action
        InputManager.GetInputAction(R.InputActions.songEditor_cut).PerformedAsObservable()
            .Where(_ => !songEditorSceneInputControl.AnyInputFieldHasFocus())
            .Subscribe(_ => CutSelectedNotes());
        
        // Paste action
        InputManager.GetInputAction(R.InputActions.songEditor_paste).PerformedAsObservable()
            .Where(_ => !songEditorSceneInputControl.AnyInputFieldHasFocus())
            .Subscribe(_ => PasteCopiedNotes());
    }

    public void PasteCopiedNotes()
    {
        if (!HasCopiedNotes)
        {
            return;
        }

        List<Note> pastedNotes = new();

        if (copyPasteData.copiedNotes.IsNullOrEmpty())
        {
            return;
        }

        // Shift to playback position
        int currentBeat = (int)songAudioPlayer.GetCurrentBeat(true);
        int minBeat = copyPasteData.copiedNotes.Select(it => it.StartBeat).Min();
        int distanceInBeats = currentBeat - minBeat;
        
        // Paste to enum layer
        foreach (Note copiedNote in copyPasteData.copiedNotes)
        {
            if (copyPasteData.copiedNoteToLayerMap.TryGetValue(copiedNote, out ESongEditorLayer layerEnum))
            {
                Note pastedNote = copiedNote.Clone();
                pastedNote.IsEditable = true;
                pastedNote.SetStartAndEndBeat(
                    pastedNote.StartBeat + distanceInBeats,
                    pastedNote.EndBeat + distanceInBeats);
                
                layerManager.AddNoteToEnumLayer(layerEnum, pastedNote);
                pastedNotes.Add(pastedNote);
            }
        }

        // Paste to original voice
        songMeta.GetVoices().ForEach(voice =>
        {
            List<Note> copiedNotesFromVoice = copyPasteData.copiedNotes
                .Where(copiedNote => !copyPasteData.copiedNoteToLayerMap.ContainsKey(copiedNote)
                                     && copyPasteData.copiedNoteToOriginalVoiceMap.ContainsKey(copiedNote)
                                     && copyPasteData.copiedNoteToOriginalVoiceMap[copiedNote] == voice)
                .ToList();
            List<Note> pastedNotesFromVoice = copiedNotesFromVoice.Select(copiedNote =>
            {
                Note pastedNote = copiedNote.Clone();
                pastedNote.IsEditable = true;
                pastedNote.SetSentence(null);
                pastedNote.SetStartAndEndBeat(
                    pastedNote.StartBeat + distanceInBeats,
                    pastedNote.EndBeat + distanceInBeats);
                return pastedNote;
            }).ToList();
            
            moveNotesToOtherVoiceAction.MoveNotesToVoice(songMeta, pastedNotesFromVoice, voice.Name);
            pastedNotes.AddRange(pastedNotesFromVoice);
        });

        // Set correct editable status
        pastedNotes.ForEach(pastedNote =>
        {
            if (layerManager.TryGetEnumLayer(pastedNote, out SongEditorEnumLayer enumLayer))
            {
                pastedNote.IsEditable = layerManager.IsEnumLayerEditable(enumLayer.LayerEnum);
            }
            else
            {
                string voiceName = pastedNote.Sentence?.Voice?.Name;
                pastedNote.IsEditable = layerManager.IsVoiceLayerEditable(voiceName);
            }
        });

        // Select copied notes.
        selectionControl.SetSelection(pastedNotes);

        songMetaChangeEventStream.OnNext(new NotesPastedEvent());
    }

    public void CutSelectedNotes()
    {
        List<Note> selectedNotes = selectionControl.GetSelectedNotes();
        if (selectedNotes.IsNullOrEmpty())
        {
            return;
        }
        CopySelectedNotes();
        deleteNotesAction.Execute(selectedNotes);
        songMetaChangeEventStream.OnNext(new NotesCutEvent());
    }

    public void CopySelectedNotes()
    {
        CopyNotes(selectionControl.GetSelectedNotes());
    }
    
    private void CopyNotes(List<Note> notes)
    {
        copyPasteData = new SongEditorCopyPasteData();

        notes.ForEach(note =>
        {
            layerManager.TryGetEnumLayer(note, out SongEditorEnumLayer layer);

            Note copiedNote = note.Clone();
            copiedNote.SetSentence(null);
            
            copyPasteData.copiedNotes.Add(copiedNote);

            if (note.Sentence != null
                && note.Sentence.Voice != null)
            {
                copyPasteData.copiedNoteToOriginalSentenceMap[copiedNote] = note.Sentence;
                copyPasteData.copiedNoteToOriginalVoiceMap[copiedNote] = note.Sentence.Voice;
            }
            else
            {
                if (layerManager.TryGetLayerEnumOfNote(note, out ESongEditorLayer layerEnum))
                {
                    copyPasteData.copiedNoteToLayerMap[copiedNote] = layerEnum;
                }
            }
        });

        selectionControl.ClearSelection();

        editorNoteDisplayer.UpdateNotes();
    }

    private class SongEditorCopyPasteData
    {
        // Flag to check whether deserialized JSON is actually copy paste data.
        public List<Note> copiedNotes = new();
        public Dictionary<Note, ESongEditorLayer> copiedNoteToLayerMap = new();
        public Dictionary<Note, Sentence> copiedNoteToOriginalSentenceMap = new();
        public Dictionary<Note, Voice> copiedNoteToOriginalVoiceMap = new();
    }
}
