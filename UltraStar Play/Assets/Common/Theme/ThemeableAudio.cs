using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[ExecuteInEditMode]
public class ThemeableAudio : Themeable
{
    [Delayed]
    public string audioPath;

    private AudioSource target;

    private void Awake()
    {
        target = GetComponent<AudioSource>();
    }

#if UNITY_EDITOR
    private string lastAudioPath;

    override protected void Start()
    {
        target = GetComponent<AudioSource>();
        base.Start();
        lastAudioPath = audioPath;
    }

    private void Update()
    {
        if (lastAudioPath != audioPath)
        {
            lastAudioPath = audioPath;
            ReloadResources(ThemeManager.CurrentTheme);
        }
    }

    override public List<UnityEngine.Object> GetAffectedObjects()
    {
        return new List<UnityEngine.Object> { target };
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
            Debug.LogWarning($"Missing audio file path", gameObject);
            return;
        }
        if (target == null)
        {
            Debug.LogWarning($"Target is null", gameObject);
            return;
        }

        AudioClip newAudioClip = theme.LoadAudioClip(audioPath);
        if (newAudioClip == null)
        {
            Debug.LogWarning($"Could not load file '{audioPath}' from theme '{theme.Name}'");
        }
        else
        {
            target.clip = newAudioClip;
        }
    }
}
