public static class NoteDisplayModeExtensions
{
    public static Translation GetTranslation(this ENoteDisplayMode noteDisplayMode)
    {
        switch (noteDisplayMode)
        {
            case ENoteDisplayMode.SentenceBySentence:
                return Translation.Get(R.Messages.options_noteDisplayMode_sentenceBySentence);
            case ENoteDisplayMode.ScrollingNoteStream:
                return Translation.Get(R.Messages.options_noteDisplayMode_scrollingNoteStream);
            case ENoteDisplayMode.None:
                return Translation.Get(R.Messages.options_noteDisplayMode_none);
            default:
                return Translation.Of(noteDisplayMode.ToString());
        }
    }
}
