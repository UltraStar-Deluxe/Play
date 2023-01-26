using UnityEngine;

public static class ClipboardUtils
{
    public static void CopyToClipboard(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        GUIUtility.systemCopyBuffer = text;
    }
}
