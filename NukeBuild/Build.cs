using System;
using Nuke.Common;
using Nuke.Common.Tools.Unity;
using static Nuke.Common.Tools.Unity.UnityTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
    // Define Environment Variables
    [Parameter] readonly string buildOutput = "../Builds";

    [Parameter] readonly string packagesConfig = "./Packages/packages.config";
    [Parameter] readonly string nuGetPackagesDir = "./Packages/NuGet/";

    Target RestoreNuGet => _ => _
        .Executes(() =>
        {
            Console.WriteLine("🔄 Running dotnet restore with packages.config...");

            DotNet($"restore {packagesConfig} --packages {nuGetPackagesDir}");
        });

    Target BuildUnity => _ => _
        .Executes(() =>
        {
            Console.WriteLine("🚀 Building Unity project...");
            Unity(new UnitySettings()
                // .SetProcessToolPath(UNITY_PATH) // Unity editor executable path
                .SetProjectPath(RootDirectory) // Path to Unity project
                .SetBatchMode(true) // Run in batch mode
                .SetQuit(true) // Quit Unity after build
                .SetExecuteMethod("MainGameBuildTools.BuildWindows64") // Specify the build method to execute
                .SetLogFile(RootDirectory / buildOutput / "nuke_unity_build.log") // Log file for Unity build
            );
        });

    public static int Main() => Execute<Build>(x => x.BuildUnity);
}
