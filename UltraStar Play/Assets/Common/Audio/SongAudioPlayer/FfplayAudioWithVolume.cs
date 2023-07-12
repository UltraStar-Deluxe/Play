using FfmpegUnity;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FfplayAudioWithVolume : FfplayAudio
{
    private AudioSource audioSource;

    private float volume = 1f;
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        volume = audioSource.volume * AudioListener.volume;
    }

    protected override void OnAudioFilterRead(float[] data, int channels)
    {
        base.OnAudioFilterRead(data, channels);
        
        // Apply volume
        if (volume >= 1)
        {
            return;
        }

        if (volume <= 0)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = 0;
            }
        }
        else
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] *= volume;
            }
        }
    }
}
