using System;
using System.Collections;
using UnityEngine;

public class CoroutineUtils
{
    public static IEnumerator ExecuteAfterDelayInSeconds(float delayInSeconds, Action action)
    {
        yield return new WaitForSeconds(delayInSeconds);
        // Code to execute after the delay
        action();
    }

    public static IEnumerator Execute(Action action)
    {
        yield return null;
        action();
    }
}