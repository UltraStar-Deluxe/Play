using System;
using ProTrans;

public static class NoteDisplayModeExtensions
{
    public static string GetTranslation(this ENoteDisplayMode noteDisplayMode)
    {
        switch (noteDisplayMode)
        {
            case ENoteDisplayMode.SentenceBySentence:
                return TranslationManager.GetTranslation(R.Messages.options_noteDisplayMode_sentenceBySentence);
            case ENoteDisplayMode.ScrollingNoteStream:
                return TranslationManager.GetTranslation(R.Messages.options_noteDisplayMode_scrollingNoteStream);
            case ENoteDisplayMode.None:
                return TranslationManager.GetTranslation(R.Messages.options_noteDisplayMode_none);
            default:
                return noteDisplayMode.ToString();
        }
    }
}
