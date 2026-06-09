using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

public class HeadlessExampleTest
{
    [Test]
    public async Task ShouldSucceed()
    {
        Debug.Log("Hello from Unity API");
        await Task.Delay(100);
        Console.WriteLine("Test done");
    }
}
