using System;
using System.Collections.Generic;
using System.IO;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Unity;
using static Nuke.Common.Tools.Unity.UnityTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace DefaultNamespace;

class Build : NukeBuild
{
    [Parameter] readonly uint cloneDepth = 1000;
    [Parameter] readonly AbsolutePath buildOutput = RootDirectory / "Build";
    [Parameter] readonly AbsolutePath unityExecutable = "C:/Program Files/Unity/Hub/Editor/2023.2.12f1/Editor/Unity.exe";

    private readonly AbsolutePath mainGameDir = RootDirectory / "UltraStar Play";
    private readonly AbsolutePath companionAppDir = RootDirectory / "UltraStar Play Companion";

    public static int Main() => Execute<Build>(x => x.BuildMainGameWindows64);

    Target RunMainGameTests => _ => _
        .Executes(() =>
        {
            RunUnityTests(mainGameDir, UnityTestPlatform.EditMode);
            RunUnityTests(mainGameDir, UnityTestPlatform.PlayMode);
        });

    Target RunCompanionAppTests => _ => _
        .Executes(() =>
        {
            RunUnityTests(companionAppDir, UnityTestPlatform.EditMode);
            RunUnityTests(companionAppDir, UnityTestPlatform.PlayMode);
        });

    Target RestoreMainGameNuGetDependencies => _ => _
        .Executes(() =>
        {
            AbsolutePath mainGameNuGetPackagesSourceFolder = GetNuGetPackagesProjectFolder(mainGameDir) / "bin";
            AbsolutePath mainGameNuGetPackagesTargetFolder = GetNuGetPackagesTargetFolder(mainGameDir);
            AbsolutePath playsharedNuGetPackagesTargetFolder = mainGameDir / "Packages" / "playshared" / "Runtime" / "Plugins" / "NuGetPackages";

            // Delete old packages
            DirectoryUtils.DeleteDirectory(mainGameNuGetPackagesSourceFolder);
            DirectoryUtils.DeleteDirectory(mainGameNuGetPackagesTargetFolder);

            // Download new packages
            DotNet($"build {GetNuGetPackagesProjectFolder(mainGameDir)}");

            // Copy libraries for playshared
            CopyFiles(
                mainGameNuGetPackagesSourceFolder,
                playsharedNuGetPackagesTargetFolder,
                "YamlDotNet.dll",
                "ICSharpCode.SharpZipLib.dll",
                "Serilog.dll",
                "Serilog.Sinks.File.dll");

            // Copy libraries for main game
            CopyFiles(
                mainGameNuGetPackagesSourceFolder,
                mainGameNuGetPackagesTargetFolder,
                "*.dll");
        });

    Target RestoreMainGameDependencies => _ => _
        .DependsOn(RestoreMainGameNuGetDependencies)
        .Executes(() => new MainGameDependencyDownloader(mainGameDir, cloneDepth).DownloadAsync());

    Target RestoreCompanionAppNuGetDependencies => _ => _
        .DependsOn(RestoreMainGameNuGetDependencies) // Restore main game dependencies for playshared
        .Executes(() =>
        {
            AbsolutePath companionAppNuGetPackagesSourceFolder = GetNuGetPackagesProjectFolder(companionAppDir) / "bin";
            AbsolutePath companionAppNuGetPackagesTargetFolder = GetNuGetPackagesTargetFolder(companionAppDir);

            // Delete old packages
            DirectoryUtils.DeleteDirectory(companionAppNuGetPackagesSourceFolder);
            DirectoryUtils.DeleteDirectory(companionAppNuGetPackagesTargetFolder);

            // Download new packages
            DotNet($"build {GetNuGetPackagesProjectFolder(companionAppDir)}");

            // Copy libraries for companion app
            CopyFiles(
                companionAppNuGetPackagesSourceFolder,
                companionAppNuGetPackagesTargetFolder,
                "*.dll");
        });

    Target RestoreCompanionAppDependencies => _ => _
        .DependsOn(RestoreCompanionAppNuGetDependencies)
        .Executes(() => new CompanionAppDependencyDownloader(companionAppDir, cloneDepth).DownloadAsync());

    Target BuildMainGameWindows64 => _ => _
        .Executes(() =>
        {
            RunUnityBuildMainGame("BuildWindows64");
        });

    Target BuildCompanionAppAndroidApk => _ => _
        .Executes(() =>
        {
            RunUnityBuildCompanionApp("BuildAndroidApk");
        });

    Target BuildAndRunCompanionAppAndroidApk => _ => _
        .Executes(() =>
        {
            RunUnityBuildCompanionApp("BuildAndRunAndroidApk");
        });

    Target BuildCompanionAppSignedAndroidApk => _ => _
        .Executes(() =>
        {
            RunUnityBuildCompanionApp("BuildSignedAndroidApk");
        });

    private AbsolutePath GetNuGetPackagesProjectFolder(AbsolutePath unityProjectDir)
    {
        return unityProjectDir / "Packages" / "NuGetPackages";
    }

    private AbsolutePath GetNuGetPackagesTargetFolder(AbsolutePath unityProjectDir)
    {
        return unityProjectDir / "Assets" / "Plugins" / "NuGetPackages";
    }

    void RunUnityBuildMainGame(string methodName)
    {
        RunUnityBuild(mainGameDir, $"MainGameBuildTools.{methodName}");
    }

    void RunUnityBuildCompanionApp(string methodName)
    {
        RunUnityBuild(companionAppDir, $"CompanionAppBuildTools.{methodName}");
    }

    private void RunUnityTests(AbsolutePath unityProjectDir, UnityTestPlatform unityTestPlatform)
    {
        Console.WriteLine($"🚀 Running Unity tests of '{unityProjectDir.Name}'...");

        UnityRunTests(s => s
            .SetProcessToolPath(unityExecutable)
            .SetProjectPath(unityProjectDir)
            .SetBatchMode(true)
            .SetQuit(true)
            .SetTestPlatform(unityTestPlatform)
            .SetLogFile(buildOutput / $"NukeRunTests-{unityTestPlatform}.log")
        );
    }

    private void RunUnityBuild(AbsolutePath unityProjectDir, string executeMethod)
    {
        Console.WriteLine($"🚀 Building Unity project '{unityProjectDir.Name}' ...");

        Unity(new UnitySettings()
                .SetProjectPath(companionAppDir)
                .SetBatchMode(true)
                .SetQuit(true)
                .SetExecuteMethod(executeMethod)
                .SetLogFile(buildOutput / "NukeBuildCompanionApp.log")
        );
    }

    private void CopyFiles(AbsolutePath source, AbsolutePath destination, params string[] fileNamePatterns)
    {
        Directory.CreateDirectory(destination);

        foreach (var pattern in fileNamePatterns)
        {
            // Get files matching the current pattern
            foreach (var file in Directory.GetFiles(source, pattern, SearchOption.AllDirectories))
            {
                Console.WriteLine($"Copying file: Source='{file}', Target='{destination}'");
                File.Copy(file, $"{destination}/{Path.GetFileName(file)}", true);
            }
        }
    }
}
