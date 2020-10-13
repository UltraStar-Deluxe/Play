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
        if (audioResource != EAudioResource.NONE
            && audioPath != audioResource.GetPath())
        {
            audioPath = audioResource.GetPath();
            ReloadResources(ThemeManager.Instance.CurrentTheme);
        }
    }
#endif

    public override void ReloadResources(Theme theme)
    {
        if (theme == null)
        {
            Debug.LogError("Theme is null", gameObject);
            return;
        }
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

        AudioClip audioClip = theme.FindResource<AudioClip>(audioPath);
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
