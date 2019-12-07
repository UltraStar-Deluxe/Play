public interface IAudioSamplesAnalyzer
{
    void Enable();

    void Disable();

    // This method is called every frame with the new audio samples from the microphone.
    // The buffer has a size of MicrophonePitchTracker.SampleRate.
    // However, only the indexes from 0 to (samplesSinceLastFrame - 1) actually contain relevant values.
    // This portion has to be analyzed, samples are in the range from -1 to +1.
    // The rest of the buffer is undefined.
    PitchEvent ProcessAudioSamples(float[] audioSamplesBuffer, int samplesSinceLastFrame, MicProfile mic);
}