using UnityEditor;
using System.Linq;
using System;
using System.Globalization;
using UnityEngine;

static class BuildCommand
{
    static string GetArgument(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Contains(name))
            {
                return args[i + 1];
            }
        }
        return null;
    }

    static string[] GetEnabledScenes()
    {
        return (
            from scene in EditorBuildSettings.scenes
            where scene.enabled
            select scene.path
        ).ToArray();
    }

    static BuildTarget GetBuildTarget()
    {
        string buildTargetName = GetArgument("customBuildTarget");
        Console.WriteLine(":: Received customBuildTarget " + buildTargetName);

        if (buildTargetName.ToLower(CultureInfo.InvariantCulture) == "android")
        {
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Internal;
        }

        return ToEnum<BuildTarget>(buildTargetName, BuildTarget.NoTarget);
    }

    static string GetBuildPath()
    {
        string buildPath = GetArgument("customBuildPath");
        Console.WriteLine(":: Received customBuildPath " + buildPath);
        if (buildPath == "")
        {
            throw new UnityException("customBuildPath argument is missing");
        }
        return buildPath;
    }

    static string GetBuildName()
    {
        string buildName = GetArgument("customBuildName");
        Console.WriteLine(":: Received customBuildName " + buildName);
        if (buildName == "")
        {
            throw new UnityException("customBuildName argument is missing");
        }
        return buildName;
    }

    static string GetFixedBuildPath(BuildTarget buildTarget, string buildPath, string buildName)
    {
        string resultName = "";
        if (buildTarget.ToString().ToLower().Contains("windows"))
        {
            resultName = buildName + ".exe";
        }
        else if (buildTarget.ToString().ToLower().Contains("webgl"))
        {
            // webgl produces a folder with index.html inside, there is no executable name for this buildTarget
            resultName = "";
        }
        return buildPath + resultName;
    }

    static BuildOptions GetBuildOptions()
    {
        string buildOptions = GetArgument("customBuildOptions");
        return buildOptions == "AcceptExternalModificationsToPlayer" ? BuildOptions.AcceptExternalModificationsToPlayer : BuildOptions.None;
    }

    // https://stackoverflow.com/questions/1082532/how-to-tryparse-for-enum-value
    static TEnum ToEnum<TEnum>(this string strEnumValue, TEnum defaultValue)
    {
        if (!Enum.IsDefined(typeof(TEnum), strEnumValue))
        {
            return defaultValue;
        }

        return (TEnum)Enum.Parse(typeof(TEnum), strEnumValue);
    }

    public static void PerformBuild()
    {
        Console.WriteLine(":: Performing build");
        var buildTarget = GetBuildTarget();
        var buildPath = GetBuildPath();
        var buildName = GetBuildName();
        var fixedBuildPath = GetFixedBuildPath(buildTarget, buildPath, buildName);

        BuildPipeline.BuildPlayer(GetEnabledScenes(), fixedBuildPath, buildTarget, GetBuildOptions());
        Console.WriteLine(":: Done with build");
    }
}
