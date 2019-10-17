using UnityEngine;
using System.Collections;
using System.Xml;
using System.Xml.Linq;

/// Extension methods to simplify working with XElements.
public static class XElementUtil
{

    /// Get an element with a given name or create it if it doesn't exist.
    public static XElement GetOrCreateElement(this XElement xelem,
                                              string name, object content = null)
    {
        if (xelem.Element(name) == null)
        {
            xelem.Add(new XElement(name, content));
        }
        return xelem.Element(name);
    }

    /// Get a string from an element value if it exists. Otherwise use the default value.
	public static string String(this XElement xelem, string defaultValue = "")
    {
        if (xelem == null || xelem.FirstNode == null)
        {
            return defaultValue;
        }
        return xelem.FirstNode.ToString();
    }

    /// Get a boolean from an element value if it exists. Otherwise use the default value.
    public static bool Bool(this XElement xelem, bool defaultValue = false)
    {
        if (xelem == null)
        {
            return defaultValue;
        }
        return bool.Parse(xelem.String());
    }

    /// Get an int from an element value if it exists. Otherwise use the default value.
    public static int Int(this XElement xelem, int defaultValue = 0)
    {
        if (xelem == null)
        {
            return defaultValue;
        }
        return int.Parse(xelem.String());
    }

    /// Get a float from an element value if it exists. Otherwise use the default value.
    public static float Float(this XElement xelem, float defaultValue = 0f)
    {
        if (xelem == null)
        {
            return defaultValue;
        }
        return float.Parse(xelem.String());
    }
}
