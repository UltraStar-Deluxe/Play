public class PitchDetectionConstants
{
    // Longest period of singable notes (C2) requires 674 samples at 44100 Hz sample rate.
    // Thus, 1024 samples should be sufficient.
    public const int LongestSingableNoteSampleCount = 2048;
}
