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

    [Inject]
    private SongEditorLayerManager songEditorLayerManager;

    public bool HasCopy => copyData != null && !copyData.SentenceCopies.IsNullOrEmpty();

    private CopyData copyData;

    private void Start()
    {
        // Copy action
        InputManager.GetInputAction(R.InputActions.songEditor_copy).PerformedAsObservable()
            .Where(_ => !songEditorSceneInputControl.AnyInputFieldHasFocus())
            .Subscribe(_ => CopySelection());

        // Cut action
        InputManager.GetInputAction(R.InputActions.songEditor_cut).PerformedAsObservable()
            .Where(_ => !songEditorSceneInputControl.AnyInputFieldHasFocus())
            .Subscribe(_ => CutSelection());

        // Paste action
        InputManager.GetInputAction(R.InputActions.songEditor_paste).PerformedAsObservable()
            .Where(_ => !songEditorSceneInputControl.AnyInputFieldHasFocus())
            .Subscribe(_ => Paste());
    }

    public void Paste()
    {
        if (!HasCopy)
        {
            return;
        }

        Dictionary<NoteCopy, Note> pastedNotes = new();

        if (copyData.SentenceCopies.IsNullOrEmpty())
        {
            return;
        }

        // Shift to playback position
        int currentBeat = (int)songAudioPlayer.GetCurrentBeat(true);
        int minBeat = copyData.SentenceCopies.SelectMany(sentenceCopy => sentenceCopy.NoteCopies).Select(noteCopy => noteCopy.StartBeat).Min();
        int distanceInBeats = currentBeat - minBeat;

        foreach (SentenceCopy sentenceCopy in copyData.SentenceCopies)
        {
            if (sentenceCopy.Layer is SongEditorEnumLayer enumLayer)
            {
                pastedNotes.AddRange(PasteToEnumLayer(sentenceCopy, enumLayer, distanceInBeats));
            }
            else if (sentenceCopy.Layer is SongEditorVoiceLayer voiceLayer)
            {
                pastedNotes.AddRange(PasteToVoiceLayer(sentenceCopy, voiceLayer, distanceInBeats));
            }
        }

        // Prevent words from merging by adding trailing space to notes that have been the end of phrase
        // but that have now a following note
        foreach ((NoteCopy noteCopy, Note pastedNote) in pastedNotes)
        {
            if (!pastedNote.Text.EndsWith(" ")
                && !IsLastNoteInSentence(pastedNote)
                && noteCopy.WasLastNoteInSentence)
            {
                pastedNote.SetText(pastedNote.Text + " ");
            }
        }

        // Select notes
        selectionControl.SetSelection(pastedNotes.Values.ToList());
        songMetaChangeEventStream.OnNext(new NotesPastedEvent());
    }

    private bool IsLastNoteInSentence(Note note)
    {
        return note.Sentence != null
               && note.Sentence.Notes.IndexOf(note) == note.Sentence.Notes.Count - 1;
    }

    private Dictionary<NoteCopy, Note> PasteToVoiceLayer(SentenceCopy sentenceCopy, SongEditorVoiceLayer voiceLayer, int distanceInBeats)
    {
        EVoiceId voiceId = voiceLayer.VoiceId;
        if (!songMeta.TryGetVoice(voiceId, out Voice voice))
        {
            throw new IllegalStateException("Failed to find voice for copied sentence");
        }

        Sentence createdSentence = null;

        Dictionary<NoteCopy, Note> pastedNotes = new();
        foreach (NoteCopy noteCopy in sentenceCopy.NoteCopies)
        {
            Note pastedNote = CreateNote(noteCopy);
            pastedNote.IsEditable = true;

            Sentence existingSentence = SongMetaUtils.GetSentenceAtBeat(voice, noteCopy.StartBeat + distanceInBeats);
            if (existingSentence == null
                && createdSentence == null)
            {
                createdSentence = new Sentence(new List<Note>() { pastedNote });
                createdSentence.SetVoice(voice);
            }
            pastedNote.SetSentence(existingSentence ?? createdSentence);

            pastedNote.SetStartAndEndBeat(
                pastedNote.StartBeat + distanceInBeats,
                pastedNote.EndBeat + distanceInBeats);

            pastedNote.IsEditable = layerManager.IsVoiceLayerEditable(voiceId);
            pastedNotes.Add(noteCopy, pastedNote);
        }

        return pastedNotes;
    }

    private Dictionary<NoteCopy, Note> PasteToEnumLayer(SentenceCopy sentenceCopy, SongEditorEnumLayer enumLayer, int distanceInBeats)
    {
        Dictionary<NoteCopy, Note> pastedNotes = new();
        foreach (NoteCopy noteCopy in sentenceCopy.NoteCopies)
        {
            Note pastedNote = CreateNote(noteCopy);
            pastedNote.IsEditable = true;
            pastedNote.SetStartAndEndBeat(
                pastedNote.StartBeat + distanceInBeats,
                pastedNote.EndBeat + distanceInBeats);

            pastedNote.IsEditable = layerManager.IsEnumLayerEditable(enumLayer.LayerEnum);
            layerManager.AddNoteToEnumLayer(enumLayer.LayerEnum, pastedNote);

            pastedNotes.Add(noteCopy, pastedNote);
        }
        return pastedNotes;
    }

    public void CutSelection()
    {
        List<Note> selectedNotes = selectionControl.GetSelectedNotes();
        if (selectedNotes.IsNullOrEmpty())
        {
            return;
        }
        CopySelection();
        deleteNotesAction.Execute(selectedNotes);
        songMetaChangeEventStream.OnNext(new NotesCutEvent());
    }

    public void CopySelection()
    {
        Copy(selectionControl.GetSelectedNotes());
    }

    private void Copy(List<Note> notes)
    {
        copyData = new CopyData();
        foreach (Note originalNote in notes)
        {
            Sentence originalSentence = originalNote.Sentence;
            bool wasLastNoteInSentence = originalSentence?.Notes.LastOrDefault() == originalNote;

            AbstractSongEditorLayer layer = layerManager.GetLayer(originalNote);
            SentenceCopy sentenceCopy = copyData.GetOrCreate(layer, originalSentence);
            sentenceCopy.Add(CreateNoteCopy(originalNote, wasLastNoteInSentence));
        }

        selectionControl.ClearSelection();
        editorNoteDisplayer.UpdateNotes();
    }

    private NoteCopy CreateNoteCopy(Note note, bool wasLastNoteInSentence)
    {
        return new NoteCopy(note.StartBeat, note.EndBeat, note.MidiNote, note.Text, note.Type, wasLastNoteInSentence);
    }

    private Note CreateNote(NoteCopy noteCopy)
    {
        return new Note(noteCopy.Type, noteCopy.StartBeat, noteCopy.Length, noteCopy.TxtPitch, noteCopy.Text);
    }

    private class CopyData
    {
        public List<SentenceCopy> SentenceCopies { get; private set; } = new();

        public SentenceCopy GetOrCreate(AbstractSongEditorLayer layer, Sentence originalSentence)
        {
            SentenceCopy sentenceCopy = SentenceCopies.FirstOrDefault(it
                => it.Layer == layer && it.OriginalSentence == originalSentence);
            if (sentenceCopy == null)
            {
                sentenceCopy = new SentenceCopy(layer, originalSentence);
                SentenceCopies.Add(sentenceCopy);
            }

            return sentenceCopy;
        }
    }

    private class SentenceCopy
    {
        public Sentence OriginalSentence { get; private set; }
        public AbstractSongEditorLayer Layer { get; private set; }
        public List<NoteCopy> NoteCopies { get; private set; } = new();

        public SentenceCopy(AbstractSongEditorLayer layer, Sentence originalSentence)
        {
            this.Layer = layer;
            this.OriginalSentence = originalSentence;
        }

        public void Add(NoteCopy noteCopy)
        {
            NoteCopies.Add(noteCopy);
        }

        // public ESongEditorLayer LayerEnum => layer is SongEditorEnumLayer songEditorEnumLayer
        //     ? songEditorEnumLayer.LayerEnum
        //     : throw new IllegalStateException("Copied sentence has no layer enum");
    }

    private class NoteCopy
    {
        public int StartBeat { get; private set; }
        public int EndBeat { get; private set; }
        public int Length => EndBeat - StartBeat;
        public int MidiNote { get; private set; }
        public int TxtPitch => MidiUtils.GetUltraStarTxtPitch(MidiNote);
        public string Text { get; private set; }
        public ENoteType Type { get; private set; }
        public bool WasLastNoteInSentence { get; private set; }

        public NoteCopy(
            int startBeat,
            int endBeat,
            int midiNote,
            string text,
            ENoteType type,
            bool wasLastNoteInSentence)
        {
            StartBeat = startBeat;
            EndBeat = endBeat;
            MidiNote = midiNote;
            Text = text;
            Type = type;
            WasLastNoteInSentence = wasLastNoteInSentence;
        }
    }
}
