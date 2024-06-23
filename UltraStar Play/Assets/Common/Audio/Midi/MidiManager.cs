using System.IO;
using AudioSynthesis.Bank;
using AudioSynthesis.Midi;
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

    [InjectedInInspector]
    public TextAsset defaultSoundfontAsset;

    // "volume" for the midi events.
    [Range(0, 127)]
    private int midiVelocity;

    [Inject]
    private Settings settings;

    [Inject]
    private SceneNavigator sceneNavigator;

    private PatchBank patchBank;

    private int audioFilterReadSampleRate;

    private bool isInitialized;
    private bool isAudioSourceInitialized;
    private AudioSource audioSource;

    public bool IsPlayingMidiFile { get; private set; }
    private bool isPlayingMidiNote;
    private long stopMidiNoteTimeMillis;

    private AudioClip midiAudioClip;
    private bool ignoreInitialOnAudioClipSetPositionCallback;

    private MidiSamplesGenerator onAudioFilterReadMidiSamplesGenerator;
    private MidiSamplesGenerator audioClipMidiSamplesGenerator;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void AwakeSingleton()
    {
        audioSource = GetComponent<AudioSource>();
        audioFilterReadSampleRate = UnityEngine.AudioSettings.outputSampleRate;
    }

    protected override void StartSingleton()
    {
        // Synchronize with settings
        midiVelocity = settings.SongEditorSettings.MidiVelocity;
        settings.SongEditorSettings.ObserveEveryValueChanged(it => it.MidiVelocity)
            .Subscribe(newMidiVelocity => midiVelocity = newMidiVelocity)
            .AddTo(gameObject);

        settings.SongEditorSettings.ObserveEveryValueChanged(it => it.MidiGain)
            .Subscribe(newMidiGain =>
            {
                if (onAudioFilterReadMidiSamplesGenerator != null)
                {
                    onAudioFilterReadMidiSamplesGenerator.Gain = newMidiGain;
                }
                if (audioClipMidiSamplesGenerator != null)
                {
                    audioClipMidiSamplesGenerator.Gain = newMidiGain;
                }
            })
            .AddTo(gameObject);

        settings.ObserveEveryValueChanged(it => it.SoundfontPath)
            .Subscribe(newValue => OnSoundfontPathChanged())
            .AddTo(gameObject);

        sceneNavigator.BeforeSceneChangeEventStream.Subscribe(_ =>
        {
            StopMidiFile();
            StopAllMidiNotes();
            DestroyMidiAudioClip();
        });

        // Deactivate until the MidiManager has been initialized.
        // This is to prevent OnAudioFilterRead to create weird noise (probably a Unity bug).
        gameObject.SetActive(false);
        audioSource.enabled = false;
    }

    public void InitIfNotDoneYet()
    {
        if (isInitialized)
        {
            return;
        }
        using DisposableStopwatch d = new DisposableStopwatch("Initialize MidiManager took <ms>");

        InitPatchBank();
        onAudioFilterReadMidiSamplesGenerator = CreateOrUpdateMidiSamplesGenerator(audioClipMidiSamplesGenerator);
        audioClipMidiSamplesGenerator = CreateOrUpdateMidiSamplesGenerator(audioClipMidiSamplesGenerator);

        isInitialized = true;
        gameObject.SetActive(true);
    }

    private void InitAudioSourceIfNotDoneYet()
    {
        if (isAudioSourceInitialized)
        {
            return;
        }
        isAudioSourceInitialized = true;

        audioSource.enabled = true;
        audioSource.Play();
    }

    private void InitPatchBank()
    {
        if (FileUtils.Exists(settings.SoundfontPath))
        {
            patchBank = new PatchBank(new FileSystemSoundfontResource(settings.SoundfontPath));
        }
        else
        {
            if (!settings.SoundfontPath.IsNullOrEmpty())
            {
                NotificationManager.CreateNotification(Translation.Get(R.Messages.common_error_fileNotFoundWithName,
                    "name", Path.GetFileName(settings.SoundfontPath)));
                Debug.LogWarning($"Soundfont file does not exist: {settings.SoundfontPath}");
            }

            patchBank = new PatchBank(new TextAssetSoundfontResource(defaultSoundfontAsset));
        }
    }

    private MidiSamplesGenerator CreateOrUpdateMidiSamplesGenerator(MidiSamplesGenerator existingInstance)
    {
        if (existingInstance == null)
        {
            return new MidiSamplesGenerator(audioFilterReadSampleRate, patchBank, settings.SongEditorSettings.MidiGain, true);
        }
        else
        {
            existingInstance.LoadBank(patchBank);
            existingInstance.Gain = settings.SongEditorSettings.MidiGain;
            return existingInstance;
        }
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
        onAudioFilterReadMidiSamplesGenerator?.UnloadMidi();
        onAudioFilterReadMidiSamplesGenerator?.UnloadBank();
        audioClipMidiSamplesGenerator?.UnloadMidi();
        audioClipMidiSamplesGenerator?.UnloadBank();
        isInitialized = false;
    }

    public void PlayMidiFile(MidiFile midiFile)
    {
        InitIfNotDoneYet();
        InitAudioSourceIfNotDoneYet();

        if (onAudioFilterReadMidiSamplesGenerator.IsMidiLoaded)
        {
            onAudioFilterReadMidiSamplesGenerator.UnloadMidi();
        }
        IsPlayingMidiFile = false;

        StopAllMidiNotes();

        onAudioFilterReadMidiSamplesGenerator.LoadMidi(midiFile);
        IsPlayingMidiFile = true;
    }

    public void StopMidiFile()
    {
        if (!isInitialized)
        {
            return;
        }

        onAudioFilterReadMidiSamplesGenerator.Stop();
        IsPlayingMidiFile = false;
    }

    public void PlayMidiNote(int midiNote)
    {
        InitIfNotDoneYet();
        InitAudioSourceIfNotDoneYet();

        isPlayingMidiNote = true;
        onAudioFilterReadMidiSamplesGenerator.NoteOn(0, midiNote, midiVelocity);
    }

    public void StopMidiNote(int midiNote)
    {
        if (!isPlayingMidiNote)
        {
            return;
        }

        InitIfNotDoneYet();
        onAudioFilterReadMidiSamplesGenerator.NoteOff(0, midiNote);
        isPlayingMidiNote = false;
        stopMidiNoteTimeMillis = TimeUtils.GetUnixTimeMilliseconds();
    }

    public void StopAllMidiNotes(bool immediate = true)
    {
        if (!isInitialized)
        {
            return;
        }

        onAudioFilterReadMidiSamplesGenerator.NoteOffAll(immediate);
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
            || !isAudioSourceInitialized)
        {
            return;
        }

        if (IsPlayingMidiFile
            || isPlayingMidiNote
            || TimeUtils.GetUnixTimeMilliseconds() - stopMidiNoteTimeMillis < 2000)
        {
            onAudioFilterReadMidiSamplesGenerator.FillOutputBuffer(data, outputChannelCount);
        }
    }

    public AudioClip CreateAudioClip(string midiFilePath)
    {
        DestroyMidiAudioClip();

        if (!FileUtils.Exists(midiFilePath))
        {
            Debug.LogError($"MIDI file does not exist: {midiFilePath}");
            return null;
        }

        // This MIDI file will synthesize samples for the AudioClip.
        InitIfNotDoneYet();
        MidiFile midiFile = MidiFileUtils.LoadMidiFile(midiFilePath);
        audioClipMidiSamplesGenerator.LoadMidi(midiFile);

        using (new DisposableStopwatch($"Creating AudioClip from MIDI file '{midiFilePath}' took <ms>"))
        {
            int audioClipSampleRate = audioFilterReadSampleRate;
            double midiFileLengthInMillis = MidiFileUtils.GetMidiFileLengthInMillis(midiFile);
            int audioClipLengthInSamples = (int)((midiFileLengthInMillis / 1000.0) * audioClipSampleRate);

            // Ignore the initial SetPosition callback.
            ignoreInitialOnAudioClipSetPositionCallback = true;

            midiAudioClip = AudioClip.Create($"MIDI file '{Path.GetFileName(midiFilePath)}'",
                audioClipLengthInSamples,
                MidiSamplesGenerator.MidiSynthesizerChannelCount,
                audioClipSampleRate,
                true,
                OnAudioClipRead,
                OnAudioClipSetPosition);

            return midiAudioClip;
        }
    }

    public void DestroyMidiAudioClip()
    {
        if (midiAudioClip != null)
        {
            Debug.Log("Destroy AudioClip of MIDI file");
            Destroy(midiAudioClip);
            midiAudioClip = null;
        }
    }

    private void OnAudioClipRead(float[] data)
    {
        audioClipMidiSamplesGenerator.FillOutputBuffer(data, MidiSamplesGenerator.MidiSynthesizerChannelCount);
    }

    private void OnAudioClipSetPosition(int positionInSamples)
    {
        if (ignoreInitialOnAudioClipSetPositionCallback
            && positionInSamples == 0)
        {
            ignoreInitialOnAudioClipSetPositionCallback = false;
            return;
        }

        audioClipMidiSamplesGenerator.SeekSampleTime(positionInSamples);
    }
}
