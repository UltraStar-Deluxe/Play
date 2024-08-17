using System.Collections.Generic;

public class AudioWaveFormCalculator : IAudioWaveFormCalculator
{
    public AudioWaveForm Calculate(float[] samples, int windowSize, int fromSample = 0, int untilSample = -1)
    {
        untilSample = untilSample >= 0 ? untilSample : samples.Length - 1;
        fromSample = NumberUtils.Limit(fromSample, 0, samples.Length - 1);
        untilSample = NumberUtils.Limit(untilSample, 0, samples.Length - 1);

        List<AmplitudeRange> amplitudeRanges = new();
        for (int i = fromSample; i <= untilSample - windowSize; i += windowSize)
        {
            AudioWaveFormUtils.FindMinAndMaxValues(samples, i, windowSize, out float min, out float max);
            amplitudeRanges.Add(new AmplitudeRange(min, max));
        }

        return new AudioWaveForm(amplitudeRanges);
    }
}
