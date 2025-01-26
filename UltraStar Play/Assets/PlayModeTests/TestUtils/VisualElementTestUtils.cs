using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static ConditionUtils;

public class VisualElementTestUtils
{
    public static async Awaitable<T> GetElementAsync<T>(string uxmlName, string ussClass = null, VisualElement root = null, float timeoutInSeconds = 10)
        where T : VisualElement
    {
        return await WaitForObjectAsync(
            () => GetRootVisualElement(root).Q<T>(uxmlName, ussClass),
            new WaitForConditionConfig {
                description = $"UI element with UXML name '{uxmlName}' and USS class '{ussClass}' from root element '{GetRootVisualElement(root)?.name}'",
                timeoutInMillis = timeoutInSeconds * 1000});
    }

    public static async Awaitable<T> GetElementAsync<T>(Func<T, bool> predicate, VisualElement root = null, float timeoutInSeconds = 10)
        where T : VisualElement
    {
        return await WaitForObjectAsync(
            () => GetRootVisualElement(root).Query<T>().Where(predicate).ToList().FirstOrDefault(),
            new WaitForConditionConfig {
                description = $"UI element with predicate from root element '{GetRootVisualElement(root)?.name}'",
                timeoutInMillis = timeoutInSeconds * 1000});
    }

    public static async Awaitable SetElementValueAsync<T>(string uxmlName, T newValue)
    {
        BaseField<T> baseField = await GetElementAsync<BaseField<T>>(uxmlName);
        await SetElementValueAsync(baseField, newValue);
    }

    public static async Awaitable SetElementValueAsync<T>(BaseField<T> element, T newValue)
    {
        await ExpectElementIsFocusableNowAsync(element);
        element.Focus();
        element.value = newValue;
        await ExpectElementHasValueAsync(element, newValue);
    }

    public static async Awaitable ClickButtonAsync(string uxmlName)
    {
        Button element = await GetElementAsync<Button>(uxmlName);
        await ClickButtonAsync(element);
    }

    public static async Awaitable ClickButtonAsync(Button button)
    {
        await ExpectElementIsFocusableNowAsync(button);
        button.SendClickEvent();
    }

    public static async Awaitable SendNavigationSubmitEventAsync(VisualElement visualElement)
    {
        await ExpectElementIsFocusableNowAsync(visualElement);
        visualElement.SendNavigationSubmitEvent();
    }

    public static async Awaitable SendPointerDownEventAsync(VisualElement visualElement)
    {
        await ExpectElementIsFocusableNowAsync(visualElement);
        visualElement.SendPointerDownEvent();
    }

    public static async Awaitable ExpectElementIsFocusableNowAsync(VisualElement element, double timeoutInSeconds = 10)
    {
        await WaitForConditionAsync(
                () => VisualElementUtils.IsFocusableNow(element, GetUiDocumentOrThrow()),
                new WaitForConditionConfig
                {
                    description = $"UI element '{element?.name}' should be focusable",
                    timeoutInMillis = timeoutInSeconds * 1000
                });
    }

    public static async Awaitable ExpectElementHasValueAsync<T>(BaseField<T> element, T value, double timeoutInSeconds = 10)
    {
        await WaitForConditionAsync(
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
