using System.IO;
using AudioSynthesis.Bank;
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

    [InjectedInInspector]
    public TextAsset defaultSoundfontAsset;
    
    private readonly int bufferSize = 1024;

    // "volume" for the midi events.
    [Range(0, 127)]
    private int midiVelocity;

    [Inject]
    private Settings settings;

    [Inject]
    private SceneNavigator sceneNavigator;

    private MidiFileSequencer midiSequencer;
    private Synthesizer midiSynthesizer;
    private PatchBank bank;

    private int audioFilterReadSampleRate;
    private CircularBuffer<float> availableSingleChannelOutputSamples;

    private bool isInitialized;
    private bool loop;
    private AudioSource audioSource;

    public bool IsPlayingMidiFile { get; private set; }
    private bool isPlayingMidiNote;
    private long stopMidiNoteTimeMillis;

    private AudioClip midiAudioClip;
    private bool ignoreInitialOnAudioClipSetPositionCallback;

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

        sceneNavigator.BeforeSceneChangeEventStream.Subscribe(_ => DestroyAudioClip());
    }

    public void InitIfNotDoneYet()
    {
        if (isInitialized)
        {
            return;
        }
        using DisposableStopwatch d = new DisposableStopwatch("Initialize MidiManager took <ms>");

        midiSynthesizer = new Synthesizer(audioFilterReadSampleRate, midiSynthesizerChannelCount, bufferSize, 16);
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
            
            bank = new PatchBank(new TextAssetSoundfontResource(defaultSoundfontAsset));
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

        UnloadMidiFile();
        
        midiSequencer.LoadMidi(midiFile);
        midiSequencer.Play();
        IsPlayingMidiFile = true;
    }

    private void UnloadMidiFile()
    {
        if (midiSequencer.IsMidiLoaded)
        {
            midiSequencer.Stop();
            midiSequencer.ResetMidi();
            midiSequencer.UnloadMidi();
        }
        IsPlayingMidiFile = false;
        
        StopAllMidiNotes();
        
        availableSingleChannelOutputSamples?.Clear();
        
        DestroyAudioClip();
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
        isPlayingMidiNote = true;
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
        if (!isPlayingMidiNote)
        {
            return;
        }
        
        InitIfNotDoneYet();
        midiSynthesizer.NoteOff(0, midiNote);
        isPlayingMidiNote = false;
        stopMidiNoteTimeMillis = TimeUtils.GetUnixTimeMilliseconds();
    }

    public void StopAllMidiNotes(bool immediate = true)
    {
        InitIfNotDoneYet();
        midiSynthesizer.NoteOffAll(immediate);
        isPlayingMidiNote = false;
        stopMidiNoteTimeMillis = TimeUtils.GetUnixTimeMilliseconds();
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
        if (!isInitialized
            || midiAudioClip != null)
        {
            return;
        }

        if (IsPlayingMidiFile
            || isPlayingMidiNote
            || TimeUtils.GetUnixTimeMilliseconds() - stopMidiNoteTimeMillis < 2000)
        {
            FillOutputBuffer(data, outputChannelCount);
        }
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
    
    public AudioClip CreateAudioClip(string midiFilePath)
    {
        if (!FileUtils.Exists(midiFilePath))
        {
            Debug.LogError($"MIDI file does not exist: {midiFilePath}");
            return null;
        }

        // This MIDI file will synthesize samples for the AudioClip.
        InitIfNotDoneYet();
        UnloadMidiFile();
        MidiFile midiFile = MidiFileUtils.LoadMidiFile(midiFilePath);
        midiSequencer.LoadMidi(midiFile);
        midiSequencer.Play();

        using (new DisposableStopwatch($"Creating AudioClip from MIDI file '{midiFilePath}' took <ms>"))
        {
            int audioClipSampleRate = audioFilterReadSampleRate;
            double midiFileLengthInMillis = MidiFileUtils.GetMidiFileLengthInMillis(midiFile);
            int audioClipLengthInSamples = (int)((midiFileLengthInMillis / 1000.0) * audioClipSampleRate);

            // Ignore the initial SetPosition callback.
            ignoreInitialOnAudioClipSetPositionCallback = true;
            
            midiAudioClip = AudioClip.Create($"MIDI file '{Path.GetFileName(midiFilePath)}'",
                audioClipLengthInSamples,
                midiSynthesizerChannelCount,
                audioClipSampleRate,
                true,
                OnAudioClipRead,
                OnAudioClipSetPosition);

            return midiAudioClip;
        }
    }

    public void DestroyAudioClip()
    {
        if (midiAudioClip != null)
        {
            Debug.Log("DestroyAudioClip");
            Destroy(midiAudioClip);
            midiAudioClip = null;
        }
    }
    
    private void OnAudioClipRead(float[] data)
    {
        FillOutputBuffer(data, midiSynthesizerChannelCount);
    }

    private void OnAudioClipSetPosition(int positionInSamples)
    {
        if (ignoreInitialOnAudioClipSetPositionCallback
            && positionInSamples == 0)
        {
            ignoreInitialOnAudioClipSetPositionCallback = false;
            return;
        }
        
        midiSequencer.SeekSampleTime(positionInSamples);
    }
}
