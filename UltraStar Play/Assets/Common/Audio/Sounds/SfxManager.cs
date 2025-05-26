using System.Collections.Generic;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.Audio;

public class SfxManager : AbstractSingletonBehaviour, INeedInjection
{
    private const string SfxAudioMixerName = "Sfx";
    private const string VolumeParameterName = "Volume";

    public static SfxManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<SfxManager>();

    private readonly Dictionary<AudioClip, int> audioClipToLastPlayedFrameCount = new();

    [InjectedInInspector]
    public AudioMixer mainAudioMixer;

    [InjectedInInspector]
    public AudioClip defaultButtonSound;

    [InjectedInInspector]
    public AudioClip songSelectSound;

    [InjectedInInspector]
    public AudioClip singingResultsRatingPopupSound;

    [Inject]
    private Settings settings;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        settings.ObserveEveryValueChanged(it => it.SfxVolumePercent)
            .Subscribe(newValue => SetAudioMixerGroupVolume(SfxAudioMixerName, newValue / 100f))
            .AddTo(gameObject);
    }

    public static void PlaySoundEffect(AudioClip clip, float volume = 1)
    {
        SfxManager instance = Instance;
        if (instance == null)
        {
            return;
        }
        instance.DoPlaySoundEffect(clip, volume);
    }

    private void DoPlaySoundEffect(AudioClip clip, float volume = 1)
    {
        if (clip == null)
        {
            return;
        }

        if (audioClipToLastPlayedFrameCount.TryGetValue(clip, out int lastPlayedFrameCount)
            && lastPlayedFrameCount == Time.frameCount)
        {
            return;
        }
        audioClipToLastPlayedFrameCount[clip] = Time.frameCount;

        SfxManager instance = Instance;
        if (instance == null
            || instance.settings.SfxVolumePercent <= 0
            || volume <= 0)
        {
            return;
        }

        GameObject sfxInstance = new GameObject($"Sfx '{clip.name}'");

        AudioSource source = sfxInstance.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.Play();

        // set the mixer group (e.g. music, sfx, etc.)
        source.outputAudioMixerGroup = GetAudioMixerGroup(SfxAudioMixerName);

        // destroy after clip length
        Destroy(sfxInstance, clip.length);
    }

    public static void PlayButtonSound()
    {
        if (Instance == null)
        {
            return;
        }

        PlaySoundEffect(Instance.defaultButtonSound, 0.5f);
    }

    public static void PlaySongSelectSound()
    {
        if (Instance == null)
        {
            return;
        }
        PlaySoundEffect(Instance.songSelectSound, 0.3f);
    }

    public static void PlaySingingResultsRatingPopupSound()
    {
        if (Instance == null)
        {
            return;
        }

        PlaySoundEffect(Instance.singingResultsRatingPopupSound, 0.5f);
    }

    public AudioMixerGroup GetAudioMixerGroup(string groupName)
    {
        if (mainAudioMixer == null)
            return null;

        AudioMixerGroup[] groups = mainAudioMixer.FindMatchingGroups(groupName);

        foreach (AudioMixerGroup match in groups)
        {
            if (match.ToString() == groupName)
                return match;
        }
        return null;
    }

    /**
     * Converts linear value between 0 and 1 into decibels and sets AudioMixer level
     */
    private void SetAudioMixerGroupVolume(string groupName, float linearValue)
    {
        float decibelValue = VolumeUnitUtils.GetDecibelValue(linearValue);
        if (mainAudioMixer != null)
        {
            mainAudioMixer.SetFloat(groupName + VolumeParameterName, decibelValue);
        }
    }

    /**
     * returns a value between 0 and 1 based on the AudioMixer's decibel value
     */
    private float GetAudioMixerGroupVolume(string groupName)
    {
        float decibelValue = 0f;
        if (mainAudioMixer != null)
        {
            mainAudioMixer.GetFloat(groupName, out decibelValue);
        }
        return VolumeUnitUtils.GetLinearValue(decibelValue);
    }
}
