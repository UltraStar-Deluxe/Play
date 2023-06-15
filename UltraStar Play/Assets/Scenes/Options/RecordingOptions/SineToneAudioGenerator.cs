using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SineToneAudioGenerator : MonoBehaviour
{
    public int Frequency { get; set; } = 440;
    
    private AudioSource audioSource;
    private int sampleRate;
    private int totalMonoSampleIndex;
    
    private void Awake()
    {
        sampleRate = AudioSettings.outputSampleRate;
        audioSource = GetComponent<AudioSource>();
    }

    public void Play()
    {
        audioSource.Play();
    }
    
    public void Pause()
    {
        audioSource.Pause();
    }
    
    public void Stop()
    {
        audioSource.Stop();
    }
    
    /**
     * Use OnAudioFilterRead for low playback latency.
     * In contrast, an AudioClip in Unity always buffers first, which causes a delay before playback starts.
     */
    private void OnAudioFilterRead(float[] data, int channelCount)
    {
        FillBuffer(data, channelCount);
    }

    private void FillBuffer(float[] data, int channelCount)
    {
        for (int sampleIndex = 0; sampleIndex < data.Length; sampleIndex += channelCount)
        {
            for (int channelIndex = 0; channelIndex < channelCount; channelIndex++)
            {
                data[sampleIndex + channelIndex] = Mathf.Sin(2 * Mathf.PI * Frequency * totalMonoSampleIndex / sampleRate);
            }
            totalMonoSampleIndex++;
        }
    }
}
