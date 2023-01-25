using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Makes use of https://github.com/yasirkula/UnityIngameDebugConsole
public static class ClipboardUtils
{
#if !UNITY_EDITOR && UNITY_ANDROID
    private static AndroidJavaClass ajc = null;
    private static AndroidJavaClass AJC
    {
        get
        {
            if( ajc == null )
                ajc = new AndroidJavaClass( "com.yasirkula.unity.DebugConsole" );

            return ajc;
        }
    }
#elif !UNITY_EDITOR && UNITY_IOS
    [System.Runtime.InteropServices.DllImport( "__Internal" )]
    private static extern void _DebugConsole_CopyText( string text );
#endif

    public static void CopyToClipboard(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

#if UNITY_EDITOR || UNITY_2018_1_OR_NEWER || ( !UNITY_ANDROID && !UNITY_IOS )
        GUIUtility.systemCopyBuffer = text;
#elif UNITY_ANDROID
			AJC.CallStatic( "CopyText", Context, log );
#elif UNITY_IOS
			_DebugConsole_CopyText( log );
#endif
    }
}
