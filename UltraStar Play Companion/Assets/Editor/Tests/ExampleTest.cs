using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ExampleTest
{
    [Test]
    public void ShouldRun()
    {
        LogAssert.ignoreFailingMessages = true;
        Debug.Log("Test did run");
    }
}
