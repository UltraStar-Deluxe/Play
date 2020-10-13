using System;

public static class ImageResources
{

    public static string GetPath(this EImageResource eImageResource)
    {
        switch (eImageResource)
        {
            case EImageResource.DEMO_SMILEY1: return "Smiley1";
            case EImageResource.DEMO_SMILEY2: return "Smiley2";
            case EImageResource.DEMO_SMILEY3: return "Smiley3";
            default:
                throw new ArgumentException("No image resource path defined for enum value " + eImageResource.ToString());
        }
    }
}
