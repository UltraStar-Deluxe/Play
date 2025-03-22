using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class MedleyCalculationTest
{
    private static readonly string folderPath = $"{Application.dataPath}/Editor/Tests/TestSongs/MedleyTestSongs/";

    private static List<MedleyCalculationTestCase> testCases = new List<MedleyCalculationTestCase>()
    {
        new("OChristmasTree-MedleyStart-MedleyEnd.txt", 396, 1045),
        new("OChristmasTree-MedleyStart.txt", 396, 528),
        new("OChristmasTree-NoMedleyStart-NoMedleyEnd.txt", 660, 858),
    };

    [Test]
    [TestCaseSource(nameof(testCases))]
    public void ShouldCalculateMedleyStartAndMedleyEnd(MedleyCalculationTestCase testCase)
    {
        UltraStarSongMeta songMeta = UltraStarSongParser.ParseFile(folderPath + testCase.FileName).SongMeta;
        Assert.AreEqual(SongMetaMedleyUtils.GetMedleyStartBeat(songMeta), testCase.ExpectedMedleyStartBeat);
        Assert.AreEqual(SongMetaMedleyUtils.GetMedleyEndBeat(songMeta, 10), testCase.ExpectedMedleyEndBeat);
    }

    public class MedleyCalculationTestCase
    {
        public string FileName { get; private set; }
        public int ExpectedMedleyStartBeat { get; private set; }
        public int ExpectedMedleyEndBeat { get; private set; }

        public MedleyCalculationTestCase(string fileName, int expectedMedleyStartBeat, int expectedMedleyEndBeat)
        {
            FileName = fileName;
            ExpectedMedleyStartBeat = expectedMedleyStartBeat;
            ExpectedMedleyEndBeat = expectedMedleyEndBeat;
        }

        public override string ToString()
        {
            return $"{FileName}, {ExpectedMedleyStartBeat}, {ExpectedMedleyEndBeat}";
        }
    }
}
