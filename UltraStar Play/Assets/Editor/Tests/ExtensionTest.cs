using System;
using NUnit.Framework;

public class ExtensionTest
{
    [Test]
    public void FloatNearlyEqualsTest()
    {
        Assert.IsTrue(99f.NearlyEquals(100, 1));
        Assert.IsTrue(0.01f.NearlyEquals(0, 0.01f));
        Assert.IsTrue((-42.001f).NearlyEquals(-42, 0.01f));
        Assert.IsTrue((-42f).NearlyEquals(-42.001f, 0.01f));
        Assert.IsTrue((-42.001f).NearlyEquals(-40f, 2.01f));
        
        Assert.IsFalse(0.01f.NearlyEquals(0, 0.001f));
    }
}
