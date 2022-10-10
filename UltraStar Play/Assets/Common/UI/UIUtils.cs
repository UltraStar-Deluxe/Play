using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public static class UIUtils
{
    // Returns a nicely readable string from a filename, e.g. "theme_blue" will become "Theme Blue"
    public static string BeautifyString(string input)
    {
        input = input.Replace("_", " ").Replace("-", " ");
        char[] chars = input.ToCharArray();
        bool lastWasSpace = true;
        for (int c = 0; c < chars.Length; c++)
        {
            if (lastWasSpace) chars[c] = char.ToUpperInvariant(input[c]);
            lastWasSpace = char.IsWhiteSpace(chars[c]);
        }
        return new string(chars);
    }

    public static void ForEachElementWithClass(VisualElement root, Action<VisualElement> callback, params string[] classNames)
    {
        foreach (string className in classNames)
        {
            root.Query(null, className).ForEach(callback);
        }
    }

    static readonly Dictionary<VisualElement, Tuple<Color, Color>> elementsBgColorsDict = new ();
    public static void SetBackgroundStyleWithHover(VisualElement root, Color backgroundColor, Color hoverBackgroundColor, Color fontColor)
    {
        if (root == null) return;

        root.style.color = fontColor;
        root.style.backgroundColor = backgroundColor;

        // We can't access pseudo states through the API (e.g. :hover), so we have to manually mimic them
        if (!elementsBgColorsDict.ContainsKey(root))
        {
            elementsBgColorsDict.Add(root, new Tuple<Color, Color>(backgroundColor, hoverBackgroundColor));

            root.RegisterCallback<PointerEnterEvent>(evt =>
            {
                Color color = elementsBgColorsDict[root].Item2;
                color.a = root.resolvedStyle.backgroundColor.a;
                root.style.backgroundColor = color;
            });
            root.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                Color color = elementsBgColorsDict[root].Item1;
                color.a = root.resolvedStyle.backgroundColor.a;
                root.style.backgroundColor = color;
            });
        }
        else
        {
            elementsBgColorsDict[root] = new Tuple<Color, Color>(backgroundColor, hoverBackgroundColor);
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
