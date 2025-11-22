using System;
using System.Runtime.InteropServices;
using LibVLCSharp;
using UnityEngine;

/// <summary>
/// Basic implementation for outputting VLC audio through a Unity Audio Source. 
/// With this implementation, you will gain ability to have 3D audio, AudioSource effects, and anything else that 
/// AudioSources support.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class VLCAudioSource : MonoBehaviour
{
    // User desired Sample Rate and Channels
    public int SampleRate = 48000;
    public int Channels = 2;
    
    // The audio source attached to the GameObject
    private AudioSource audioSource;
    // The audio clip that will receive the converted VLC audio
    private AudioClip audioClip;
    // The samples from VLC
    private float[] samples;
    // The buffer size for Unity audio
    private int bufferSize;
    // The unity audio buffer
    private float[] buffer;
    // The writing position for the buffer
    private int writePosition;
    // The reading position for the buffer
    private int readPosition;
    // Lock object for the buffer
    private readonly object bufferLock = new object();

    public void Attach(MediaPlayer mediaPlayer)
    {
        // Get the attached AudioSource (MUST be on this GameObject)
        audioSource = GetComponent<AudioSource>();
        // Loop the audio source
        audioSource.loop = true;
        // Tell the media player to be in Unity's audio format and convert the media to the user specified
        mediaPlayer.SetAudioFormat("FL32", (uint) SampleRate, (uint) Channels);
        // Audio Callbacks for VLC to our functions
        mediaPlayer.SetAudioCallbacks(OnAudioCallback, null, null, OnFlush, null);
        // Create the buffer array
        bufferSize = SampleRate * Channels;
        buffer = new float[bufferSize];
        // Create the audio clip and initialize the AudioSource
        audioClip = AudioClip.Create("VLCAudio", bufferSize, Channels, SampleRate, true, OnAudioRead);
        audioSource.clip = audioClip;
        audioSource.Play();
    }

    private void OnAudioCallback(IntPtr data, IntPtr samplesPtr, uint count, long pts)
    {
        // Gets the number of floats in the samples
        int floatCount = (int) count * Channels;
        // Ensures the samples buffer has enough storage to hold them
        if (samples == null || samples.Length != floatCount)
            samples = new float[floatCount];
        // Copies VLC PCM data to the samples array
        Marshal.Copy(samplesPtr, samples, 0, floatCount);
        // Lock the buffer (thread safety)
        lock (bufferLock)
        {
            // Write each sample into the Unity buffer at the current position
            for (int i = 0; i < floatCount; i++)
            {
                buffer[writePosition] = samples[i];
                writePosition = (writePosition + 1) % buffer.Length;
                // If the write position catches up to the read position, move read position forward
                if (writePosition == readPosition)
                    readPosition = (readPosition + 1) % buffer.Length;
            }
        }
    }

    private void OnAudioRead(float[] data)
    {
        // Lock the buffer (thread safety again)
        lock (bufferLock)
        {
            // For every sample Unity requests
            for (int i = 0; i < data.Length; i++)
            {
                // If there is data available
                if (readPosition != writePosition)
                {
                    // Copy the data
                    data[i] = buffer[readPosition];
                    // Advance the read position
                    readPosition = (readPosition + 1) % buffer.Length;
                }
                else
                {
                    // Otherwise, fill with silence
                    data[i] = 0f;
                }
            }
        }
    }
    
    private void OnFlush(IntPtr data, long l)
    {
        // Lock the buffer (more thread safety)
        lock (bufferLock)
        {
            // Set read and write position to 0 (clears buffer)
            readPosition = writePosition = 0;
        }
    }
}
