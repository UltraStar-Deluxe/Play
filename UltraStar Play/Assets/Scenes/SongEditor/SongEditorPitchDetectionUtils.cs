using System.Linq;

public static class SongEditorPitchDetectionUtils
{
    public static async void AnalyzePitchUsingAi(
        SongMeta songMeta,
        PitchDetectionAction pitchDetectionAction,
        NoteAreaControl noteAreaControl)
    {
        if (!FileUtils.Exists(SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.VocalsAudio)))
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.songEditor_error_missingVocalsAudio));
            return;
        }

        PitchDetectionResult pitchDetectionResult = await pitchDetectionAction.AnalyzePitchUsingAi(true);
        ScrollPitchDetectionResultIntoView(noteAreaControl, pitchDetectionResult);
    }
    
    private static void ScrollPitchDetectionResultIntoView(NoteAreaControl noteAreaControl,
        PitchDetectionResult pitchDetectionResult)
    {
        double fromMillis = pitchDetectionResult.Notes.Min(note => note.StartInMillis);
        double toMillis = pitchDetectionResult.Notes.Max(note => note.StartInMillis + note.LengthInMillis);
        double midiNote = pitchDetectionResult.Notes.Min(note => note.MidiNote);
        noteAreaControl.ScrollIntoView(fromMillis, toMillis, midiNote, midiNote);
    }
}
