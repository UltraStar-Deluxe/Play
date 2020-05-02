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
        Assert.AreEqual(0, MidiUtils.GetRelativePitchDistance(24, 48));

        // Shortest distance via 7 to 8 = 2
        Assert.AreEqual(2, MidiUtils.GetRelativePitchDistance(6, 8));
        // Shortest distance via 1, 0, 11, 10 = 4
        Assert.AreEqual(4, MidiUtils.GetRelativePitchDistance(2, 10));

        // No distance
        Assert.AreEqual(0, MidiUtils.GetRelativePitchDistanceSigned(5, 5));
        // Shortest signed distance from F to A -> 4
        Assert.AreEqual(4, MidiUtils.GetRelativePitchDistanceSigned(53, 69));
        // Shortest signed distance via 11, 0, 1, 2 -> 4
        Assert.AreEqual(4, MidiUtils.GetRelativePitchDistanceSigned(10, 2));
        // Shortest signed distance via 1, 0, 11, 10 -> -4
        Assert.AreEqual(-4, MidiUtils.GetRelativePitchDistanceSigned(2, 10));
        // Shortest signed distance via 5, 6, 7, 8 -> 4
        Assert.AreEqual(4, MidiUtils.GetRelativePitchDistanceSigned(4, 8));
        // Shortest signed distance via 7, 6, 5, 4 -> -4
        Assert.AreEqual(-4, MidiUtils.GetRelativePitchDistanceSigned(8, 4));
        // Shortest signed distance via 0 -> 1
        Assert.AreEqual(1, MidiUtils.GetRelativePitchDistanceSigned(11, 0));
        // Shortest signed distance via 11 -> -1
        Assert.AreEqual(-1, MidiUtils.GetRelativePitchDistanceSigned(0, 11));
        // Shortest signed distance from F to B -> 6 or -6
        Assert.IsTrue(MidiUtils.GetRelativePitchDistanceSigned(77, 59).IsOneOf(-6, 6));
        // Shortest signed distance from A to D -> 5
        Assert.AreEqual(5, MidiUtils.GetRelativePitchDistanceSigned(45, 74));
        // Shortest signed distance from D to A -> -5
        Assert.AreEqual(-5, MidiUtils.GetRelativePitchDistanceSigned(74, 45));
    }
}
