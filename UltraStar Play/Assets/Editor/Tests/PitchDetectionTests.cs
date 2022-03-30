﻿using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class PitchDetectionTests
{
    [Test]
    public void TestCamdPitchDetection()
    {
        TestPitchDetection(sampleRateHz => new CamdAudioSamplesAnalyzer(sampleRateHz, 2048));
    }

    [Test]
    public void TestDywaPitchDetection()
    {
        TestPitchDetection(sampleRateHz => new DywaAudioSamplesAnalyzer(sampleRateHz, 2048));
    }

    private void TestPitchDetection(Func<int, IAudioSamplesAnalyzer> audioSamplesAnalyzerProvider)
    {
        MicProfile micProfile = CreateDummyMicProfile();

        string assetsPath = Application.dataPath;
        string sineWaveToneDir = assetsPath + "/Common/Audio/SineWaveTones/";
        Dictionary<string, string> pathToExpectedMidiNoteNameMap = new();
        pathToExpectedMidiNoteNameMap.Add(sineWaveToneDir + "sine-wave-a3-220hz.ogg", "A3");
        pathToExpectedMidiNoteNameMap.Add(sineWaveToneDir + "sine-wave-a4-440hz.ogg", "A4");
        pathToExpectedMidiNoteNameMap.Add(sineWaveToneDir + "sine-wave-a5-880hz.ogg", "A5");
        pathToExpectedMidiNoteNameMap.Add(sineWaveToneDir + "sine-wave-c2-61,74hz.ogg", "C2");
        pathToExpectedMidiNoteNameMap.Add(sineWaveToneDir + "sine-wave-c4-261,64hz.ogg", "C4");
        pathToExpectedMidiNoteNameMap.Add(sineWaveToneDir + "sine-wave-c6-1046,50hz.ogg", "C6");

        foreach (KeyValuePair<string, string> pathAndNoteName in pathToExpectedMidiNoteNameMap)
        {
            // Load the audio clip
            string uri = "file://" + pathAndNoteName.Key;
            AudioClip audioClip = AudioUtils.GetAudioClipUncached(uri, false);
            float[] samples = new float[audioClip.samples];
            audioClip.GetData(samples, 0);

            // Analyze the samples
            IAudioSamplesAnalyzer audioSamplesAnalyzer = audioSamplesAnalyzerProvider(audioClip.frequency);
            audioSamplesAnalyzer.Enable();
            PitchEvent pitchEvent = audioSamplesAnalyzer.ProcessAudioSamples(samples, 0, samples.Length - 1, micProfile);

            // Check result
            Assert.NotNull(pitchEvent, $"No pitch detected when analyzing audio resource {uri}");
            string expectedName = pathAndNoteName.Value;
            string analyzedName = MidiUtils.GetAbsoluteName(pitchEvent.MidiNote);
            Assert.AreEqual(expectedName, analyzedName,
                $"Expected {expectedName} but was {analyzedName} when analyzing audio resource {uri}");
        }
    }

    private static MicProfile CreateDummyMicProfile()
    {
        MicProfile result = new("Dummy Mic");
        result.Amplification = 0;
        result.NoiseSuppression = 0;
        result.IsEnabled = true;
        result.Color = Colors.indigo;
        return result;
    }
}
