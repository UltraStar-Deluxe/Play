#if UNITY_2018_1_OR_NEWER
#define UNITY_SUPPORTS_BUILD_REPORT
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEditor.Build;
#if UNITY_SUPPORTS_BUILD_REPORT
using UnityEditor.Build.Reporting;
#endif

namespace Videolabs.VLCUnity.Editor
{
    public class PreProcessBuild :
#if UNITY_SUPPORTS_BUILD_REPORT
        IPreprocessBuildWithReport
#else
        IPreprocessBuild
#endif
    {
        public int callbackOrder { get { return 0; } }

        const string AndroidVulkanErrorMessage = "The Vulkan graphics API is not supported by the VLC Unity plugin." +
        "\n\nPlease go to Player Settings > Android > Auto Graphics API and remove Vulkan from the list." +
        "\nOnly OpenGL ES 2.0 and 3.0 are currently supported on Android.";

#if UNITY_SUPPORTS_BUILD_REPORT
        public void OnPreprocessBuild(BuildReport report)
        {
            OnPreprocessBuild(report.summary.platform, report.summary.outputPath);
        }
#endif

        public void OnPreprocessBuild(BuildTarget target, string path)
        {
            if(target != BuildTarget.Android) return;

            if(IsVulkanConfigured)
            {
                ShowAbortDialog(AndroidVulkanErrorMessage);
            }
        }

        static void ShowAbortDialog(string message)
        {
            if (!EditorUtility.DisplayDialog("Continue Build?", message, "Continue", "Cancel"))
            {
                throw new BuildFailedException(message);
            }
        }

        static bool IsVulkanConfigured => GetGraphicsApiIndex(BuildTarget.Android, GraphicsDeviceType.Vulkan) >= 0;

        static int GetGraphicsApiIndex(BuildTarget target, GraphicsDeviceType api)
        {
            int result = -1;
            GraphicsDeviceType[] devices = UnityEditor.PlayerSettings.GetGraphicsAPIs(target);
            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i] == api)
                {
                    result = i;
                    break;
                }
            }
            return result;
        }
    }
}