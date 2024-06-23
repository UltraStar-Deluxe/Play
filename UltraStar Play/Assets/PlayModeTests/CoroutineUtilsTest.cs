using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.TestTools;

public class CoroutineUtilsTest : AbstractPlayModeTest
{
    [UnityTest]
    public IEnumerator SequenceIsExecutedStepByStep()
    {
        LogAssert.ignoreFailingMessages = true;

        List<string> list = new();

        ApplicationManager.Instance.StartCoroutine(CoroutineUtils.Sequence(
            AddTextCoroutine(list, "a", false),
            AddTextCoroutine(list, "b", false),
            AddTextCoroutine(list, "c", false)
        ));

        long startTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        yield return new WaitUntil(() => list.Count == 3 || TimeUtils.IsDurationAboveThresholdInMillis(startTimeInMillis, 500));

        Assert.IsTrue(list.SequenceEqual(new List<string> () { "a", "b", "c"}));
    }

    [UnityTest]
    public IEnumerator ExceptionInCoroutineStopsSequence()
    {
        LogAssert.ignoreFailingMessages = true;

        List<string> list = new();

        ApplicationManager.Instance.StartCoroutine(CoroutineUtils.Sequence(
            AddTextCoroutine(list, "a", false),
            AddTextCoroutine(list, "b", true),
            AddTextCoroutine(list, "c", false)
        ));

        long startTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        yield return new WaitUntil(() => list.Count == 3 || TimeUtils.IsDurationAboveThresholdInMillis(startTimeInMillis, 500));

        Assert.IsTrue(list.SequenceEqual(new List<string> () { "a" }));
    }

    private IEnumerator<string> AddTextCoroutine(List<string> list, string text, bool throwException)
    {
        if (throwException)
        {
            throw new System.Exception("Dummy exception in coroutine");
        }
        list.Add(text);
        yield return null;
    }
}
