using NUnit.Framework;

public class CompareVersionStringTest
{
    [Test]
    [TestCase("5", "20")]
    [TestCase("1.1.1", "1.1.2")]
    [TestCase("1.2.1", "1.2.2")]
    [TestCase("1.1.2", "1.2.0")]
    [TestCase("1.2.0", "1.2.0.5")]
    [TestCase("2020.1.5f", "2020.1.6f")]
    [TestCase("2020.1.4f", "2020.1.6f")]
    [TestCase("2020.04", "2020.9")]
    [TestCase("2020.9", "2020.11")]
    [TestCase("2020.1", "2020.1-devbuild1")]
    [TestCase("2020.1-devbuild1", "2020.1-devbuild2")]
    public void ComparisonHasCorrectResult(string smallVersion, string bigVersion)
    {
        AssertComparisonResult(-1, smallVersion, bigVersion);
        AssertComparisonResult(1, bigVersion, smallVersion);
        AssertComparisonResult(0, smallVersion, smallVersion);
        AssertComparisonResult(0, bigVersion, bigVersion);
    }

    private void AssertComparisonResult(int expectedResult, string versionA, string versionB)
    {
        int actualResult = NewVersionChecker.CompareVersionString(versionA, versionB);
        Assert.AreEqual(expectedResult, actualResult, $"Compare('{versionA}', '{versionB}') returned {actualResult} but expected {expectedResult}");
    }
}
