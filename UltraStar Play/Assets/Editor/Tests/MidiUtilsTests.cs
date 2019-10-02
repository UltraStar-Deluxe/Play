using System;
using NUnit.Framework;
using System.Collections.Generic;

public class MidiUtilsTests
{
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
