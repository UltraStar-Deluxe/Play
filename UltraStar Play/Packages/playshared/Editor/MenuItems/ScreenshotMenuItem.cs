using System;
using System.IO;
using UniInject;
using UnityEditor;
using UnityEngine;

public class ScreenshotMenuItem
{
    [MenuItem("Tools/Screenshot %j")]
    public static void TakeScreenshot()
    {
        string targetFolder = $"{Application.persistentDataPath}/Screenshots";
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        string timeStamp = DateTime.Now.ToString("dd-MM-yyyy HH-mm-ss");
        string screenshotName = $"{timeStamp}-screenshot.png";
        string targetPath = $"{targetFolder}/{screenshotName}";
        ScreenCapture.CaptureScreenshot(targetPath);
        ApplicationUtils.OpenDirectory(targetFolder);
        Debug.Log($"Saved screenshot: '{targetPath}'");
    }
}
