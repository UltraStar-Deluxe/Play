using System;

[Serializable]
public class DeveloperSettings
{
    public bool showFps;
    public bool disableDynamicThemes;

    /**
     * Require explicit user action to use custom event system
     * because of a Unity issue that can make the UI unusable on Android.
     * (see https://issuetracker.unity3d.com/issues/android-uitoolkit-buttons-cant-be-clicked-with-a-cursor-in-samsung-dex-when-using-eventsystem)
     */
    public bool enableEventSystemOnAndroid;
}
