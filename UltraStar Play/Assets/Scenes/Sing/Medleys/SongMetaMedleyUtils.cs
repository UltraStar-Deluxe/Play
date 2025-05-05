using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class SongMetaMedleyUtils
{
    public static int GetMedleyStartBeat(SongMeta songMeta)
    {
        return HasSpecifiedMedleyStartBeat(songMeta)
            ? GetSpecifiedMedleyStartBeat(songMeta)
            : GetDefaultMedleyStartBeat(songMeta);
    }

    public static int GetMedleyEndBeat(SongMeta songMeta, int targetDurationInSeconds)
    {
        return HasSpecifiedMedleyEndBeat(songMeta)
            ? GetSpecifiedMedleyEndBeat(songMeta)
            : GetDefaultMedleyEndBeat(songMeta, targetDurationInSeconds);
    }

    private static int GetSpecifiedMedleyStartBeat(SongMeta songMeta)
    {
        return (int)SongMetaBpmUtils.MillisToBeats(songMeta, songMeta.MedleyStartInMillis);
    }

    private static bool HasSpecifiedMedleyStartBeat(SongMeta songMeta)
    {
        return NumberUtils.IsDistanceGreaterThan(songMeta.MedleyStartInMillis, 0, 1);
    }

    private static int GetSpecifiedMedleyEndBeat(SongMeta songMeta)
    {
        return (int)SongMetaBpmUtils.MillisToBeats(songMeta, songMeta.MedleyEndInMillis);
    }

    private static bool HasSpecifiedMedleyEndBeat(SongMeta songMeta)
    {
        return NumberUtils.IsDistanceGreaterThan(songMeta.MedleyEndInMillis, 0, 1);
    }

    private static int GetDefaultMedleyStartBeat(SongMeta songMeta)
    {
        // Search for lyrics about the middle of the song, approx. 20 seconds afterwards.
        int middleBeat = GetMiddleBeat(songMeta);
        Voice voice = SongMetaUtils.GetVoiceById(songMeta, EVoiceId.P1);
        List<Sentence> sentences = voice.Sentences.ToList();
        List<Sentence> sentencesBeforeMiddleBeat = sentences
            .Where(sentence => sentence.ExtendedMaxBeat < middleBeat)
            .ToList();
        if (sentencesBeforeMiddleBeat.IsNullOrEmpty())
        {
            // Should not happen, this is a weird song.
            Debug.LogWarning("Could not calculate a nice medley start beat. Using the middle of the song instead.");
            return middleBeat;
        }

        sentencesBeforeMiddleBeat.Sort(Sentence.comparerByStartBeat);
        return sentencesBeforeMiddleBeat.LastOrDefault().MinBeat;
    }

    private static int GetDefaultMedleyEndBeat(SongMeta songMeta, int targetDurationInSeconds)
    {
        // End the medley several seconds after the medley start.
        int medleyStartBeta = GetMedleyStartBeat(songMeta);
        int targetDurationInBeats = (int)SongMetaBpmUtils.MillisToBeatsWithoutGap(songMeta, targetDurationInSeconds * 1000);
        int targetEndBeat = medleyStartBeta + targetDurationInBeats;

        List<Sentence> sentencesAfterMedleyStart = SongMetaUtils.GetVoiceById(songMeta, EVoiceId.P1)
            .Sentences
            .Where(sentence => sentence.MinBeat > medleyStartBeta)
            .ToList();

        if (sentencesAfterMedleyStart.IsNullOrEmpty())
        {
            // Should not happen, this is a weird song.
            Debug.LogWarning("Could not calculate a nice medley end beat. Using some beats after medley start instead.");
            return medleyStartBeta + targetDurationInBeats;
        }

        Sentence sentence = sentencesAfterMedleyStart.FindMinElement(sentence =>
        {
            // Use sentence which best approximates the target distance.
            float distanceToTargetBeat = Math.Abs(sentence.ExtendedMaxBeat - targetEndBeat);
            return distanceToTargetBeat;
        });
        if (sentence == null)
        {
            return medleyStartBeta + 1;
        }

        return sentence.ExtendedMaxBeat;
    }

    private static int GetMiddleBeat(SongMeta songMeta)
    {
        // Search for lyrics about the middle of the song, approx. 20 seconds afterwards.
        List<Note> allNotes = SongMetaUtils.GetAllNotes(songMeta);
        int minBeat = SongMetaUtils.GetMinBeat(allNotes);
        int maxBeat = SongMetaUtils.GetMaxBeat(allNotes);
        return minBeat + ((maxBeat - minBeat) / 2);
    }
}
