using PrimeInputActions;
using Responsible;
using static Responsible.Responsibly;

public class AbstractResponsibleTest : AbstractInputSystemTest
{
    protected TestInstructionExecutor Executor { get; set; }

    protected ITestInstruction<object> TriggerInputAction(string inputActionPath)
        => Do($"perform InputAction '{inputActionPath}'",
            () => InputFixture.Trigger(InputManager.GetInputAction(inputActionPath).InputAction));
}
