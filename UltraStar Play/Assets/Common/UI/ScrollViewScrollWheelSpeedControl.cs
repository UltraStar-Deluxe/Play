using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

/**
 * Defines a usable values for how much scrolling is performed in a ScrollView
 * when using a scroll wheel on a mouse.
 *
 * See https://forum.unity.com/threads/mouse-wheel-on-scrollviews-very-slow-with-unity-2021-1-x.1111492/
 */
public class ScrollViewScrollWheelSpeedControl : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    [Inject] private UIDocument uiDocument;

    public void OnInjectionFinished()
    {
        DoUpdateScrollWheelSpeedOfAllScrollViews(uiDocument.rootVisualElement);
    }

    public static void UpdateScrollWheelSpeedOfAllScrollViews(VisualElement root)
    {
        ScrollViewScrollWheelSpeedControl instance = FindObjectOfType<ScrollViewScrollWheelSpeedControl>();
        if (instance == null)
        {
            return;
        }

        instance.DoUpdateScrollWheelSpeedOfAllScrollViews(root);
    }

    private void DoUpdateScrollWheelSpeedOfAllScrollViews(VisualElement root)
    {
        List<ScrollView> scrollViews = root.Query<ScrollView>()
            .ToList();

        foreach (ScrollView scrollView in scrollViews)
        {
            TryUpdateScrollWheelSpeed(scrollView);
            scrollView.RegisterCallback<GeometryChangedEvent>(evt => OnScrollViewGeometryChanged(scrollView, evt));
        }
    }

    private void OnScrollViewGeometryChanged(ScrollView scrollView, GeometryChangedEvent evt)
    {
        TryUpdateScrollWheelSpeed(scrollView);
    }

    private void TryUpdateScrollWheelSpeed(ScrollView scrollView)
    {
        if (!VisualElementUtils.HasGeometryAndNonZeroSize(scrollView))
        {
            return;
        }

        // TODO: These values are exposed to USS, but setting them in USS did not work properly.
        // mouseWheelScrollSize behaves differently during play mode, i.e., scrolling is much much slower, so value needs to be bigger
        // See https://github.com/achimmihca/ScrollViewScrollWheelSpeedInUnity
        if (PlatformUtils.IsWindows)
        {
            scrollView.mouseWheelScrollSize = 40000;
        }
    }
}
