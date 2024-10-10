public static class SongEditorSettingsUtils
{
    public static bool ShouldAdjustFollowingNotes(Settings settings, bool adjustFollowingNotesIfNeeded = true)
    {
        return adjustFollowingNotesIfNeeded
               && settings.SongEditorSettings.AdjustFollowingNotes
               && !InputUtils.IsKeyboardShiftPressed();
    }
}
