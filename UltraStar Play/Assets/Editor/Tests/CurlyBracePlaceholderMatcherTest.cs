using System.Collections.Generic;
using NUnit.Framework;
using SimpleHttpServerForUnity;

public class CurlyBracePlaceholderMatcherTest
{
    [Test]
    public void DoTestCurlyBracePlaceholderMatcher()
    {
        CurlyBracePlaceholderMatcher matcher = new CurlyBracePlaceholderMatcher("http://api/{foo}/{bar}");
        
        bool isMatch = matcher.TryMatch("http://api/fooValue", out Dictionary<string, string> placeholderValues1);
        Assert.IsFalse(isMatch);
        Assert.IsNull(placeholderValues1);
        
        isMatch = matcher.TryMatch("http://api/fooValue/barValue", out Dictionary<string, string> placeholderValues2);
        Assert.IsTrue(isMatch);
        Assert.AreEqual("fooValue", placeholderValues2["foo"]);
        Assert.AreEqual("barValue", placeholderValues2["bar"]);
    }
}
