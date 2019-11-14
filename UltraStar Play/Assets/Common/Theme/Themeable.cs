using System.Xml.Linq;
using UnityEngine;

abstract public class Themeable : MonoBehaviour
{
    abstract public void ReloadResources(Theme theme);

    /// Looks for the color with the given name in the current theme and all parent themes.
    /// Returns true iff the color was found.
    protected bool TryLoadColorFromTheme(Theme theme, string colorsResource, string colorName, out Color resultColor)
    {
        TextAsset textAsset = LoadResourceFromTheme<TextAsset>(theme, colorsResource);
        if (textAsset != null)
        {
            XElement xcolors = XElement.Parse(textAsset.text);
            foreach (XElement xcolor in xcolors.Elements("color"))
            {
                string loadedColorName = xcolor.Attribute("name").String();
                if (loadedColorName == colorName)
                {
                    string hexColorValue = xcolor.Value;
                    return ColorUtility.TryParseHtmlString("#" + hexColorValue, out resultColor);
                }
            }
        }
        resultColor = Color.black;
        return false;
    }

    /// Looks for the resource with the given name in the specified theme and all parent themes.
    /// Returns null if the resource was not found. Otherwise the loaded resource is returned.
    protected T LoadResourceFromTheme<T>(Theme theme, string resourceName) where T : UnityEngine.Object
    {
        if (theme == null)
        {
            return null;
        }

        string path = theme.Name + "/" + resourceName;
        T asset = Resources.Load<T>(path);
        if (asset != null)
        {
            return asset;
        }
        return LoadResourceFromTheme<T>(theme.ParentTheme, resourceName);
    }
}
