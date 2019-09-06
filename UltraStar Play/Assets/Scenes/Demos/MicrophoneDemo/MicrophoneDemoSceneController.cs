using System;
using UnityEngine;
using UnityEngine.UI;
using Pitch;

[RequireComponent(typeof(AudioSource))]
public class MicrophoneDemoSceneController : MonoBehaviour
{
    private const int SampleRate = 22050;
    private const int BufferSteps = 10;
    private const int BufferSize = SampleRate / BufferSteps;

    public string micDeviceName = "Mikrofon (2- USB Microphone)";
    public Text currentNoteLabel;
    public bool playRecordedAudio;

    private AudioSource micAudioSource;
    private float[] audioClipData = new float[BufferSize];

    private PitchTracker pitchTracker;

    void Start()
    {
        pitchTracker = new PitchTracker();
        pitchTracker.PitchRecordHistorySize = 5;
        pitchTracker.SampleRate = SampleRate;

        micAudioSource = GetComponent<AudioSource>();

        if (Microphone.devices.Length == 0)
        {
            throw new Exception("No mic found");
        }
        foreach (var device in Microphone.devices)
        {
            Debug.Log("Found mic: " + device);
        }

        if (Array.Exists(Microphone.devices, (device) => device.Equals(micDeviceName)))
        {
            Debug.Log($"Start recording with '{micDeviceName}'");
            // Code for low-latency microphone input taken from
            // https://support.unity3d.com/hc/en-us/articles/206485253-How-do-I-get-Unity-to-playback-a-Microphone-input-in-real-time-
            // It seems that there is still a latency of more than 200ms, which is too much for real-time processing.
            AudioSource audio = GetComponent<AudioSource>();
            micAudioSource.clip = Microphone.Start(null, true, 1, SampleRate);
            micAudioSource.loop = true;
            if (playRecordedAudio)
            {
                while (!(Microphone.GetPosition(null) > 0)) { /* Busy waiting */ }
                Debug.Log("Start playing... position is " + Microphone.GetPosition(null));
                micAudioSource.Play();
            }
            pitchTracker.PitchDetected += new PitchTracker.PitchDetectedHandler(OnPitchDetected);
        }
        else
        {
            gameObject.SetActive(false);
            throw new Exception($"Microphone '{micDeviceName}' not found. Please, provide the name of a found mic.");
        }
    }

    void Update()
    {
        if (playRecordedAudio && !micAudioSource.isPlaying)
        {
            micAudioSource.Play();
        }
        else if (!playRecordedAudio && micAudioSource.isPlaying)
        {
            micAudioSource.Stop();
        }

        // Fill buffer with raw sample data from microphone
        micAudioSource.clip.GetData(audioClipData, BufferSize);

        // Detect the pitch of the sample
        pitchTracker.ProcessBuffer(audioClipData, 0);
    }

    private void OnPitchDetected(PitchTracker sender, PitchTracker.PitchRecord pitchRecord)
    {
        // Show the note that has been detected
        var midiNote = pitchRecord.MidiNote;
        if (midiNote > 0)
        {
            currentNoteLabel.text = "Note: " + MidiUtils.MidiNoteToAbsoluteName(midiNote);
        }
        else
        {
            currentNoteLabel.text = "Note: unkown";
        }
    }

    void OnDestroy()
    {
        Microphone.End(micDeviceName);
    }
}
