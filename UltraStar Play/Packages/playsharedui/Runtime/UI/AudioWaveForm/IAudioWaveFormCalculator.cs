public interface IAudioWaveFormCalculator
{
    AudioWaveForm Calculate(float[] samples, int windowSize, int fromSample = 0, int untilSample = -1);
}
