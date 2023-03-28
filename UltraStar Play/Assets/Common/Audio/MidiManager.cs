using System.IO;
using AudioSynthesis;
using AudioSynthesis.Bank;
using AudioSynthesis.Bank.Patches;
using AudioSynthesis.Midi;
using AudioSynthesis.Sequencer;
using AudioSynthesis.Synthesis;
using CircularBuffer;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

// For OnAudioFilterRead to work as expected, an AudioSource is needed although it is not directly referenced.
[RequireComponent(typeof(AudioSource))]
public class MidiManager : AbstractSingletonBehaviour, INeedInjection
{
    public static MidiManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<MidiManager>();

    // MIDI sound is generated for 1 channel (mono).
    public static readonly int midiSynthesizerChannelCount = 1;

    // Factor to amplify the generated midi samples.
    public float midiGain = 1f;

    private readonly string defaultBankFilePath = "Soundfonts/MuseScore_General.sf2";
    // private readonly string defaultBankFilePath = "Soundfonts/Yamaha_YPT_220_soundfont_studio_version.sf2";
    
    private readonly int bufferSize = 1024;
    // "volume" for the midi events.
    [Range(0, 127)]
    private int midiVelocity;

    [Inject]
    private Settings settings;

    // Explanation of the different buffers:
    // The midi synthesizer generates some samples for 1 channel (mono). The newSampleBuffer is used for this small amount.
    // A larger amount is buffered in availableSamplesSynthesizerSampleRate. The values are unchanged, using the sample rate of the midi synthesizer.
    // This buffer is then resampled to the output device sample rate. These resampled values are buffered in availableSamplesOutputSampleRate.
    private float[] newSampleBuffer;
    private CircularBuffer<float> availableSynthesizerSamples;
    private CircularBuffer<float> availableOutputSamples;
    private MidiFileSequencer midiSequencer;
    private Synthesizer midiSynthesizer;
    private PatchBank bank;

    private int audioFilterReadSampleRate;
    private CircularBuffer<float> availableSingleChannelOutputSamples;

    private bool isInitialized;
    private bool loop;
    private AudioSource audioSource;

    public bool IsPlayingMidiFile { get; private set; }

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void AwakeSingleton()
    {
        audioSource = GetComponent<AudioSource>();
        audioFilterReadSampleRate = UnityEngine.AudioSettings.outputSampleRate;
        availableSingleChannelOutputSamples = new CircularBuffer<float>(audioFilterReadSampleRate);
    }

    protected override void StartSingleton()
    {
        // Synchronize with settings
        midiVelocity = settings.SongEditorSettings.MidiVelocity;
        settings.SongEditorSettings.ObserveEveryValueChanged(it => it.MidiVelocity)
            .Subscribe(newMidiVelocity => midiVelocity = newMidiVelocity)
            .AddTo(gameObject);

        midiGain = settings.SongEditorSettings.MidiGain;
        settings.SongEditorSettings.ObserveEveryValueChanged(it => it.MidiGain)
            .Subscribe(newMidiGain =>
            {
                midiGain = newMidiGain;
                if (midiSynthesizer != null)
                {
                    midiSynthesizer.MixGain = newMidiGain;
                }
            })
            .AddTo(gameObject);

        settings.ObserveEveryValueChanged(it => it.AudioSettings.soundfontPath)
            .Subscribe(newValue => OnSoundfontPathChanged())
            .AddTo(gameObject);
    }

    public void InitIfNotDoneYet()
    {
        if (isInitialized)
        {
            return;
        }
        using DisposableStopwatch d = new DisposableStopwatch("Initialize MidiManager took <ms>");

        midiSynthesizer = new Synthesizer(audioFilterReadSampleRate, midiSynthesizerChannelCount, bufferSize, 16);
        newSampleBuffer = new float[bufferSize * midiSynthesizerChannelCount];
        midiSynthesizer.MixGain = midiGain;

        if (FileUtils.Exists(settings.AudioSettings.soundfontPath))
        {
            bank = new PatchBank(new FileSystemSoundfontResource(settings.AudioSettings.soundfontPath));
        }
        else
        {
            if (!settings.AudioSettings.soundfontPath.IsNullOrEmpty())
            {
                string message = $"Soundfont file does not exist: {settings.AudioSettings.soundfontPath}";
                UiManager.CreateNotification(message);
                Debug.LogWarning(message);
            }
            bank = new PatchBank(defaultBankFilePath);
        }
        
        midiSynthesizer.UnloadBank();
        midiSynthesizer.LoadBank(bank);
        midiSequencer = new MidiFileSequencer(midiSynthesizer);

        audioSource.Play();

        isInitialized = true;
    }

    private void OnSoundfontPathChanged()
    {
        if (!isInitialized)
        {
            return;
        }
        Debug.Log("MidiManager - unloading soundfont because soundfont path changed");
        
        // Unload everything
        audioSource.Stop();
        midiSequencer.Stop();
        midiSequencer.UnloadMidi();
        midiSynthesizer.UnloadBank();
        isInitialized = false;
    }
    
    public void PlayMidiFile(MidiFile midiFile)
    {
        InitIfNotDoneYet();

        if (IsPlayingMidiFile)
        {
            StopMidiFile();
            midiSequencer.UnloadMidi();
        }

        if (midiSequencer.IsMidiLoaded)
        {
            midiSequencer.UnloadMidi();
        }
            
        
        StopAllMidiNotes();
        midiSequencer.LoadMidi(midiFile);
        midiSequencer.Play();
        IsPlayingMidiFile = true;
    }

