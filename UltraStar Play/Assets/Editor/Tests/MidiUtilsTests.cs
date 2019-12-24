using System;
using NUnit.Framework;
using System.Collections.Generic;

public class MidiUtilsTests
{
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
