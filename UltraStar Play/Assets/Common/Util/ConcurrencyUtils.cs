using System;
using System.Collections;
using UnityEngine;

public class ConcurrencyUtils
{
    public static IEnumerator ExecuteAfterDelayInSeconds(float delayInSeconds, Action action)
    {
        yield return new WaitForSeconds(delayInSeconds);
        // Code to execute after the delay
        action();
    }
}