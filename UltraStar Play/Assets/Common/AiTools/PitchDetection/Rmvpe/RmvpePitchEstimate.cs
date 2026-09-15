public class RmvpePitchEstimate
{
    /**
     * The timestamp in seconds from the start of the audio.
     */
    public double Time { get; set; }

    /**
     * The detected fundamental frequency (f0) in Hz.
     */
    public double Frequency { get; set; }

    /**
     * The confidence level of the detection, typically 1.0 for voiced and 0.0 for unvoiced.
     */
    public float Confidence { get; set; }
}
