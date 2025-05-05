using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Nuke.Common.IO;

namespace DefaultNamespace;

public class MainGameDependencyDownloader(
    AbsolutePath unityProjectDir,
    uint cloneDepth
) : BaseDependencyDownloader(unityProjectDir, cloneDepth) {

    public override async Task DownloadAsync()
    {
        await base.DownloadAsync();

        DownloadCSharpSynthForUnity();
        DownloadUnityStandaloneFileBrowser();

        await DownloadSpleeterMsvcExeAsync();
    }

    private void DownloadCSharpSynthForUnity()
    {
        var downloader = new GitDownloader
        {
            RemoteUrl = "https://github.com/KNCarnage/CSharpSynthForUnity2.0.git",
            CommitHash = "806bce0820d2611f804066e06e1cd3842439addd",
            TargetDir = unityProjectDir / "Assets" / "Plugins" / "CSharpSynthForUnity",
            Depth = cloneDepth,
            SparseCheckoutPatterns =
            {
                "Assets/ThirdParty/*"
            },
            MovePostprocess =
            {
                { "Assets/*", "." }
            },
            DeletePostprocess =
            {
                "Assets",
                ".git"
            }
        };

        downloader.Download();
    }

    private async Task DownloadSpleeterMsvcExeAsync()
    {
        var targetDir = unityProjectDir / "Assets" / "StreamingAssets" / "SpleeterMsvcExe";
        Console.WriteLine($"Downloading SpleeterMsvcExe: TargetDir='{targetDir}'");

        DirectoryUtils.DeleteDirectory(targetDir);
        DirectoryUtils.CreateDirectory(targetDir);
        var zipFile = targetDir / "SpleeterMsvcExe.zip";

        using (var client = new HttpClient())
        {
            using (var response = await client.GetAsync("https://github.com/achimmihca/SpleeterMsvcExe/releases/download/v1.0/SpleeterMsvcExe-v1.0-2stems-only.zip"))
            {
                response.EnsureSuccessStatusCode();
                await using (var fileStream = File.Create(zipFile))
                {
                    await response.Content.CopyToAsync(fileStream);
                }
            }
        }

        ZipFile.ExtractToDirectory(zipFile, targetDir);
        File.Delete(zipFile);

        Console.WriteLine("Downloading SpleeterMsvcExe done");
    }

    private void DownloadUnityStandaloneFileBrowser()
    {
        var downloader = new GitDownloader
        {
            RemoteUrl = "https://github.com/gkngkc/UnityStandaloneFileBrowser.git",
            CommitHash = "04a5d49ed2545556da8a7192e86c69bd47641f10",
            Branch = "master",
            TargetDir = unityProjectDir / "Assets" / "Plugins" / "UnityStandaloneFileBrowser",
            Depth = cloneDepth,
            SparseCheckoutPatterns = { "Assets/*" },
            MovePostprocess = { { "Assets/*", "." } },
            DeletePostprocess = { "Assets", ".git" },
        };
        downloader.Download();

        AsmdefFileUtils.Create(downloader.TargetDir / "UnityStandaloneFileBrowser.asmdef");
    }
}
