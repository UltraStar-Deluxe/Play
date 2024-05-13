using System.Collections.Generic;
using NUnit.Framework;

public class ListExtensionTest
{
    [Test]
    public void ShouldReturnElementBefore()
    {
        List<string> list = new() { "a", "b", "c", "d" };
        string result1 = list.GetElementBefore("c", false);
        Assert.AreEqual("b", result1);

        string result2 = list.GetElementBefore("a", true);
        Assert.AreEqual("d", result2);

        string result3 = list.GetElementBefore("a", false);
        Assert.IsNull(result3);
    }

    [Test]
    public void ShouldReturnElementAfter()
    {
        List<string> list = new() { "a", "b", "c", "d" };
        string result1 = list.GetElementAfter("c", false);
        Assert.AreEqual("d", result1);

        string result2 = list.GetElementAfter("d", true);
        Assert.AreEqual("a", result2);

        string result3 = list.GetElementAfter("d", false);
        Assert.IsNull(result3);
    }

    [Test]
    public void ShouldReturnElementsBefore()
    {
        List<string> list = new() { "a", "b", "c", "d" };
        List<string> result1 = list.GetElementsBefore("c", false);
        Assert.AreEqual(new List<string> { "a", "b" }, result1);

        List<string> result2 = list.GetElementsBefore("c", true);
        Assert.AreEqual(new List<string> { "a", "b", "c" }, result2);

        List<string> result3 = list.GetElementsBefore("a", false);
        Assert.AreEqual(new List<string>(), result3);

        List<string> result4 = list.GetElementsBefore("x", true);
        Assert.AreEqual(new List<string>(), result4);
    }
}
