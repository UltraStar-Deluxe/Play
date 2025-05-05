using System;
using UnityEngine;

public static class PitchShifterUtils
{
    public static void SetPitchWithPitchShifter(AudioSource audioSource, float pitch)
    {
        if (audioSource == null
            || Math.Abs(audioSource.pitch - pitch) < 0.01f)
        {
            return;
        }

        if (audioSource.outputAudioMixerGroup == null
            || audioSource.outputAudioMixerGroup.audioMixer == null)
        {
            audioSource.outputAudioMixerGroup = AudioManager.Instance.pitchShifterAudioMixerGroup;
        }

        // Setting the pitch of an AudioPlayer will change tempo and pitch.
        audioSource.pitch = pitch;

        // A Pitch Shifter effect on an AudioMixerGroup can be used to compensate the pitch change of the AudioPlayer,
        // such that only the change of the tempo remains.
        // See here for details: https://answers.unity.com/questions/25139/how-i-can-change-the-speed-of-a-song-or-sound.html
        // See here for how the pitch value of the Pitch Shifter effect is made available for scripting: https://learn.unity.com/tutorial/audio-mixing#5c7f8528edbc2a002053b506
        audioSource.outputAudioMixerGroup.audioMixer.SetFloat("PitchShifter.Pitch", 1 + (1 - pitch));
    }

    public static void ResetPitchAndPitchShifter(AudioSource audioSource)
    {
        SetPitchWithPitchShifter(audioSource, 1);
    }
}
