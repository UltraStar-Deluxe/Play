using System.Collections.Generic;
using UniInject;
using UnityEngine;

public class MyHighscoreProvider : IHighscoreProvider
{
    [Inject]
    private Settings settings;
    
    public int GetScore()
    {
        return 2002;
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

        int micProfileCount = settings.MicProfiles.Count;
        Debug.Log("MyHighscoreProvider - MicProfile count: " + micProfileCount);
        
        List<Note> notes = SongMetaUtils.GetAllNotes(songMeta);
        return notes.Count;
    }
}
