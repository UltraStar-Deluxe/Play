using Responsible;
using static Responsible.Responsibly;

public static class ResponsibleTimeUtils
{
    public static ITestInstruction<T> ExpectNow<T>(
        this ITestWaitCondition<T> condition)
        => condition.ExpectWithinSeconds(0);

}
