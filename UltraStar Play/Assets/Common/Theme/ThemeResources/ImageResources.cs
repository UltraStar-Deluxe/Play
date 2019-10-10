using System;

public static class ImageResources
{

    public static string GetPath(this EImageResource eImageResource)
    {
        switch (eImageResource)
        {
            case EImageResource.DEMO_SMILEY1: return "themedemo/smiley1";
            case EImageResource.DEMO_SMILEY2: return "themedemo/smiley2";
            case EImageResource.DEMO_SMILEY3: return "themedemo/smiley3";
            default:
                throw new ArgumentException("No image resource path defined for enum value " + eImageResource.ToString());
        }
    }
}
