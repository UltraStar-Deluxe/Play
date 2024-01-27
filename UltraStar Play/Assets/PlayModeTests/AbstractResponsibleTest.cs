using System;
using System.Collections;
using Responsible;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using static Responsible.Responsibly;

public class AbstractResponsibleTest : AbstractInputSystemTest
{
    protected TestInstructionExecutor Executor { get; set; }

    protected ITestInstruction<object> WriteTextViaKeyboard(string text) => WaitForCoroutine(
        $"write text '{text}' via keyboard",
        () => WriteTextViaKeyboardCoroutine(text))
        .ExpectWithinSeconds(text.Length * 0.1f);

    private IEnumerator WriteTextViaKeyboardCoroutine(string text)
    {
        foreach (char c in text)
        {
            Key key = (Key)Enum.Parse(typeof(Key), c.ToString().ToUpper());
            KeyControl keyControl = Keyboard[key];
            Debug.Log($"Typing character '{c}' via key {key} that is mapped to InputControl {keyControl}");
            InputFixture.Press(keyControl);
            yield return new WaitForEndOfFrame();
            InputFixture.Release(keyControl);
            yield return new WaitForEndOfFrame();
        }
    }
}
