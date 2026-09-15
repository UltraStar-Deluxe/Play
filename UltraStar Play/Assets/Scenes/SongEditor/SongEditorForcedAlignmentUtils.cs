public class SongEditorForcedAlignmentUtils
{
    public static void ShowForcedAlignmentInSelectionDialog(
        SongEditorSceneControl songEditorSceneControl,
        ForcedAlignmentAction forcedAlignmentAction,
        int minBeat,
        int lengthInBeats,
        string initialLyrics,
        SpeechRecognitionAction speechRecognitionAction,
        Settings settings)
    {
        TextInputDialogControl dialogControl = songEditorSceneControl.CreateTextInputDialog(
            Translation.Get(R.Messages.songEditor_action_forcedAlignmentInSelection_dialog_title),
            Translation.Get(R.Messages.songEditor_action_forcedAlignmentInSelection_dialog_message),
            newLyrics =>
            {
                // If no lyrics are given, then run speech recognition instead of forced alignment
                if (newLyrics.IsNullOrEmpty())
                {
                    speechRecognitionAction.CreateNotesFromSpeechRecognition(
                        minBeat,
                        lengthInBeats,
                        settings.SongEditorSettings.AiSamplesSource,
                        settings.SongEditorSettings.SpaceBetweenNotesInMillis,
                        true);
                    return;
                }
                
                forcedAlignmentAction.CreateNotesViaForcedAlignmentInSelection(
                    newLyrics,
                    minBeat,
                    lengthInBeats,
                    true);
            },
            initialLyrics);
        
        // Accept empty value
        dialogControl.ValidateValueCallback = newValue => ValueInputDialogValidationResult.CreateValidResult();
    }
}
