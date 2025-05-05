using NUnit.Framework;
using UnityEngine;

public class ExampleTest
{
    [Test]
    public void ShouldRun()
    {
        LogAssertUtils.IgnoreFailingMessages();
        Debug.Log("Test did run");
    }
}
