using System;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class PitchDetectionTests
{
    [Test]
    public void TestPitchDetection()
    {
        MicProfile micProfile = CreateDummyMicProfile();

        string assetsPath = Application.dataPath;
        string sineWaveToneDir = assetsPath + "/Editor/Tests/SineWaveTones/";
        Dictionary<string, string> pathToExpectedMidiNoteNameMap = new Dictionary<string, string>();
        pathToExpectedMidiNoteNameMap.Add(sineWaveToneDir + "sine-wave-a3-220hz.ogg", "A3");
        pathToExpectedMidiNoteNameMap.Add(sineWaveToneDir + "sine-wave-a4-440hz.ogg", "A4");
        pathToExpectedMidiNoteNameMap.Add(sineWaveToneDir + "sine-wave-a5-880hz.ogg", "A5");
        pathToExpectedMidiNoteNameMap.Add(sineWaveToneDir + "sine-wave-c2-61,74hz.ogg", "C2");
        pathToExpectedMidiNoteNameMap.Add(sineWaveToneDir + "sine-wave-c4-261,64hz.ogg", "C4");
        pathToExpectedMidiNoteNameMap.Add(sineWaveToneDir + "sine-wave-c6-1046,50hz.ogg", "C6");

        foreach (KeyValuePair<string, string> pathAndNoteName in pathToExpectedMidiNoteNameMap)
        {
            // Load the audio clip
            string path = pathAndNoteName.Key;
            AudioClip audioClip = AudioUtils.GetAudioClip(path);
            float[] samples = new float[audioClip.samples];
            audioClip.GetData(samples, 0);

            // Analyze the samples
            IAudioSamplesAnalyzer audioSamplesAnalyzer = new CamdAudioSamplesAnalyzer(audioClip.frequency);
            audioSamplesAnalyzer.Enable();
            PitchEvent pitchEvent = audioSamplesAnalyzer.ProcessAudioSamples(samples, samples.Length, micProfile);

            // Check result
            Assert.NotNull(pitchEvent, $"No pitch detected when analyzing {path}");
            string expectedName = pathAndNoteName.Value;
            string analyzedName = MidiUtils.GetAbsoluteName(pitchEvent.MidiNote);
            Assert.AreEqual(expectedName, analyzedName,
                $"Expected {expectedName} but was {analyzedName} when analyzing {path}");
        }
    }

    private static MicProfile CreateDummyMicProfile()
    {
        MicProfile result = new MicProfile("Dummy Mic");
        result.Amplification = 0;
        result.NoiseSuppression = 0;
        result.IsEnabled = true;
        result.Color = Colors.indigo;
        return result;
    }
}
