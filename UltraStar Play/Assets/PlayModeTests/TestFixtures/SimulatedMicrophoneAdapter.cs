using System;
using System.Collections.Generic;
using UnityEngine;

public class SimulatedMicrophoneAdapter : IMicrophoneAdapter
{
    private const int SimulatedDeviceSampleRate = 44100;
    private const double DefaultSimulatedPitchInHz = 440; // 440 Hz <=> A4 concert pitch

    private static List<string> simulatedDevices = new List<string>
    {
        GetSimulatedMicName(0)
    };

    private static readonly HashSet<string> deviceNameToIsRecording = new HashSet<string>();
    private static readonly HashSet<string> deviceNameToIsSilent = new HashSet<string>();
    private static readonly Dictionary<string, double> deviceNameToSimulatedPitchInHz = new Dictionary<string, double>();

    public static string GetSimulatedMicName(int index)
    {
        return $"Simulated Mic {index}";
    }

    public static void SetSimulatedDevices(List<string> newSimulatedDevices)
    {
        simulatedDevices = newSimulatedDevices;
    }

    public static void SimulateRecording(string deviceName, bool isRecording)
    {
        if (isRecording)
        {
            Debug.Log($"Simulate start recording with '{deviceName}'");
            deviceNameToIsRecording.Add(deviceName);
        }
        else
        {
            Debug.Log($"Simulate stop recording with '{deviceName}'");
            deviceNameToIsRecording.Remove(deviceName);
        }
    }

    public static void SimulateSilence(string deviceName, bool isSilent)
    {
        if (isSilent)
        {
            Debug.Log($"Simulate silent input with '{deviceName}'");
            deviceNameToIsSilent.Add(deviceName);
        }
        else
        {
            Debug.Log($"Simulate pitch input with '{deviceName}'");
            deviceNameToIsSilent.Remove(deviceName);
        }
    }

    public static void SetSimulatedDevicePitchInHz(string deviceName, double pitchInHz)
    {
        if (pitchInHz < 8.18 || pitchInHz > 12543.85)
        {
            throw new ArgumentException("Pitch frequency outside MIDI note range");
        }
        deviceNameToSimulatedPitchInHz[deviceName] = pitchInHz;
    }

    private static double GetSimulatedDevicePitchInHzOrDefault(string deviceName)
    {
        if (deviceNameToSimulatedPitchInHz.TryGetValue(deviceName, out double pitchInHz))
        {
            return pitchInHz;
        }
        else
        {
            return DefaultSimulatedPitchInHz;
        }
    }

    public bool UsePortAudio
    {
        // Only simulating PortAudioForUnity microphone but not AudioClip and other Unity API.
        get => true;
        set { }
    }

    public string[] Devices => simulatedDevices.ToArray();

    public void GetDeviceCaps(
        string deviceName,
        out int minFreq,
        out int maxFreq,
        out int channelCount)
    {
        minFreq = SimulatedDeviceSampleRate;
        maxFreq = SimulatedDeviceSampleRate;
        channelCount = 1;
    }

    public bool IsRecording(string deviceName) => deviceNameToIsRecording.Contains(deviceName);
    public bool IsSilent(string deviceName) => deviceNameToIsSilent.Contains(deviceName);

    public AudioClip Start(
        string inputDeviceName,
        bool loop,
        int bufferLengthSec,
        int sampleRate,
        string outputDeviceName = "",
        float directOutputAmplificationFactor = 1)
    {
        SimulateRecording(inputDeviceName, true);
        return null;
    }

    public void End(string deviceName)
    {
        SimulateRecording(deviceName, false);
    }

    public int GetPosition(string deviceName)
    {
        // Assuming a buffer length of one second.
        double zeroToOneCounterInSeconds = (TimeUtils.GetUnixTimeMilliseconds() % 1000) / 1000.0;
        return (int)(SimulatedDeviceSampleRate * zeroToOneCounterInSeconds);
    }

    public void GetRecordedSamples(
        string deviceName,
        int channelIndex,
        AudioClip microphoneAudioClip,
        int recordingPosition,
        float[] bufferToBeFilled)
    {
        if (!IsRecording(deviceName)
            || IsSilent(deviceName))
        {
            return;
        }

        double simulatedPitchInHz = GetSimulatedDevicePitchInHzOrDefault(deviceName);
        for (int i = 0; i < bufferToBeFilled.Length; i++)
        {
            float time = i / (float)SimulatedDeviceSampleRate;
            bufferToBeFilled[i] = Mathf.Sin((float)(2f * Mathf.PI * simulatedPitchInHz * time));
        }
    }

}
