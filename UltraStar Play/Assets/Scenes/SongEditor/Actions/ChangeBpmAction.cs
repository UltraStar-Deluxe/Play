using System;
using System.Linq;
using UniInject;

#pragma warning disable CS0649

public class ChangeBpmAction : INeedInjection
{
    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    public void ReduceBpmAndNotify(SongMeta songMeta)
    {
        ReduceBpm(songMeta);
        songMetaChangeEventStream.OnNext(new BpmChangeEvent());
    }

    public void MultiplyBpmAndNotify(SongMeta songMeta, int factor)
    {
        MultiplyBpm(songMeta, factor);
        songMetaChangeEventStream.OnNext(new BpmChangeEvent());
    }

    public static void ReduceBpm(SongMeta songMeta)
    {
        int greatestCommonDivisor = songMeta.GetVoices()
            .SelectMany(voice => voice.Sentences)
            .SelectMany(sentence => sentence.Notes)
            .SelectMany(note => new int[] { note.StartBeat, note.EndBeat })
            .Aggregate(GreatestCommonDivisor);

        if (greatestCommonDivisor > 1)
        {
            DivideBpm(songMeta, greatestCommonDivisor);
        }
    }

    private static void DivideBpm(SongMeta songMeta, int factor)
    {
        if (factor <= 1)
        {
            throw new ArgumentException("Factor must be greater than 1");
        }

        foreach (Voice voice in songMeta.GetVoices())
        {
            DivideBpm(voice, factor);
        }
        float newBpm = songMeta.Bpm / factor;
        songMeta.Bpm = newBpm;
    }

    private static void DivideBpm(Voice voice, int factor)
    {
        foreach (Sentence sentence in voice.Sentences)
        {
            DivideBpm(sentence, factor);
        }
    }

    private static void DivideBpm(Sentence sentence, int factor)
    {
        int oldLinebreakBeat = sentence.LinebreakBeat;
        foreach (Note note in sentence.Notes)
        {
            DivideBpm(note, factor);
        }
        sentence.SetLinebreakBeat(oldLinebreakBeat / factor);
    }

    private static void DivideBpm(Note note, int factor)
    {
        int newStartBeat = note.StartBeat / factor;
        int newEndBeat = note.EndBeat / factor;
        note.SetStartAndEndBeat(newStartBeat, newEndBeat);
    }

    public static void MultiplyBpm(SongMeta songMeta, int factor)
    {
        if (factor <= 1)
        {
            throw new ArgumentException("Factor must be greater than 1");
        }

        foreach (Voice voice in songMeta.GetVoices())
        {
            MultiplyBpm(voice, factor);
        }
        float newBpm = songMeta.Bpm * factor;
        songMeta.Bpm = newBpm;
    }

    private static void MultiplyBpm(Voice voice, int factor)
    {
        foreach (Sentence sentence in voice.Sentences)
        {
            MultiplyBpm(sentence, factor);
        }
    }

    private static void MultiplyBpm(Sentence sentence, int factor)
    {
        int oldLinebreakBeat = sentence.LinebreakBeat;
        foreach (Note note in sentence.Notes)
        {
            MultiplyBpm(note, factor);
        }
        sentence.SetLinebreakBeat(oldLinebreakBeat * factor);
    }

    private static void MultiplyBpm(Note note, int factor)
    {
        int newStartBeat = note.StartBeat * factor;
        int newEndBeat = note.EndBeat * factor;
        note.SetStartAndEndBeat(newStartBeat, newEndBeat);
    }

    private static int GreatestCommonDivisor(int a, int b)
    {
        while (a != 0 && b != 0)
        {
            if (a > b)
            {
                a %= b;
            }
            else
            {
                b %= a;
            }
        }

        return a == 0 ? b : a;
    }
}
