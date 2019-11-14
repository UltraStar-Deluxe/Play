using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ThemeableAudio : Themeable
{
    [ReadOnly]
    public string audioPath;
    public EAudioResource audioResource = EAudioResource.NONE;
    public AudioSource target;

#if UNITY_EDITOR
    private EAudioResource lastAudioResource = EAudioResource.NONE;
#endif

    void OnEnable()
    {
        if (target == null)
        {
            target = GetComponent<AudioSource>();
        }
    }

#if UNITY_EDITOR
    void Update()
    {
        if (audioResource != EAudioResource.NONE && lastAudioResource != audioResource)
        {
            lastAudioResource = audioResource;
            audioPath = audioResource.GetPath();
            Theme currentTheme = ThemeManager.Instance.GetCurrentTheme();
            ReloadResources(currentTheme);
        }
    }
#endif

    public override void ReloadResources(Theme theme)
    {
        if (string.IsNullOrEmpty(audioPath))
        {
            Debug.LogWarning($"Missing audio file name", gameObject);
            return;
        }
        if (target == null)
        {
            Debug.LogWarning($"Target is null", gameObject);
            return;
        }

        AudioClip audioClip = LoadResourceFromTheme<AudioClip>(theme, audioPath);
        if (audioClip == null)
        {
            Debug.LogError($"Could not load audio file {audioPath}", gameObject);
        }
        else
        {
            target.clip = audioClip;
        }
    }
}
