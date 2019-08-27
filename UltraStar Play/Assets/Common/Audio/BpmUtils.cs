using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BpmUtils
{
    public static float BeatToSecondsInSong(SongMeta songMeta, double beat) {
        var secondsPerBeat = 60.0 / songMeta.Bpm;
        var secondsInSong = beat * secondsPerBeat;
        return (float)secondsInSong;
    }
}
