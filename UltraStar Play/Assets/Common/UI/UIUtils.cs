using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public static class UIUtils
{
    public static void ForEachElementWithClass(VisualElement root, Action<VisualElement> callback, params string[] classNames)
    {
        foreach (string className in classNames)
        {
            root.Query(null, className).ForEach(callback);
        }
    }

    static readonly Dictionary<VisualElement, Tuple<Color, Color>> ElementsBgColorsDict = new ();

    public static void SetBackgroundStyleWithHover(VisualElement root, Color backgroundColor, Color hoverBackgroundColor, Color fontColor)
    {
        SetBackgroundStyleWithHover(root, root, backgroundColor, hoverBackgroundColor, fontColor);
    }

    public static void SetBackgroundStyleWithHover(VisualElement root, VisualElement hoverRoot, Color backgroundColor, Color hoverBackgroundColor, Color fontColor)
    {
        if (root == null)
        {
            return;
        }

        root.style.color = fontColor;
        root.style.backgroundColor = backgroundColor;

        // We can't access pseudo states through the API (e.g. :hover), so we have to manually mimic them
        if (!ElementsBgColorsDict.ContainsKey(hoverRoot))
        {
            ElementsBgColorsDict.Add(hoverRoot, new Tuple<Color, Color>(backgroundColor, hoverBackgroundColor));

            hoverRoot.RegisterCallback<PointerEnterEvent>(evt =>
            {
                Color color = ElementsBgColorsDict[hoverRoot].Item2;
                color.a = root.resolvedStyle.backgroundColor.a;
                root.style.backgroundColor = color;
            });
            hoverRoot.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                Color color = ElementsBgColorsDict[hoverRoot].Item1;
                color.a = root.resolvedStyle.backgroundColor.a;
                root.style.backgroundColor = color;
            });
        }
        else
        {
            ElementsBgColorsDict[hoverRoot] = new Tuple<Color, Color>(backgroundColor, hoverBackgroundColor);
        }
    }

    public static void ApplyFontColorForElements(VisualElement root, string[] names, string[] classes, Color fontColor)
    {
        if (names == null)
        {
            root.Query(null, classes).ForEach(element => element.style.color = fontColor);
            return;
        }

        foreach (string name in names)
        {
            root.Query(name, classes).ForEach(element => element.style.color = fontColor);
        }
    }

    public static Color ColorHSVOffset(Color inputColor, float hueOffset, float saturationOffset, float valueOffset)
    {
        float h, s, v;
        Color.RGBToHSV(inputColor, out h, out s, out v);
        h += hueOffset;
        s += saturationOffset;
        v += valueOffset;
        return Color.HSVToRGB(h, s, v);
    }
}
