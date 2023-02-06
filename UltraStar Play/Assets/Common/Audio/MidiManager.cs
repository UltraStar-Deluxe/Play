using CircularBuffer;
using CSharpSynth.Midi;
using CSharpSynth.Sequencer;
using CSharpSynth.Synthesis;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

// An AudioSource is needed although it is not directly referenced.
// Furthermore, the AudioSource must have "Play on Awake" set to true.
[RequireComponent(typeof(AudioSource))]
public class MidiManager : AbstractSingletonBehaviour, INeedInjection
{
    public static MidiManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<MidiManager>();

    // The sound bank (or CSharpSynth) can only handle a specific sample rate of 44100 Hz. With 44800 Hz, the sound is distorted.
    public static readonly int midiStreamSampleRateHz = 44100;
    // MIDI sound is generated for 1 channel (mono).
    public static readonly int midiStreamChannelCount = 1;

    [Range(0, 2)] // Piano 0, 1 or 2
    public int midiInstrument;

    // Factor to amplify the generated midi samples.
    public float midiGain = 1f;

    // The txt file describing the instruments of the sound bank. Must be in a Resources folder.
    private readonly string bankFilePath = "GM Bank - Piano/GM Bank - Piano";
    private readonly int bufferSize = 1024;
    // "volume" for the midi events.
    [Range(0, 127)]
    private int midiVelocity;
    // Output sample rate of the device. The value is fetched from Unity.
    private int outputSampleRateHz;

    [Inject]
    private Settings settings;

    // Explanation of the different buffers:
    // The midi synthesizer generates some samples for 1 channel (mono). The newSampleBuffer is used for this small amount.
    // A larger amount is buffered in availableSamplesSynthesizerSampleRate. The values are unchanged, using the sample rate of the midi synthesizer.
    // This buffer is then resampled to the output device sample rate. These resampled values are buffered in availableSamplesOutputSampleRate.
    private float[] newSampleBuffer;
    private CircularBuffer<float> availableSynthesizerSamples;
    private CircularBuffer<float> availableOutputSamples;
    private MidiSequencer midiSequencer;
    private StreamSynthesizer midiStreamSynthesizer;

    // Fields to counter-check the output device sample rate that Unity uses (48000 Hz).
    [ReadOnly]
    public int audioFilterReadSampleRateHz;
    private int audioFilterReadSampleCounter;
    private float audioFilterReadSecondsCounter;

    private bool isInitialized;
    
    public bool IsPlayingMidiFile { get; private set; }

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void AwakeSingleton()
    {
        outputSampleRateHz = UnityEngine.AudioSettings.outputSampleRate;
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
            .Subscribe(newMidiGain => midiGain = newMidiGain)
            .AddTo(gameObject);
    }

    private void Update()
    {
        audioFilterReadSecondsCounter += Time.deltaTime;
        if (audioFilterReadSecondsCounter > 1)
        {
            audioFilterReadSampleRateHz = (int)((double)audioFilterReadSampleCounter / audioFilterReadSecondsCounter);
            audioFilterReadSecondsCounter = 0;
            audioFilterReadSampleCounter = 0;
        }
    }

    public void InitIfNotDoneYet()
    {
        if (isInitialized)
        {
            return;
        }

        midiStreamSynthesizer = new StreamSynthesizer(midiStreamSampleRateHz, midiStreamChannelCount, bufferSize, 16);
        newSampleBuffer = new float[bufferSize * midiStreamChannelCount];
        availableSynthesizerSamples = new CircularBuffer<float>(midiStreamSampleRateHz / 10);
        availableOutputSamples = new CircularBuffer<float>(outputSampleRateHz / 10);

        midiStreamSynthesizer.LoadBank(bankFilePath);
        midiSequencer = new MidiSequencer(midiStreamSynthesizer);

        isInitialized = true;
    }

    public void PlayMidiFile(MidiFile midiFile)
    {
        InitIfNotDoneYet();
        
        StopAllMidiNotes();
        midiSequencer.LoadMidi(midiFile, false);
        midiSequencer.Play();
        IsPlayingMidiFile = true;
    }

