using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ThemeableAudio : Themeable
{
    public string audioName;
    public AudioSource target;

    void OnEnable()
    {
        if (target == null)
        {
            target = GetComponent<AudioSource>();
        }
    }

    public override void ReloadResources()
    {
        if (string.IsNullOrEmpty(audioName))
        {
            Debug.LogWarning($"Missing audio file name", gameObject);
            return;
        }
        if (target == null)
        {
            Debug.LogWarning($"Target is null", gameObject);
            return;
        }

        AudioClip audioClip = LoadAssetFromTheme<AudioClip>(audioName);
        if (audioClip == null)
        {
            Debug.LogError($"Could not load audio file {audioName}", gameObject);
        }
        else
        {
            target.clip = audioClip;
        }
    }
}
