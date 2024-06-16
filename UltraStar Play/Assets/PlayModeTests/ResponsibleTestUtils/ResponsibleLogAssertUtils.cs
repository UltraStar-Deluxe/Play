using Responsible;
using UnityEngine.TestTools;
using static Responsible.Responsibly;

public class ResponsibleLogAssertUtils
{
   public static ITestInstruction<object> IgnoreFailingMessages(bool shouldIgnore = true)
        => Do($"ignore failing messages: {shouldIgnore}",
                () => LogAssert.ignoreFailingMessages = shouldIgnore);
}
