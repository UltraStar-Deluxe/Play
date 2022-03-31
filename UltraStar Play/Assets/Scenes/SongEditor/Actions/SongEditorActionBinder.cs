using System.Collections.Generic;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorActionBinder : MonoBehaviour, IBinder
{
    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new();
        bb.BindTypeToNewInstances(typeof(DeleteNotesAction));
        bb.BindTypeToNewInstances(typeof(SplitNotesAction));
        bb.BindTypeToNewInstances(typeof(MergeNotesAction));
        bb.BindTypeToNewInstances(typeof(SetNoteTypeAction));
        bb.BindTypeToNewInstances(typeof(AddNoteAction));
        bb.BindTypeToNewInstances(typeof(MoveNoteToAjacentSentenceAction));
        bb.BindTypeToNewInstances(typeof(MoveNotesToOtherVoiceAction));
        bb.BindTypeToNewInstances(typeof(MoveNoteToOwnSentenceAction));
        bb.BindTypeToNewInstances(typeof(MoveNotesAction));
        bb.BindTypeToNewInstances(typeof(ExtendNotesAction));

        bb.BindTypeToNewInstances(typeof(ToggleNoteTypeAction));

        bb.BindTypeToNewInstances(typeof(DeleteSentencesAction));
        bb.BindTypeToNewInstances(typeof(MergeSentencesAction));
        bb.BindTypeToNewInstances(typeof(SentenceFitToNoteAction));

        bb.BindTypeToNewInstances(typeof(SetMusicGapAction));
        bb.BindTypeToNewInstances(typeof(SetVideoGapAction));
        bb.BindTypeToNewInstances(typeof(ChangeBpmAction));
        bb.BindTypeToNewInstances(typeof(ApplyBpmAndAdjustNoteLengthAction));
        bb.BindTypeToNewInstances(typeof(ApplyBpmDontAdjustNoteLengthAction));
        bb.BindTypeToNewInstances(typeof(SpaceBetweenNotesAction));
        return bb.GetBindings();
    }
}
