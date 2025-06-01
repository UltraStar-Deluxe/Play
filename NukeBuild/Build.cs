using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    [Parameter] readonly AbsolutePath unityExecutable;

    [Parameter] readonly UnityTestPlatform testPlatform = UnityTestPlatform.PlayMode;

    private readonly AbsolutePath mainGameDir = RootDirectory / "UltraStar Play";
    private readonly AbsolutePath companionAppDir = RootDirectory / "UltraStar Play Companion";

    public static int Main() => Execute<Build>(x => x.BuildMainGameWindows64);

    public enum UnityProject
    {
        MainGame,
        CompanionApp,
    }

    Target TestMainGame => _ => _
        .Executes(() =>
        {
            TestUnityProject(UnityProject.MainGame, testPlatform);
        });

    Target TestCompanionApp => _ => _
        .Executes(() =>
        {
            TestUnityProject(UnityProject.CompanionApp, testPlatform);
        });

    Target BuildMainGameWindows64 => _ => _
        .Executes(() =>
        {
            BuildMainGame("BuildWindows64");
        });

    Target BuildMainGameLinux64 => _ => _
        .Executes(() =>
        {
            BuildMainGame("BuildLinux64");
        });

    Target BuildMainGameMacOS => _ => _
        .Executes(() =>
        {
            BuildMainGame("BuildMacOS");
        });

    Target BuildCompanionAppAndroidApk => _ => _
        .Executes(() =>
        {
            BuildCompanionApp("BuildAndroidApk");
        });

    Target BuildAndRunCompanionAppAndroidApk => _ => _
        .Executes(() =>
        {
            BuildCompanionApp("BuildAndRunAndroidApk");
        });

    Target BuildCompanionAppSignedAndroidAppBundle => _ => _
        .Executes(() =>
        {
            BuildCompanionApp("BuildSignedAndroidAppBundle");
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

            // Move libraries for playshared
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
                "System.Runtime.CompilerServices.Unsafe.dll", // transitive dependency of Serilog
                "System.Threading.Channels.dll", // transitive dependency of Serilog
                "YamlDotNet.dll");

            // Move libraries for main game
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

    private void TestUnityProject(UnityProject unityProject, UnityTestPlatform unityTestPlatform)
    {
        AbsolutePath unityProjectDir = GetUnityProjectDir(unityProject);

        Console.WriteLine($"🧪 Testing Unity project '{unityProjectDir.Name}' ...");

        UnityRunTests(new UnityRunTestsSettings()
            .SetProcessToolPath(GetUnityExecutableOrDefault(unityExecutable, unityProjectDir))
            .SetBatchMode(true)
            .SetSilentCrashes(true)
            .SetLogFile(buildOutput / $"Nuke-Test-{unityProject}-{unityTestPlatform}.log")
            .SetTestResultFile(buildOutput / $"Nuke-TestResults-{unityProject}-{unityTestPlatform}.xml")
            .SetProjectPath(unityProjectDir)
            .SetTestPlatform(unityTestPlatform)
            .SetStableExitCodes(0, 2)
        );
    }

    private void BuildMainGame(string methodName)
    {
        BuildUnityProject(UnityProject.MainGame, $"MainGameBuildTools.{methodName}");
    }

    private void BuildCompanionApp(string methodName)
    {
        BuildUnityProject(UnityProject.CompanionApp, $"CompanionAppBuildTools.{methodName}");
    }

    private void BuildUnityProject(UnityProject unityProject, string executeMethod)
    {
        AbsolutePath unityProjectDir = GetUnityProjectDir(unityProject);

        Console.WriteLine($"🚀 Building Unity project '{unityProjectDir.Name}' ...");

        // See https://docs.unity3d.com/Manual/EditorCommandLineArguments.html
        Unity(new UnitySettings()
            .SetProcessToolPath(GetUnityExecutableOrDefault(unityExecutable, unityProjectDir))
            .SetBatchMode(true)
            .SetSilentCrashes(true)
            .SetLogFile(buildOutput / $"Nuke-Build-{unityProject}-{executeMethod}.log")
            .SetProjectPath(unityProjectDir)
            .SetExecuteMethod(executeMethod)
        );
    }

    private AbsolutePath GetUnityProjectDir(UnityProject unityProject)
    {
        return unityProject is UnityProject.MainGame ? mainGameDir : companionAppDir;
    }

    private string GetUnityExecutableOrDefault(AbsolutePath providedUnityExecutable, AbsolutePath unityProjectDir)
    {
        if (!string.IsNullOrEmpty(providedUnityExecutable))
        {
            return providedUnityExecutable;
        }

        string unityVersion = UnityUtils.GetUnityVersion(unityProjectDir);
        Console.WriteLine($"Unity project '{unityProjectDir.Name}' is using Unity version {unityVersion}");

        return UnityUtils.GetUnityExecutable(unityVersion);
    }
}
