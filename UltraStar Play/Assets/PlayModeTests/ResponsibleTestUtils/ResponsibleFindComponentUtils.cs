using NUnit.Framework;
using Responsible;
using UnityEngine;
using static Responsible.Responsibly;

public class ResponsibleFindComponentUtils
{
    public static ITestInstruction<T> FindFirstObjectByType<T>(FindObjectsInactive findObjectsInactive = FindObjectsInactive.Exclude) where T : Object
        => DoAndReturn(
            $"find first object by type '{typeof(T)}' and '{findObjectsInactive}' inactive",
            () =>
            {
                T result = Object.FindFirstObjectByType<T>(findObjectsInactive);
                Assert.IsNotNull(result);
                return result;
            });
}
