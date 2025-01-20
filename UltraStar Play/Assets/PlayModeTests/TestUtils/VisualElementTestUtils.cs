using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class VisualElementTestUtils
{
    public static async Awaitable<T> GetElement<T>(string uxmlName, string ussClass = null, VisualElement root = null, float timeoutInSeconds = 10)
        where T : VisualElement
    {
        return await ConditionTestUtils.WaitForObjectAsync(
            () => GetRootVisualElement(root).Q<T>(uxmlName, ussClass),
            new WaitForConditionConfig {
                description = $"UI element with UXML name '{uxmlName}' and USS class '{ussClass}' from root element '{GetRootVisualElement(root)?.name}'",
                timeoutInMillis = timeoutInSeconds * 1000});
    }

    public static async Awaitable<T> GetElement<T>(Func<T, bool> predicate, VisualElement root = null, float timeoutInSeconds = 10)
        where T : VisualElement
    {
        return await ConditionTestUtils.WaitForObjectAsync(
            () => GetRootVisualElement(root).Query<T>().Where(predicate).ToList().FirstOrDefault(),
            new WaitForConditionConfig {
                description = $"UI element with predicate from root element '{GetRootVisualElement(root)?.name}'",
                timeoutInMillis = timeoutInSeconds * 1000});
    }

    public static async Awaitable SetElementValue<T>(string uxmlName, T newValue)
    {
        BaseField<T> baseField = await GetElement<BaseField<T>>(uxmlName);
        await SetElementValue(baseField, newValue);
    }

    public static async Awaitable SetElementValue<T>(BaseField<T> element, T newValue)
    {
        await ExpectElementIsFocusableNow(element);
        element.Focus();
        element.value = newValue;
        await ExpectElementHasValue(element, newValue);
    }

    public static async Awaitable ClickButton(string uxmlName)
    {
        Button element = await GetElement<Button>(uxmlName);
        await ClickButton(element);
    }

    public static async Awaitable ClickButton(Button button)
    {
        await ExpectElementIsFocusableNow(button);
        button.SendClickEvent();
    }

    public static async Awaitable SendNavigationSubmitEvent(VisualElement visualElement)
    {
        await ExpectElementIsFocusableNow(visualElement);
        visualElement.SendNavigationSubmitEvent();
    }

    public static async Awaitable SendPointerDownEvent(VisualElement visualElement)
    {
        await ExpectElementIsFocusableNow(visualElement);
        visualElement.SendPointerDownEvent();
    }

    public static async Awaitable ExpectElementIsFocusableNow(VisualElement element, double timeoutInSeconds = 10)
    {
        await ConditionTestUtils.WaitForConditionAsync(
                () => VisualElementUtils.IsFocusableNow(element, GetUiDocumentOrThrow()),
                new WaitForConditionConfig
                {
                    description = $"UI element '{element?.name}' should be focusable",
                    timeoutInMillis = timeoutInSeconds * 1000
                });
    }

    public static async Awaitable ExpectElementHasValue<T>(BaseField<T> element, T value, double timeoutInSeconds = 10)
    {
        await ConditionTestUtils.WaitForConditionAsync(
            () => Equals(element.value, value),
            new WaitForConditionConfig
            {
                description = $"expect value '{value}' in UI element '{element?.name}'",
                timeoutInMillis = timeoutInSeconds * 1000
            });
    }

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
