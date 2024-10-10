using System.Collections.Generic;
using System.Linq;

public class PrecalculatingAudioWaveFormCalculator : IAudioWaveFormCalculator
{
    private float[] lastSamples;
    private int lastWindowSize;
    private AmplitudeRange[] precalculatedAmplitudeRanges;

    public AudioWaveForm Calculate(float[] samples, int windowSize, int fromSample = 0, int untilSample = -1)
    {
        untilSample = untilSample >= 0 ? untilSample : samples.Length - 1;
        fromSample = NumberUtils.Limit(fromSample, 0, samples.Length - 1);
        untilSample = NumberUtils.Limit(untilSample, 0, samples.Length - 1);

        if (this.lastSamples != samples
            || this.lastWindowSize != windowSize)
        {
            Log.Verbose(() => "Precalculating amplitude ranges because samples or windowSize changed");
            this.lastSamples = samples;
            this.lastWindowSize = windowSize;
            this.precalculatedAmplitudeRanges = PrecalculateAmplitudeRanges(samples, windowSize);
        }

        int sampleLength = untilSample - fromSample;
        List<AmplitudeRange> amplitudeRanges = precalculatedAmplitudeRanges
            .Skip(fromSample / windowSize)
            .Take(sampleLength / windowSize)
            .ToList();
        return new AudioWaveForm(amplitudeRanges);
    }

    private static AmplitudeRange[] PrecalculateAmplitudeRanges(float[] samples, int windowSize)
    {
        int destinationLength = samples.Length / windowSize;
        AmplitudeRange[] amplitudeRanges = new AmplitudeRange[destinationLength];

        for (int i = 0; i < destinationLength; i++)
        {
            AudioWaveFormUtils.FindMinAndMaxValues(samples, i * windowSize, windowSize, out float min, out float max);
            amplitudeRanges[i] = new AmplitudeRange(min, max);
        }
        return amplitudeRanges;
    }
}
