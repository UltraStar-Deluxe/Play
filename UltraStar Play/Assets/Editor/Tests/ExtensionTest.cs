using NUnit.Framework;

public class ExtensionTest
{
    [Test]
    public void FloatEqualsTest()
    {
        Assert.IsTrue(99f.Equals(100, 1));
        Assert.IsTrue(0.01f.Equals(0, 0.01f));
        Assert.IsTrue((-42.001f).Equals(-42, 0.01f));
        Assert.IsTrue((-42f).Equals(-42.001f, 0.01f));
        Assert.IsTrue((-42.001f).Equals(-40f, 2.01f));

        Assert.IsFalse(0.01f.Equals(0, 0.001f));
    }
}
