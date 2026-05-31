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
        string initialLyrics,
        SpeechRecognitionAction speechRecognitionAction,
        SongAudioPlayer songAudioPlayer,
        Settings settings)
    {
        TextInputDialogControl dialogControl = songEditorSceneControl.CreateTextInputDialog(
            Translation.Get(R.Messages.songEditor_action_forcedAlignment_dialog_title),
            Translation.Get(R.Messages.songEditor_action_forcedAlignment_dialog_message),
            newLyrics =>
            {
                // If no lyrics are given, then run speech recognition instead of forced alignment
                if (newLyrics.IsNullOrEmpty())
                {
                    speechRecognitionAction.CreateNotesFromSpeechRecognition(
                        0,
                        (int)songAudioPlayer.DurationInBeats,
                        settings.SongEditorSettings.AiSamplesSource,
                        settings.SongEditorSettings.SpaceBetweenNotesInMillis,
                        true);
                    return;
                }
                
                forcedAlignmentAction.RunForcedAlignment(newLyrics, true);
            },
            initialLyrics);
        
        // Accept empty value
        dialogControl.ValidateValueCallback = newValue => ValueInputDialogValidationResult.CreateValidResult();
    }
}
