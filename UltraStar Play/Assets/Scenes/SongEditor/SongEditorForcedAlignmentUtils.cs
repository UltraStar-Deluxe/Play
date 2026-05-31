public class SongEditorForcedAlignmentUtils
{
    public static void ShowForcedAlignmentInSelectionDialog(
        SongEditorSceneControl songEditorSceneControl,
        ForcedAlignmentAction forcedAlignmentAction,
        int minBeat,
        int lengthInBeats,
        string initialLyrics)
    {
        songEditorSceneControl.CreateTextInputDialog(
            Translation.Get(R.Messages.songEditor_action_forcedAlignmentInSelection_dialog_title),
            Translation.Get(R.Messages.songEditor_action_forcedAlignmentInSelection_dialog_message),
            newLyrics =>
            {
                forcedAlignmentAction.CreateNotesViaForcedAlignmentInSelection(
                    newLyrics,
                    minBeat,
                    lengthInBeats,
                    true);
            },
            initialLyrics);
    }
    
    public static void ShowForcedAlignmentDialog(
        SongEditorSceneControl songEditorSceneControl,
        ForcedAlignmentAction forcedAlignmentAction,
        string initialLyrics)
    {
        songEditorSceneControl.CreateTextInputDialog(
            Translation.Get(R.Messages.songEditor_action_forcedAlignment_dialog_title),
            Translation.Get(R.Messages.songEditor_action_forcedAlignment_dialog_message),
            newLyrics =>
            {
                forcedAlignmentAction.RunForcedAlignment(newLyrics, true);
            },
            initialLyrics);
    }
}
