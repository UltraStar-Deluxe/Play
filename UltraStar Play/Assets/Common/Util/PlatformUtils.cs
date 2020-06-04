using UnityEngine;
using System.Collections;

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
}
