using System;
using System.Linq;
using Responsible;
using UnityEngine.UIElements;
using static Responsible.Responsibly;
using static ResponsibleUtils;

public class ResponsibleVisualElementUtils
{
    public static ITestInstruction<T> GetElement<T>(string uxmlName, string ussClass = null, VisualElement root = null, float timeoutInSeconds = 10) where T : VisualElement
        => WaitForThenDoAndReturn(
            $"UI element with UXML name '{uxmlName}' and USS class '{ussClass}' from root element '{GetRootVisualElement(root)?.name}'",
            () => GetRootVisualElement(root).Q<T>(uxmlName, ussClass),
            timeoutInSeconds);

    public static ITestInstruction<T> GetElement<T>(Func<T, bool> predicate, VisualElement root = null, float timeoutInSeconds = 10)
        where T : VisualElement
        => WaitForThenDoAndReturn($"UI element with predicate from root element '{GetRootVisualElement(root)?.name}'",
                () => GetRootVisualElement(root).Query<T>().Where(predicate).ToList().FirstOrDefault(),
                timeoutInSeconds);

    public static ITestInstruction<object> SetElementValue<T>(string uxmlName, T newValue)
        => GetElement<BaseField<T>>(uxmlName)
            .ContinueWith(element => SetElementValue(element, newValue));

    public static ITestInstruction<object> SetElementValue<T>(BaseField<T> element, T newValue)
        => ExpectElementIsFocusableNow(element)
            .ContinueWith(Do($"focus UI element {element.name}", () => element.Focus()))
            .ContinueWith(Do(
                    $"set value '{newValue}' for UI element '{element.name}'",
                    () => element.value = newValue)
                .ContinueWith(ExpectElementHasValue(element, newValue)));

    public static ITestInstruction<object> ClickButton(string uxmlName)
        => GetElement<Button>(uxmlName)
            .ContinueWith(element => ClickButton(element));

    public static ITestInstruction<object> ClickButton(Button button)
        => ExpectElementIsFocusableNow(button)
            .ContinueWith(Do(
                $"click button '{button?.name}'",
                () => button.Click()));

    public static ITestInstruction<object> SendNavigationSubmitEvent(VisualElement visualElement)
        => ExpectElementIsFocusableNow(visualElement)
            .ContinueWith(Do(
                $"send NavigationSubmitEvent on '{visualElement?.name}'",
                () => visualElement.SendNavigationSubmitEvent()));

    public static ITestInstruction<object> SendPointerDownEvent(VisualElement visualElement)
        => ExpectElementIsFocusableNow(visualElement)
            .ContinueWith(Do(
                $"Send PointerDownEvent on '{visualElement?.name}'",
                () => visualElement.SendPointerDownEvent()));

    public static ITestInstruction<object> ExpectElementIsFocusableNow(VisualElement element, double timeoutInSeconds = 10)
        => WaitForCondition(
                $"UI element '{element?.name}' should be focusable",
                () => VisualElementUtils.IsFocusableNow(element, GetUiDocumentOrThrow()))
            .ExpectWithinSeconds(timeoutInSeconds);

    public static ITestInstruction<object> ExpectElementHasValue<T>(BaseField<T> element, T value, double timeoutInSeconds = 10)
        => WaitForCondition(
                $"expect value '{value}' in UI element '{element?.name}'",
                () => Equals(element.value, value))
            .ExpectWithinSeconds(timeoutInSeconds);

    private static UIDocument GetUiDocumentOrThrow()
    {
        return UIDocumentUtils.FindUIDocumentOrThrow();
    }

    private static VisualElement GetRootVisualElement(VisualElement root)
    {
        return root != null
            ? root
            : GetUiDocumentOrThrow().rootVisualElement;
    }
}
