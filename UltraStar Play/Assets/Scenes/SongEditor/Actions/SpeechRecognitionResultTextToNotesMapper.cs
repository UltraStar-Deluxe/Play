using System.Collections.Generic;
using System.Linq;

public class SpeechRecognitionResultTextToNotesMapper
{
    public static void MapSpeechRecognitionResultTextToNotes(SongMeta songMeta, List<SpeechRecognitionWordResult> words, List<Note> notes, int wordOffsetInBeats)
    {
        List<SpeechRecognitionWordResult> unusedWords = words.ToList();
        List<Note> unsetNotes = notes.ToList();

        // First round: Best matching word is the word that has the largest temporal overlap with the note
        unsetNotes.ToList().ForEach(note =>
        {
            SpeechRecognitionWordResult bestMatchingWord = null;
            double bestMatchingWordOverlapInMillis = 0;
            foreach (SpeechRecognitionWordResult word in unusedWords)
            {
                double noteStartInMillis = SongMetaBpmUtils.BeatsToMillisWithoutGap(songMeta, note.StartBeat - wordOffsetInBeats);
                double noteEndInMillis = SongMetaBpmUtils.BeatsToMillisWithoutGap(songMeta, note.EndBeat - wordOffsetInBeats);

                double overlapInMillis = NumberUtils.GetIntersectionLength(
                    noteStartInMillis, noteEndInMillis,
                    word.Start.TotalMilliseconds, word.End.TotalMilliseconds);
                if (overlapInMillis > 0
                    && (bestMatchingWord == null
                        || bestMatchingWordOverlapInMillis < overlapInMillis))
                {
                    bestMatchingWord = word;
                    bestMatchingWordOverlapInMillis = overlapInMillis;
                }
            }

            if (bestMatchingWord != null)
            {
                note.SetText(bestMatchingWord.Text + " ");
                // Do not use this word again
                unusedWords.Remove(bestMatchingWord);
                unsetNotes.Remove(note);
            }
        });

        unsetNotes.ForEach(note => note.SetText("_"));
    }
}
