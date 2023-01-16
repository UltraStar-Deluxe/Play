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

    public List<Note> CopiedNotes
    {
        get
        {
            return layerManager.GetEnumLayerNotes(ESongEditorLayer.CopyPaste);
        }
    }

    private readonly Dictionary<Note, OriginalNoteCopyData> copiedNoteToOriginalDataMap = new();

    void Start()
    {
        songAudioPlayer.PositionInSongEventStream.Subscribe(newMillis => MoveCopiedNotesToMillisInSong(newMillis));

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
        
        // Cancel copy
        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(SongEditorSceneInputControl.cancelCopyPriority)
            .Where(_ => !songEditorSceneInputControl.AnyInputFieldHasFocus())
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
            note.IsEditable = true;
            note.MoveHorizontal(distanceInBeats);
            note.IsEditable = false;
        }

        editorNoteDisplayer.UpdateNotes();
    }

    private void ClearCopiedNotes()
    {
        CopiedNotes.ForEach(copiedNote => editorNoteDisplayer.RemoveNoteControl(copiedNote));
        layerManager.ClearEnumLayer(ESongEditorLayer.CopyPaste);
        copiedNoteToOriginalDataMap.Clear();
    }

    public bool HasCopiedNotes()
    {
        return !layerManager.GetEnumLayerNotes(ESongEditorLayer.CopyPaste).IsNullOrEmpty();
    }
    
    public void PasteCopiedNotes()
    {
        if (!HasCopiedNotes())
        {
            return;
        }

        List<Note> pastedNotes = new();

        // Paste to original layer
        layerManager.GetEnumLayers().ForEach(layer =>
        {
            List<Note> copiedNotesFromLayer = CopiedNotes
                .Where(copiedNote => copiedNoteToOriginalDataMap[copiedNote].OriginalEnumLayer == layer)
                .ToList();

            copiedNotesFromLayer.ForEach(copiedNote =>
            {
                copiedNote.IsEditable = true;
                layerManager.RemoveNoteFromAllEnumLayers(copiedNote);
                layerManager.AddNoteToEnumLayer(layer.LayerEnum, copiedNote);
                pastedNotes.Add(copiedNote);
            });
        });

        // Paste to original voice
        songMeta.GetVoices().ForEach(voice =>
        {
            List<Note> copiedNotesFromVoice = CopiedNotes
                .Where(copiedNote => copiedNoteToOriginalDataMap[copiedNote].OriginalEnumLayer == null
                                     && copiedNoteToOriginalDataMap[copiedNote].OriginalVoice == voice)
                .ToList();
            copiedNotesFromVoice.ForEach(copiedNote =>
            {
                copiedNote.IsEditable = true;
                layerManager.RemoveNoteFromAllEnumLayers(copiedNote);
            });
            moveNotesToOtherVoiceAction.MoveNotesToVoice(songMeta, copiedNotesFromVoice, voice.Name);
            pastedNotes.AddRange(copiedNotesFromVoice);
        });

        // Make editable again
        pastedNotes.ForEach(note =>
        {
            if (layerManager.TryGetEnumLayer(note, out SongEditorEnumLayer enumLayer))
            {
                note.IsEditable = layerManager.IsEnumLayerEditable(enumLayer.LayerEnum);
            }
            else
            {
                string voiceName = note?.Sentence?.Voice?.Name;
                note.IsEditable = layerManager.IsVoiceLayerEditable(voiceName);
            }
        });

        // All done, nothing to copy anymore.
        ClearCopiedNotes();

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
        ClearCopiedNotes();

        List<Note> selectedNotes = selectionControl.GetSelectedNotes();
        selectedNotes.ForEach(note =>
        {
            layerManager.TryGetEnumLayer(note, out SongEditorEnumLayer layer);

            Note copiedNote = note.Clone();
            copiedNoteToOriginalDataMap[copiedNote] = new OriginalNoteCopyData
            {
                OriginalEnumLayer = layer,
                OriginalSentence = note.Sentence,
                OriginalVoice = note.Sentence?.Voice,
            };

            copiedNote.SetSentence(null);
            copiedNote.IsEditable = false;
            CopiedNotes.Add(copiedNote);
            layerManager.AddNoteToEnumLayer(ESongEditorLayer.CopyPaste, copiedNote);
        });

        selectionControl.ClearSelection();

        editorNoteDisplayer.UpdateNotes();
    }

    private class OriginalNoteCopyData
    {
        public SongEditorEnumLayer OriginalEnumLayer { get; set; }
        public Sentence OriginalSentence { get; set; }
        public Voice OriginalVoice { get; set; }
    }
}
