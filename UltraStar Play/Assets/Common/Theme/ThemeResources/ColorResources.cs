using System;

public static class ColorResources
{
    public static string GetPath(this EColorResource eColorResource)
    {
        switch (eColorResource)
        {
            case EColorResource.DEMO_COLOR1: return "themedemo/colors";
            case EColorResource.DEMO_COLOR2: return "themedemo/colors";
            default:
                throw new ArgumentException("No color resource path defined for enum value " + eColorResource.ToString());
        }
    }

    public static string GetName(this EColorResource eColorResource)
    {
        switch (eColorResource)
        {
            case EColorResource.DEMO_COLOR1: return "demo_color1";
            case EColorResource.DEMO_COLOR2: return "demo_color2";
            default:
                throw new ArgumentException("No color name defined for enum value " + eColorResource.ToString());
        }
    }
}
