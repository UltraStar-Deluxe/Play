public static class PlatformUtils
{
    public static bool IsStandalone
    {
        get
        {
#if UNITY_STANDALONE
            return true;
#else
            return false;
#endif
        }
    }

    public static bool IsAndroid
    {
        get
        {
#if UNITY_ANDROID
            return true;
#else
            return false;
#endif
        }
    }

    public static bool IsWindows
    {
        get
        {
#if UNITY_STANDALONE_WIN
            return true;
#else
            return false;
#endif
        }
    }
}
