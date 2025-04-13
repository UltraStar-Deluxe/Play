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

    Target TestMainGame => _ => _
        .Executes(() =>
        {
            RunUnityTests(mainGameDir, UnityTestPlatform.EditMode);
            RunUnityTests(mainGameDir, UnityTestPlatform.PlayMode);
        });

    Target TestCompanionApp => _ => _
        .Executes(() =>
        {
            RunUnityTests(companionAppDir, UnityTestPlatform.EditMode);
            RunUnityTests(companionAppDir, UnityTestPlatform.PlayMode);
        });

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

    Target BuildCompanionAppSignedAndroidAppBundle => _ => _
        .Executes(() =>
        {
            RunUnityBuildCompanionApp("BuildSignedAndroidAppBundle");
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
            DirectoryUtils.DeleteDirectory(playsharedNuGetPackagesTargetFolder);

            // Download new packages
            DotNet($"build {GetNuGetPackagesProjectFolder(mainGameDir)}");

            // Copy libraries for playshared
            DirectoryUtils.MoveFiles(
                mainGameNuGetPackagesSourceFolder,
                playsharedNuGetPackagesTargetFolder,
                SearchOption.AllDirectories,
                "ICSharpCode.SharpZipLib.dll",
                "JsonNet.ContractResolvers.dll",
                "LiteNetLib.dll",
                "Serilog.dll",
                "Serilog.Sinks.File.dll",
                "System.Diagnostics.DiagnosticSource.dll", // transitive dependency of Serilog
                "System.Threading.Channels.dll", // transitive dependency of Serilog
                "YamlDotNet.dll");

            // Copy libraries for main game
            DirectoryUtils.MoveFiles(
                mainGameNuGetPackagesSourceFolder,
                mainGameNuGetPackagesTargetFolder,
                SearchOption.AllDirectories,
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
            DirectoryUtils.MoveFiles(
                companionAppNuGetPackagesSourceFolder,
                companionAppNuGetPackagesTargetFolder,
                SearchOption.AllDirectories,
                "*.dll");
        });

    Target RestoreCompanionAppDependencies => _ => _
        .DependsOn(RestoreCompanionAppNuGetDependencies)
        .Executes(() => new CompanionAppDependencyDownloader(companionAppDir, cloneDepth).DownloadAsync());

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
        Console.WriteLine($"🧪 Testing Unity project '{unityProjectDir.Name}' ...");

        UnityRunTests(new UnityRunTestsSettings()
            .SetProcessToolPath(unityExecutable)
            .SetBatchMode(true)
            .SetSilentCrashes(true)
            .SetLogFile(buildOutput / $"NukeRunTests-{unityTestPlatform}.log")
            .SetProjectPath(unityProjectDir)
            .SetTestPlatform(unityTestPlatform)
        );
    }

    private void RunUnityBuild(AbsolutePath unityProjectDir, string executeMethod)
    {
        Console.WriteLine($"🚀 Building Unity project '{unityProjectDir.Name}' ...");

        // See https://docs.unity3d.com/Manual/EditorCommandLineArguments.html
        Unity(new UnitySettings()
            .SetProcessToolPath(unityExecutable)
            .SetBatchMode(true)
            .SetSilentCrashes(true)
            .SetLogFile(buildOutput / "NukeBuildCompanionApp.log")
            .SetProjectPath(companionAppDir)
            .SetExecuteMethod(executeMethod)
        );
    }
}
