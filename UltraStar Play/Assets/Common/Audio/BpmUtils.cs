using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BpmUtils
{

    public static double BeatToMillisecondsInSong(SongMeta songMeta, double beat)
    {
        return BeatToMillisecondsInSongWithoutGap(songMeta, beat) + songMeta.Gap;
    }

    public static double BeatToMillisecondsInSongWithoutGap(SongMeta songMeta, double beat)
    {
        // Ultrastar BPM is not "beats per minute" but "bars per minute" in four-four-time.
        // To get the common "beats per minute", one has to multiply with 4.
        double beatsPerMinute = songMeta.Bpm * 4.0;
        double millisecondsPerBeat = 60000.0 / beatsPerMinute;
        double millisecondsInSong = beat * millisecondsPerBeat;
        return millisecondsInSong;
    }

    public static double MillisecondInSongToBeat(SongMeta songMeta, double millisInSong)
    {
        double millisInSongAfterGap = millisInSong - songMeta.Gap;
        // Ultrastar BPM is not "beats per minute" but "bars per minute" in four-four-time.
        // To get the common "beats per minute", one has to multiply with 4.
        double beatsPerMinute = songMeta.Bpm * 4.0;
        double result = beatsPerMinute * millisInSongAfterGap / 1000.0 / 60.0;
        return result;
    }

    public static double MillisecondsPerBeat(SongMeta songMeta)
    {
        double millisOfBeat0 = BeatToMillisecondsInSong(songMeta, 0);
        double millisOfBeat1 = BeatToMillisecondsInSong(songMeta, 1);
        return millisOfBeat1 - millisOfBeat0;
    }
}