    public void StopMidiFile()
    {
        if (!isInitialized)
        {
            return;
        }
        
        midiSequencer.Stop(true);
        IsPlayingMidiFile = false;
    }
    
    public void PlayMidiNote(int midiNote)
    {
        InitIfNotDoneYet();
        midiStreamSynthesizer.NoteOn(0, midiNote, midiVelocity, midiInstrument);
    }

    public void PlayMidiNoteForDuration(int midiNote, float durationInSeconds)
    {
        InitIfNotDoneYet();
        midiStreamSynthesizer.NoteOn(0, midiNote, midiVelocity, midiInstrument);
        StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(durationInSeconds, () => StopMidiNote(midiNote)));
    }

    public void StopMidiNote(int midiNote)
    {
        InitIfNotDoneYet();
        midiStreamSynthesizer.NoteOff(0, midiNote);
    }

    public void StopAllMidiNotes(bool immediate = true)
    {
        InitIfNotDoneYet();
        midiStreamSynthesizer.NoteOffAll(immediate);
    }

    public MidiFile LoadMidiFile(string path)
    {
        InitIfNotDoneYet();
        MidiFile midiFile = midiSequencer.LoadMidiFromFile(path, false);
        return midiFile;
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
    void OnAudioFilterRead(float[] data, int outputChannelCount)
    {
        if (!isInitialized)
        {
            return;
        }

        // Statistics to find the actually used sample rate
        audioFilterReadSampleCounter += (data.Length / outputChannelCount);

        // Synthesize new samples from the Midi instrument until there is enough to fill the data array.
        int neededSingleChannelSamples = data.Length / outputChannelCount;
        if (availableOutputSamples.Count < neededSingleChannelSamples)
        {
            FillAvailableSampleBuffers();
        }

        // The Midi stream is generated in mono (1 channel).
        // These samples are written to every channel of the output data array.
        for (int sampleIndex = 0; sampleIndex < data.Length; sampleIndex += outputChannelCount)
        {
            float sampleValue = availableOutputSamples.Front() * midiGain;
            availableOutputSamples.PopFront();

            for (int outputChannelIndex = 0; outputChannelIndex < outputChannelCount; outputChannelIndex++)
            {
                data[sampleIndex + outputChannelIndex] = sampleValue;
            }
        }
    }

    private void FillAvailableSampleBuffers()
    {
        // Synthesize midi samples (in midi synthesizer's sample rate).
        SynthesizeMidiSamples();
        // Resample the data for the output device sample rate.
        ReSampleAndFill(availableSynthesizerSamples,
            availableOutputSamples,
            midiStreamSampleRateHz,
            outputSampleRateHz,
            availableOutputSamples.Capacity / 2);
    }

    private void SynthesizeMidiSamples()
    {
        while (availableSynthesizerSamples.Count < availableSynthesizerSamples.Capacity / 2)
        {
            midiStreamSynthesizer.GetNext(newSampleBuffer);
            for (int i = 0; i < newSampleBuffer.Length; i++)
            {
                availableSynthesizerSamples.PushBack(newSampleBuffer[i]);
            }
        }
    }

    private void ReSampleAndFill(CircularBuffer<float> source, CircularBuffer<float> destination, int sourceSampleRate, int destinationSampleRate, int sampleCountToResample)
    {
        destination.PushBack(source[0]);
        float ratio = (float)(sourceSampleRate - 1) / (float)(destinationSampleRate - 1);
        int usedSourceSamples = 0;
        for (int i = 1; i < sampleCountToResample; i++)
        {
            // Interpolate sample using neighboring indexes
            float sourceIndexFloat = (float)i * ratio;
            int sourceIndexInt = (int)sourceIndexFloat;
            float sourceIndexDifference = sourceIndexFloat - (float)sourceIndexInt;
            float sourceSampleDifference = source[sourceIndexInt + 1] - source[sourceIndexInt];
            float interpolatedSample = source[sourceIndexInt] + sourceSampleDifference * sourceIndexDifference;

            destination.PushBack(interpolatedSample);
            usedSourceSamples = sourceIndexInt;
        }
        // Remove used samples from source
        for (int i = 0; i < usedSourceSamples; i++)
        {
            source.PopFront();
        }
    }
}
