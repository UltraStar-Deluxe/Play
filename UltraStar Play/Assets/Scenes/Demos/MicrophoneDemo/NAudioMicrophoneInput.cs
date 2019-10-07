using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NAudio.Wave;

public class NAudioMicrophoneInput : MonoBehaviour
{
    public int deviceNumber = 1;
    public bool isRecording;
    public bool isPlaying;

    private WaveInEvent waveIn;
    private WaveOut waveOut;
    private BufferedWaveProvider bufferedWaveProvider;

    void Start()
    {
        // Configure input
        waveIn = new WaveInEvent();
        waveIn.DeviceNumber = deviceNumber;
        waveIn.DataAvailable += OnDataAvailable;
        // waveIn.WaveFormat = new WaveFormat(22050, 8, 1);
        Debug.Log(waveIn.WaveFormat.ToString());
        waveIn.StartRecording();

        // Configure output
        bufferedWaveProvider = new BufferedWaveProvider(waveIn.WaveFormat);
        bufferedWaveProvider.DiscardOnBufferOverflow = true;

        waveOut = new WaveOut();
        waveOut.Init(bufferedWaveProvider);
        waveOut.Play();
    }

    void Update()
    {
        // Enable / disable playback of recorded audio
        if (isPlaying && waveOut.PlaybackState != PlaybackState.Playing)
        {
            Debug.Log("Start playing");
            waveOut.Play();
        }
        else if (!isPlaying && waveOut.PlaybackState == PlaybackState.Playing)
        {
            Debug.Log("Stop playing");
            waveOut.Stop();
        }
    }

    void OnDestroy()
    {
        waveOut.Dispose();
        waveOut.Dispose();
    }

    private void OnDataAvailable(object sender, WaveInEventArgs args)
    {
        if (isRecording)
        {
            // writer.Write(args.Buffer, 0, args.BytesRecorded);
            bufferedWaveProvider.AddSamples(args.Buffer, 0, args.BytesRecorded);
        }
    }
}
