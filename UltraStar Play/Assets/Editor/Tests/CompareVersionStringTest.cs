using System.Collections.Generic;
using NUnit.Framework;

public class CompareVersionStringTest
{
    [Test]
    public void CompareVersionStringTestMethod()
    {
        Dictionary<string, string> smallVersionToBigVersion = new()
        {
            {"5", "20"},
            {"1.1.1", "1.1.2"},
            {"1.2.1", "1.2.2"},
            {"1.1.2", "1.2.0"},
            {"1.2.0", "1.2.0.5"},
            {"2020.1.5f", "2020.1.6f"},
            {"2020.1.4f", "2020.1.6f"},
            {"2020.04", "2020.9"},
            {"2020.9", "2020.11"},
            {"2020.1", "2020.1-devbuild1"},
            {"2020.1-devbuild1", "2020.1-devbuild2"},
        };

        foreach (KeyValuePair<string, string> smallVersionAndBigVersion in smallVersionToBigVersion)
        {
            string smallVersion = smallVersionAndBigVersion.Key;
            string bigVersion = smallVersionAndBigVersion.Value;

            DoTest(-1, smallVersion, bigVersion);
            DoTest(1, bigVersion, smallVersion);
            DoTest(0, smallVersion, smallVersion);
            DoTest(0, bigVersion, bigVersion);
        }
    }

    private void DoTest(int expectedResult, string versionA, string versionB)
    {
        int actualResult = NewVersionChecker.CompareVersionString(versionA, versionB);
        Assert.AreEqual(expectedResult, actualResult, $"Compare('{versionA}', '{versionB}') returned {actualResult} but expected {expectedResult}");
    }
}
