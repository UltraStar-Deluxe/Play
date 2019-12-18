using UnityEditor;
using System.Linq;
using System;
using System.IO;
using UnityEngine;

static class BuildCommand
{
    private const string KEYSTORE_PASS  = "KEYSTORE_PASS";
    private const string KEY_ALIAS_PASS = "KEY_ALIAS_PASS";
    private const string KEY_ALIAS_NAME = "KEY_ALIAS_NAME";
    private const string KEYSTORE       = "keystore.keystore";
    private const string BUILD_OPTIONS_ENV_VAR = "BuildOptions";

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
            where !string.IsNullOrEmpty(scene.path)
            select scene.path
        ).ToArray();
    }

    static BuildTarget GetBuildTarget()
    {
        string buildTargetName = GetArgument("customBuildTarget");
        Console.WriteLine(":: Received customBuildTarget " + buildTargetName);

        if (buildTargetName.TryConvertToEnum(out BuildTarget target))
            return target;

        Console.WriteLine($":: {nameof(buildTargetName)} \"{buildTargetName}\" not defined on enum {nameof(BuildTarget)}, using {nameof(BuildTarget.NoTarget)} enum to build");

        return BuildTarget.NoTarget;
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

    static string GetFixedBuildPath(BuildTarget buildTarget, string buildPath, string buildName, BuildOptions buildOptions)
    {
        if (buildTarget.ToString().ToLower().Contains("windows")) {
            buildName += ".exe";
        } else if (buildTarget == BuildTarget.Android && buildOptions == BuildOptions.None) {
            buildName += ".apk";
        }
        return buildPath + buildName;
    }

    static BuildOptions GetBuildOptions()
    {
        if (TryGetEnv(BUILD_OPTIONS_ENV_VAR, out string envVar)) {
            string[] allOptionVars = envVar.Split(',');
            BuildOptions allOptions = BuildOptions.None;
            BuildOptions option;
            string optionVar;
            int length = allOptionVars.Length;

            Console.WriteLine($":: Detecting {BUILD_OPTIONS_ENV_VAR} env var with {length} elements ({envVar})");

            for (int i = 0; i < length; i++) {
                optionVar = allOptionVars[i];

                if (optionVar.TryConvertToEnum(out option)) {
                    allOptions |= option;
                }
                else {
                    Console.WriteLine($":: Cannot convert {optionVar} to {nameof(BuildOptions)} enum, skipping it.");
                }
            }

            return allOptions;
        }

        return BuildOptions.None;
    }

    // https://stackoverflow.com/questions/1082532/how-to-tryparse-for-enum-value
    static bool TryConvertToEnum<TEnum>(this string strEnumValue, out TEnum value)
    {
        if (!Enum.IsDefined(typeof(TEnum), strEnumValue))
        {
            value = default;
            return false;
        }

        value = (TEnum)Enum.Parse(typeof(TEnum), strEnumValue);
        return true;
    }

    static bool TryGetEnv(string key, out string value)
    {
        value = Environment.GetEnvironmentVariable(key);
        return !string.IsNullOrEmpty(value);
    }

    static void PerformBuild()
    {
        Console.WriteLine(":: Performing build");

        var buildTarget = GetBuildTarget();

        if (buildTarget == BuildTarget.Android) {
            HandleAndroidKeystore();
        }

        var buildPath      = GetBuildPath();
        var buildName      = GetBuildName();
        var buildOptions   = GetBuildOptions();
        var fixedBuildPath = GetFixedBuildPath(buildTarget, buildPath, buildName, buildOptions);

        BuildPipeline.BuildPlayer(GetEnabledScenes(), fixedBuildPath, buildTarget, buildOptions);
        Console.WriteLine(":: Done with build");
    }

    private static void HandleAndroidKeystore()
    {
#if UNITY_2019_1_OR_NEWER
        PlayerSettings.Android.useCustomKeystore = false;
#endif

        if (!File.Exists(KEYSTORE)) {
            Console.WriteLine($":: {KEYSTORE} not found, skipping setup, using Unity's default keystore");
            return;    
        }

        PlayerSettings.Android.keystoreName = KEYSTORE;

        string keystorePass;
        string keystoreAliasPass;

        if (TryGetEnv(KEY_ALIAS_NAME, out string keyaliasName)) {
            PlayerSettings.Android.keyaliasName = keyaliasName;
            Console.WriteLine($":: using ${KEY_ALIAS_NAME} env var on PlayerSettings");
        } else {
            Console.WriteLine($":: ${KEY_ALIAS_NAME} env var not set, using Project's PlayerSettings");
        }

        if (!TryGetEnv(KEYSTORE_PASS, out keystorePass)) {
            Console.WriteLine($":: ${KEYSTORE_PASS} env var not set, skipping setup, using Unity's default keystore");
            return;
        }

        if (!TryGetEnv(KEY_ALIAS_PASS, out keystoreAliasPass)) {
            Console.WriteLine($":: ${KEY_ALIAS_PASS} env var not set, skipping setup, using Unity's default keystore");
            return;
        }
#if UNITY_2019_1_OR_NEWER
        PlayerSettings.Android.useCustomKeystore = true;
#endif
        PlayerSettings.Android.keystorePass = keystorePass;
        PlayerSettings.Android.keyaliasPass = keystoreAliasPass;
    }
}
