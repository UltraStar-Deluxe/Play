using System.Collections;
using System.Collections.Generic;
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

    public static int BounceVisualElementSize(GameObject gameObject, VisualElement visualElement, float animTimeInSeconds)
    {
        return LeanTween.value(gameObject, Vector3.one * 0.75f, Vector3.one, animTimeInSeconds)
            .setEaseSpring()
            .setOnUpdate(s => visualElement.style.scale = new StyleScale(new Scale(new Vector3(s, s, 1))))
            .id;
    }
    
    public static IEnumerator FadeOutThenRemoveVisualElementCoroutine(
        VisualElement visualElement,
        float solidTimeInSeconds,
        float fadeOutTimeInSeconds)
    {
        yield return new WaitForSeconds(solidTimeInSeconds);
        float startOpacity = visualElement.resolvedStyle.opacity;
        float startTime = Time.time;
        while (visualElement.resolvedStyle.opacity > 0)
        {
            float newOpacity = Mathf.Lerp(startOpacity, 0, (Time.time - startTime) / fadeOutTimeInSeconds);
            if (newOpacity < 0)
            {
                newOpacity = 0;
            }

            visualElement.style.opacity = newOpacity;
            yield return null;
        }

        // Remove VisualElement
        if (visualElement.parent != null)
        {
            visualElement.parent.Remove(visualElement);
        }
    }
    
    public static IEnumerator TransitionBackgroundImageGradientCoroutine(VisualElement bgTest, List<GradientConfig> gradientConfigs, float animTimeInSeconds)
    {
        foreach (GradientConfig gradientConfig in gradientConfigs)
        {
            bgTest.style.backgroundImage = GradientManager.GetGradientTexture(gradientConfig);
            yield return new WaitForSeconds(animTimeInSeconds / gradientConfigs.Count);
        }
    }
}
