using AudioSynthesis.Bank;
using AudioSynthesis.Midi;
using AudioSynthesis.Sequencer;
using AudioSynthesis.Synthesis;
using CircularBuffer;
using UnityEngine;

public class MidiSamplesGenerator
{
    // MIDI sound is generated for 1 channel (mono).
    public const int MidiSynthesizerChannelCount = 1;
    
    // Factor to amplify the generated midi samples.
    public float Gain
    {
        get => midiSynthesizer.MixGain;
        set => midiSynthesizer.MixGain = value;
    }

    public bool IsMidiLoaded => midiSequencer.IsMidiLoaded;
    
    public bool Loop { get; set; }

    private readonly int bufferSize = 1024;

    private MidiFileSequencer midiSequencer;
    private Synthesizer midiSynthesizer;

    private CircularBuffer<float> availableSingleChannelOutputSamples;

    public MidiSamplesGenerator(int sampleRate, PatchBank patchBank, float gain = 0.75f, bool loop = false)
    {
        availableSingleChannelOutputSamples = new CircularBuffer<float>(sampleRate);

        midiSynthesizer = new Synthesizer(sampleRate, MidiSynthesizerChannelCount, bufferSize, 16);
        midiSynthesizer.UnloadBank();
        midiSynthesizer.LoadBank(patchBank);
        midiSequencer = new MidiFileSequencer(midiSynthesizer);
        Gain = gain;
        Loop = loop;
    }
    
    public void FillOutputBuffer(float[] data, int outputChannelCount)
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
            midiSequencer.FillMidiEventQueue(Loop);
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

    public void LoadMidi(MidiFile midiFile)
    {
        if (IsMidiLoaded)
        {
            UnloadMidi();
        }
        
        midiSequencer.LoadMidi(midiFile);
        midiSequencer.Play();
    }

    public void Play()
    {
        midiSequencer.Play();
    }
    
    public void Stop()
    {
        midiSequencer.Stop();
        midiSequencer.ResetMidi();
    }
    
    public void UnloadMidi()
    {
        midiSequencer.Stop();
        midiSequencer.ResetMidi();
        midiSequencer.UnloadMidi();
        availableSingleChannelOutputSamples.Clear();
    }

    public void UnloadBank()
    {
        midiSynthesizer.UnloadBank();
    }
    
    public void LoadBank(PatchBank patchBank)
    {
        midiSynthesizer.UnloadBank();
        midiSynthesizer.LoadBank(patchBank);
    }

    public void NoteOn(int channel, int midiNote, int midiVelocity)
    {
        midiSynthesizer.NoteOn(channel, midiNote, midiVelocity);
    }

    public void NoteOff(int channel, int midiNote)
    {
        midiSynthesizer.NoteOff(channel, midiNote);
    }

    public void NoteOffAll(bool immediate)
    {
        midiSynthesizer.NoteOffAll(immediate);
    }

    public void SeekSampleTime(int positionInSamples)
    {
        midiSequencer.SeekSampleTime(positionInSamples);
    }
}
