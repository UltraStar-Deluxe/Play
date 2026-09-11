using System.Collections.Generic;
using NUnit.Framework;

public class StringUtilsTest
{
    [Test]
    public void ShouldTrimStartAndEnd()
    {
        string prefix = "prefix-";
        string suffix = "-suffix";
        string middle = "-middle-";
        string text = $"{prefix}{prefix}{middle}{suffix}{suffix}";

        // Should remove only first prefix resp. suffix
        Assert.AreEqual($"{prefix}{middle}{suffix}{suffix}", text.TrimStart(prefix));
        Assert.AreEqual($"{prefix}{prefix}{middle}{suffix}", text.TrimEnd(suffix));

        // Test empty string and empty pattern
        Assert.AreEqual($"", "".TrimStart(prefix));
        Assert.AreEqual($"", "".TrimEnd(suffix));

        Assert.AreEqual($"abc", "abc".TrimStart(""));
        Assert.AreEqual($"abc", "abc".TrimEnd(""));
    }

    [Test]
    public void ShouldReplaceInvalidCharacters()
    {
        Assert.AreEqual("dummy-file__-name", StringUtils.ReplaceInvalidChars(
            "dummy-file*|-name", '_', new HashSet<char>() { '*', '|' }));

        Assert.AreEqual("XA bX cC dD eE fF", StringUtils.ReplaceInvalidChars(
            "aA bB cC dD eE fF", 'X', new HashSet<char>() { 'a', 'B' }));
    }

    [Test]
    public void ShouldCountOccurrencesInString()
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
