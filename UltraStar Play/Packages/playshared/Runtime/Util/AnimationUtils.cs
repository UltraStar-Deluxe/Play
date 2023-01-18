using UnityEngine;
using UnityEngine.UIElements;

public static class AnimationUtils
{
    public static int FadeOutVisualElement(GameObject gameObject, VisualElement visualElement, float animTimeInSeconds)
    {
        return LeanTween
            .value(gameObject, visualElement.resolvedStyle.opacity, 0, animTimeInSeconds)
            .setOnUpdate(interpolatedValue => visualElement.style.opacity = interpolatedValue)
            .id;
    }

    public static int FadeInVisualElement(GameObject gameObject, VisualElement visualElement, float animTimeInSeconds)
    {
        return LeanTween
            .value(gameObject, visualElement.resolvedStyle.opacity, 1, animTimeInSeconds)
            .setOnUpdate(interpolatedValue => visualElement.style.opacity = interpolatedValue)
            .id;
    }
}
