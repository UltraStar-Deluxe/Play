using System;
using Responsible;
using static Responsible.Responsibly;

public static class ResponsibleUtils
{
    public static ITestInstruction<T> WaitForThenDoAndReturn<T>(
        string description,
        Func<T> func,
        float timeoutInSeconds = 10) where T : class
        => WaitForCondition($"wait for {description}",
                () => func() != null)
            .ExpectWithinSeconds(timeoutInSeconds)
            .ContinueWith(DoAndReturn($"get {description}",
                () => func()));
}
