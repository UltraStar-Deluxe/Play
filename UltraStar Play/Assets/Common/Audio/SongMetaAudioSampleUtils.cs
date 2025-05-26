using UnityEngine;

public class SongMetaAudioSampleUtils
{
    public static float[] GetMonoSamples(
        SongMeta songMeta,
        AudioClip audioClip,
        int startBeat,
        int lengthInBeats)
    {
        using DisposableStopwatch ds = new("GetSamplesOfBeatRangeFromAudioClip took <ms>");

        if (lengthInBeats <= 0)
        {
            return null;
        }

        double startBeatInMillis = SongMetaBpmUtils.BeatsToMillis(songMeta, startBeat);
        double singleBeatLengthInMillis = SongMetaBpmUtils.MillisPerBeat(songMeta);
        double lengthInMillis = singleBeatLengthInMillis * lengthInBeats;

        float[] monoAudioSamples = AudioSampleUtils.GetAudioSamples(audioClip, startBeatInMillis, lengthInMillis, true);
        return monoAudioSamples;
    }
}
