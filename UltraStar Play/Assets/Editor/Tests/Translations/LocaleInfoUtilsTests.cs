using NUnit.Framework;

public class LocaleInfoUtilsTests
{
    [Test]
    public void GetTwoLetterCountryCodeTest()
    {
        Assert.AreEqual("en", LocaleInfoUtils.GetTwoLetterCountryCode("english"));
        Assert.AreEqual("en", LocaleInfoUtils.GetTwoLetterCountryCode("ENGLISH"));
        Assert.AreEqual("en", LocaleInfoUtils.GetTwoLetterCountryCode("EN"));
        Assert.AreEqual("en", LocaleInfoUtils.GetTwoLetterCountryCode("en"));

        Assert.AreEqual("de", LocaleInfoUtils.GetTwoLetterCountryCode("german"));

        Assert.AreEqual("", LocaleInfoUtils.GetTwoLetterCountryCode(""));
        Assert.AreEqual("", LocaleInfoUtils.GetTwoLetterCountryCode("InvalidLanguage"));
        Assert.AreEqual("", LocaleInfoUtils.GetTwoLetterCountryCode(null));
    }
}
