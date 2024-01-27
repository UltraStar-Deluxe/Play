using System.Collections;
using Responsible;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using static Responsible.Bdd.Keywords;
using static Responsible.Responsibly;

public class ResponsibleExampleTests : AbstractPlayModeTest
{
    [UnityTest]
    public IEnumerator ResponsibleExampleTest() => WaitForCondition(
        "wait for 4 < 11",
        () => 4 < 11)
        .ExpectWithinSeconds(1)
        .ToYieldInstruction(Executor);
}
