using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class SongMetaUtils
{
    public static List<Sentence> GetSentencesAtBeat(SongMeta songMeta, int beat)
    {
        return songMeta.GetVoices().SelectMany(voice => voice.Sentences)
            .Where(sentence => IsBeatInSentence(sentence, beat)).ToList();
    }

    public static bool IsBeatInSentence(Sentence sentence, int beat)
    {
        return sentence.MinBeat <= beat && beat <= sentence.ExtendedMaxBeat;
    }

    public static Sentence FindExistingSentenceForNote(IReadOnlyCollection<Sentence> sentences, Note note)
    {
        return sentences.Where(sentence => (sentence.MinBeat <= note.StartBeat)
                                        && (note.EndBeat <= sentence.ExtendedMaxBeat)).FirstOrDefault();
    }

    public static Voice GetOrCreateVoice(SongMeta songMeta, string voiceName)
    {
        Voice matchingVoice = songMeta.GetVoices()
            .Where(it => it.Name == voiceName || (voiceName.IsNullOrEmpty() && it.Name == Voice.soloVoiceName))
            .FirstOrDefault();
        if (matchingVoice != null)
        {
            return matchingVoice;
        }

        // Create new voice.
        // Set voice identifier for solo voice because this is not a solo song anymore.
        Voice soloVoice = songMeta.GetVoices().Where(it => it.Name == Voice.soloVoiceName).FirstOrDefault();
        if (soloVoice != null)
        {
            soloVoice.SetName(Voice.firstVoiceName);
        }

        Voice newVoice = new Voice(voiceName);
        songMeta.AddVoice(newVoice);

        return newVoice;
    }

    public static List<Note> GetFollowingNotes(SongMeta songMeta, List<Note> notes)
    {
        if (notes.IsNullOrEmpty())
        {
            return new List<Note>();
        }

        int maxBeat = notes.Select(it => it.EndBeat).Max();
        List<Note> result = GetAllSentences(songMeta)
            .SelectMany(sentence => sentence.Notes)
            .Where(note => note.StartBeat >= maxBeat)
            .ToList();
        return result;
    }

    // Returns the notes in the song as well as the notes in the layers in no particular order.
    public static List<Note> GetAllNotes(SongMeta songMeta)
    {
        List<Note> result = GetAllSentences(songMeta).SelectMany(sentence => sentence.Notes).ToList();
        return result;
    }

    public static List<Sentence> GetAllSentences(SongMeta songMeta)
    {
        List<Sentence> result = new List<Sentence>();
        List<Sentence> sentencesInVoices = songMeta.GetVoices().SelectMany(voice => voice.Sentences).ToList();
        result.AddRange(sentencesInVoices);
        return result;
    }

    public static Sentence GetNextSentence(Sentence sentence)
    {
        if (sentence.Voice == null)
        {
            return null;
        }

        List<Sentence> sortedSentencesOfVoice = new List<Sentence>(sentence.Voice.Sentences);
        sortedSentencesOfVoice.Sort(Sentence.comparerByStartBeat);
        Sentence lastSentence = null;
        foreach (Sentence s in sortedSentencesOfVoice)
        {
            if (lastSentence == sentence)
            {
                return s;
            }
            lastSentence = s;
        }
        return null;
    }

    public static Sentence GetPreviousSentence(Sentence sentence)
    {
        if (sentence.Voice == null)
        {
            return null;
        }

        List<Sentence> sortedSentencesOfVoice = new List<Sentence>(sentence.Voice.Sentences);
        sortedSentencesOfVoice.Sort(Sentence.comparerByStartBeat);
        Sentence lastSentence = null;
        foreach (Sentence s in sortedSentencesOfVoice)
        {
            if (s == sentence)
            {
                return lastSentence;
            }
            lastSentence = s;
        }
        return null;
    }

    public static List<Note> GetSortedNotes(Sentence sentence)
    {
        List<Note> result = new List<Note>(sentence.Notes);
        result.Sort(Note.comparerByStartBeat);
        return result;
    }

    public static List<Note> GetSortedNotes(SongMeta songMeta)
    {
        List<Note> result = GetAllNotes(songMeta);
        result.Sort(Note.comparerByStartBeat);
        return result;
    }

    public static List<Sentence> GetSortedSentences(SongMeta songMeta)
    {
        List<Sentence> result = GetAllSentences(songMeta);
        result.Sort(Sentence.comparerByStartBeat);
        return result;
    }

    public static List<Sentence> GetSortedSentences(Voice voice)
    {
        List<Sentence> result = new List<Sentence>(voice.Sentences);
        result.Sort(Sentence.comparerByStartBeat);
        return result;
    }
}