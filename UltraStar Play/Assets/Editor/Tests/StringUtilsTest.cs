using NUnit.Framework;

public class StringUtilsTest
{
    [Test]
    public void CountOccurrencesInStringTest()
    {
        Assert.AreEqual(3, StringUtils.CountOccurrencesInString("abcaa", "a"));
        Assert.AreEqual(1, StringUtils.CountOccurrencesInString("abcaa", "ab"));
        Assert.AreEqual(1, StringUtils.CountOccurrencesInString("abcaa", "aa"));
        Assert.AreEqual(1, StringUtils.CountOccurrencesInString("abcaa", "abcaa"));

        // Null or empty should return 0
        Assert.AreEqual(0, StringUtils.CountOccurrencesInString(null, null));
        Assert.AreEqual(0, StringUtils.CountOccurrencesInString("x", null));
        Assert.AreEqual(0, StringUtils.CountOccurrencesInString(null, "x"));

        Assert.AreEqual(0, StringUtils.CountOccurrencesInString("", ""));
        Assert.AreEqual(0, StringUtils.CountOccurrencesInString("x", ""));
        Assert.AreEqual(0, StringUtils.CountOccurrencesInString("", "x"));
    }
}
