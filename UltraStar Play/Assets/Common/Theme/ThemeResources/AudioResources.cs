using System;

public static class AudioResources
{

    public static string GetPath(this EAudioResource eAudioResource)
    {
        switch (eAudioResource)
        {
            case EAudioResource.DEMO_BUTTON_CLICK1: return "themedemo/button_click1";
            default:
                throw new ArgumentException("No audio resource path defined for enum value " + eAudioResource.ToString());
        }
    }
}
