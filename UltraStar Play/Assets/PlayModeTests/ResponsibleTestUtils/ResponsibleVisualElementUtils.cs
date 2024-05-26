using System.Collections.Generic;
using Responsible;
using UnityEngine.UIElements;
using static Responsible.Responsibly;

public class ResponsibleVisualElementUtils
{
    public static ITestInstruction<T> GetElement<T>(string uxmlName, string ussClass = null, VisualElement root = null) where T : VisualElement
        => DoAndReturn(
            $"get UI element with UXML name '{uxmlName}' and USS class '{ussClass}' from root element '{GetRootVisualElement(root)?.name}'",
            () => GetRootVisualElement(root).Q<T>(uxmlName, ussClass));

    public static ITestInstruction<List<T>> GetElements<T>(string uxmlName, string ussClass = null, VisualElement root = null) where T : VisualElement
        => DoAndReturn(
            $"get UI elements with UXML name '{uxmlName}' and USS class '{ussClass}' from root element '{GetRootVisualElement(root)?.name}'",
            () => GetRootVisualElement(root).Query<T>(uxmlName, ussClass).ToList());

    public static ITestInstruction<object> SetElementValue<T>(string uxmlName, T newValue)
        => GetElement<BaseField<T>>(uxmlName)
            .ContinueWith(element => SetElementValue(element, newValue));

    public static ITestInstruction<object> SetElementValue<T>(BaseField<T> element, T newValue)
        => ExpectElementIsFocusableNow(element)
            .ContinueWith(Do(
                    $"set value '{newValue}' for UI element '{element?.name}'",
                    () => element.value = newValue)
                .ContinueWith(ExpectElementHasValue(element, newValue)));

    public static ITestInstruction<object> ClickButton(string uxmlName)
        => GetElement<Button>(uxmlName)
            .ContinueWith(element => ClickButton(element));

    public static ITestInstruction<object> ClickButton(Button button)
        => ExpectElementIsFocusableNow(button)
            .ContinueWith(_ => Do(
                $"click button '{button?.name}'",
                () => button.Click()));

    public static ITestInstruction<object> SendNavigationSubmitEvent(VisualElement visualElement)
        => ExpectElementIsFocusableNow(visualElement)
            .ContinueWith(_ => Do(
                $"send NavigationSubmitEvent on '{visualElement?.name}'",
                () => visualElement.SendNavigationSubmitEvent()));

    public static ITestInstruction<object> SendPointerDownEvent(VisualElement visualElement)
        => ExpectElementIsFocusableNow(visualElement)
            .ContinueWith(_ => Do(
                $"Send PointerDownEvent on '{visualElement?.name}'",
                () => visualElement.SendPointerDownEvent()));

    public static ITestInstruction<object> ExpectElementIsFocusableNow(VisualElement element, double timeoutInSeconds = 1)
        => WaitForCondition(
                $"UI element '{element?.name}' should be focusable",
                () => VisualElementUtils.IsFocusableNow(element, GetUiDocumentOrThrow()))
            .ExpectWithinSeconds(timeoutInSeconds);

    public static ITestInstruction<object> ExpectElementHasValue<T>(BaseField<T> element, T value, double timeoutInSeconds = 1)
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
