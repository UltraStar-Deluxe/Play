using UnityEngine;
using System.Collections;
using System.Xml;
using System.Xml.Linq;

/// Extension methods to simplify working with XAttributes.
public static class XAttributeUtil
{
    /// Get a string from an attribute if it exists. Otherwise use the default value.
    public static string String(this XAttribute xatt, string defaultValue = "")
    {
        if (xatt == null)
        {
            return defaultValue;
        }
        return (string)xatt;
    }

    /// Get a boolean from an attribute if it exists. Otherwise use the default value.
    public static bool Bool(this XAttribute xatt, bool defaultValue = false)
    {
        if (xatt == null)
        {
            return defaultValue;
        }
        return (bool)xatt;
    }

    /// Get a boolean from an attribute if it exists. Otherwise use the default value.
    public static int Int(this XAttribute xatt, int defaultValue = 0)
    {
        if (xatt == null)
        {
            return defaultValue;
        }
        return (int)xatt;
    }

    /// Get a float from an attribute if it exists. Otherwise use the default value.
    public static float Float(this XAttribute xatt, float defaultValue = 0f)
    {
        if (xatt == null)
        {
            return defaultValue;
        }
        return (float)xatt;
    }
}
