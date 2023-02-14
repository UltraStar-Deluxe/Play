using System.Collections.Generic;

public class MyHighscoreProvider : IHighscoreProvider
{
    public int GetScore()
    {
        return 2000;
    }

    public int GetNoteCount(SongMeta songMeta)
    {
        // int count = 0;
        // var voices = songMeta.GetVoices();
        // foreach(var voice in voices)
        // {
        //     foreach(var sentence in voice.Sentences)
        //     {
        //         count += sentence.Notes.Count;
        //     }
        // }
        // return count;

        List<Note> notes = SongMetaUtils.GetAllNotes(songMeta);
        return notes.Count;
    }
}