    public void StopMidiFile()
    {
        if (!isInitialized)
        {
            return;
        }
        
        midiSequencer.Stop();
        midiSequencer.ResetMidi();
        IsPlayingMidiFile = false;
    }
    
    public void PlayMidiNote(int midiNote)
    {
        InitIfNotDoneYet();
        midiSynthesizer.NoteOn(0, midiNote, midiVelocity);
    }

    public void PlayMidiNoteForDuration(int midiNote, float durationInSeconds)
    {
        InitIfNotDoneYet();
        midiSynthesizer.NoteOn(0, midiNote, midiVelocity);
        StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(durationInSeconds, () => StopMidiNote(midiNote)));
    }

    public void StopMidiNote(int midiNote)
    {
        InitIfNotDoneYet();
        midiSynthesizer.NoteOff(0, midiNote);
    }

    public void StopAllMidiNotes(bool immediate = true)
    {
        InitIfNotDoneYet();
        midiSynthesizer.NoteOffAll(immediate);
    }

    public MidiFile LoadMidiFile(string path)
    {
        InitIfNotDoneYet();
        
        using (new DisposableStopwatch($"Loading MIDI file '{path}' took <ms>"))
        {
            byte[] midiFileBytes = File.ReadAllBytes(path);
            MidiFile midiFile = new MidiFile(midiFileBytes);
            return midiFile;
        }
    }

    // See http://unity3d.com/support/documentation/ScriptReference/MonoBehaviour.OnAudioFilterRead.html for reference code
    //	If OnAudioFilterRead is implemented, Unity will insert a custom filter into the audio DSP chain.
    //
    //	The filter is inserted in the same order as the MonoBehaviour script is shown in the inspector. 	
    //	OnAudioFilterRead is called everytime a chunk of audio is routed thru the filter (this happens frequently, every ~20ms depending on the samplerate and platform). 
    //	The audio data is an array of floats ranging from [-1.0f;1.0f] and contains audio from the previous filter in the chain or the AudioClip on the AudioSource. 
    //	If this is the first filter in the chain and a clip isn't attached to the audio source this filter will be 'played'. 
    //	That way you can use the filter as the audio clip, procedurally generating audio.
    //
    //	If OnAudioFilterRead is implemented a VU meter will show up in the inspector showing the outgoing samples level. 
    //	The process time of the filter is also measured and the spent milliseconds will show up next to the VU Meter 
    //	(it turns red if the filter is taking up too much time, so the mixer will starv audio data). 
    //	Also note, that OnAudioFilterRead is called on a different thread from the main thread (namely the audio thread) 
    //	so calling into many Unity functions from this function is not allowed ( a warning will show up ). 	
    private void OnAudioFilterRead(float[] data, int outputChannelCount)
    {
        if (!isInitialized)
        {
            return;
        }

        FillOutputBuffer(data, outputChannelCount);
    }

    private void FillOutputBuffer(float[] data, int outputChannelCount)
    {
        if (data == null)
        {
            return;
        }
            
        // Synthesize new samples from the Midi instrument until there is enough to fill the data array.
        int neededSingleChannelSamples = data.Length / outputChannelCount;
        if (neededSingleChannelSamples >= availableSingleChannelOutputSamples.Capacity)
        {
            Debug.LogWarning($"available sample capacity is too small. Samples needed: {neededSingleChannelSamples}, capacity: {availableSingleChannelOutputSamples.Capacity}");
            neededSingleChannelSamples = availableSingleChannelOutputSamples.Capacity - 1;
        }
        while (availableSingleChannelOutputSamples.Count < neededSingleChannelSamples)
        {
            midiSequencer.FillMidiEventQueue(loop);
            midiSynthesizer.GetNext();
            for (int i = 0; i < midiSynthesizer.WorkingBuffer.Length; i++)
            {
                availableSingleChannelOutputSamples.PushBack(midiSynthesizer.WorkingBuffer[i]);
            }
        }

        // The Midi stream is generated in mono (1 channel).
        // These samples are written to every channel of the output data array.
        for (int outputSampleIndex = 0; outputSampleIndex < data.Length && !availableSingleChannelOutputSamples.IsEmpty; outputSampleIndex += outputChannelCount)
        {
            float sampleValue = availableSingleChannelOutputSamples.Front();
            availableSingleChannelOutputSamples.PopFront();

            for (int outputChannelIndex = 0; outputChannelIndex < outputChannelCount; outputChannelIndex++)
            {
                data[outputSampleIndex + outputChannelIndex] = sampleValue;
            }
        }
    }
    
    private class FileSystemSoundfontResource : IResource
    {
        private readonly string path;
    
        public FileSystemSoundfontResource(string path)
        {
            this.path = path;
        }

        public bool ReadAllowed()
        {
            return true;
        }

        public bool WriteAllowed()
        {
            return false;
        }

        public bool DeleteAllowed()
        {
            return false;
        }

        public string GetName()
        {
            return Path.GetFileName(path);
        }

        public Stream OpenResourceForRead()
        {
            return File.OpenRead(path);
        }

        public Stream OpenResourceForWrite()
        {
            throw new System.NotImplementedException();
        }

        public void DeleteResource()
        {
            throw new System.NotImplementedException();
        }
    }
}
