using System;
using NUnit.Framework;
using System.Collections.Generic;

public class MidiUtilsTests
{

    [Test]
    public void HalftoneFrequenciesTest()
    {
        // C2, 65.41 Hz
        Assert.True(DoubleEquals(MidiUtils.halftoneFrequencies[0], 65.41, 0.1), "Frequency of C2 is wrong");
        // C6, 1046.50 Hz
        Assert.True(DoubleEquals(MidiUtils.halftoneFrequencies[MidiUtils.halftoneFrequencies.Length - 1], 1046.50, 0.1), "Frequency of C6 is wrong");
        // A4, 440 Hz
        Assert.True(DoubleEquals(MidiUtils.halftoneFrequencies[33], 440, 0.1), "Frequency of A4 is wrong");
    }

    [Test]
    public void HalftoneDelaysTest()
    {
        // C2, 65.41 Hz
        int[] halftoneDelays44100 = MidiUtils.PrecalculateHalftoneDelays(44100);
        int[] halftoneDelays22050 = MidiUtils.PrecalculateHalftoneDelays(22050);
        // At 44100 samples per second, this has a duration of 44100 [sample/second] * (1/65.41) [second] == 674.20 [sample]
        Assert.True(halftoneDelays44100[0] == 674, "Period in samples of C2 is wrong (44100 samples per second)");
        Assert.True(halftoneDelays22050[0] == 337, "Period in samples of C2 is wrong (22050 samples per second)");

        Assert.True(halftoneDelays44100[halftoneDelays44100.Length - 1] == 42, "Period in samples of C6 is wrong (44100 samples per second)");
        Assert.True(halftoneDelays22050[halftoneDelays22050.Length - 1] == 21, "Period in samples of C6 is wrong (22050 samples per second)");
    }

    private bool DoubleEquals(double a, double b, double range)
    {
        return Math.Abs(a - b) < range;
    }

    [Test]
    public void GetRelativeNameTest()
    {
        Dictionary<int, string> midiNoteToRelativeNameMap = new Dictionary<int, string>();
        midiNoteToRelativeNameMap.Add(36, "C");
        midiNoteToRelativeNameMap.Add(57, "A");
        midiNoteToRelativeNameMap.Add(60, "C");
        midiNoteToRelativeNameMap.Add(69, "A");
        midiNoteToRelativeNameMap.Add(81, "A");
        midiNoteToRelativeNameMap.Add(84, "C");
        foreach (KeyValuePair<int, string> midiNoteAndName in midiNoteToRelativeNameMap)
        {
            string noteName = MidiUtils.GetRelativeName(midiNoteAndName.Key);
            Assert.AreEqual(midiNoteAndName.Value, noteName);
        }
    }

    [Test]
    public void GetAbsoluteNameTest()
    {
        Dictionary<int, string> midiNoteToAbsoluteNameMap = new Dictionary<int, string>();
        midiNoteToAbsoluteNameMap.Add(36, "C2");
        midiNoteToAbsoluteNameMap.Add(57, "A3");
        midiNoteToAbsoluteNameMap.Add(60, "C4");
        midiNoteToAbsoluteNameMap.Add(69, "A4");
        midiNoteToAbsoluteNameMap.Add(81, "A5");
        midiNoteToAbsoluteNameMap.Add(84, "C6");
        foreach (KeyValuePair<int, string> midiNoteAndName in midiNoteToAbsoluteNameMap)
        {
            string noteName = MidiUtils.GetAbsoluteName(midiNoteAndName.Key);
            Assert.AreEqual(midiNoteAndName.Value, noteName);
        }
    }

    [Test]
    public void GetRelativePitchDistanceTest()
    {
        // The distance must be computed on relative notes, i.e., the pitch must be taken modulo 12.
        int distanceSamePitchDifferentOctaves = MidiUtils.GetRelativePitchDistance(24, 48);
        Assert.AreEqual(0, distanceSamePitchDifferentOctaves);

        // Shortest distance via 7 to 8 = 2
        int distance1 = MidiUtils.GetRelativePitchDistance(6, 8);
        Assert.AreEqual(2, distance1);

        // Shortest distance via 1, 0, 11, 10 = 4
        int distance2 = MidiUtils.GetRelativePitchDistance(2, 10);
        Assert.AreEqual(4, distance2);
    }
}
