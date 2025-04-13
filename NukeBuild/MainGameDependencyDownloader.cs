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
        DownloadSpleeterSharp();
        DownloadUnityStandaloneFileBrowser();

        await DownloadSoundfontsAsync();
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

    private async Task DownloadSoundfontsAsync()
    {
        var targetDir = unityProjectDir / "Assets" / "Plugins" / "Soundfonts";
        Console.WriteLine($"Downloading Soundfonts: TargetDir='{targetDir}'");

        DirectoryUtils.DeleteDirectory(targetDir);
        DirectoryUtils.CreateDirectory(targetDir);
        var targetFile = targetDir / "MuseScore_General.sf2.bytes";

        using (var client = new HttpClient())
        {
            using (var response = await client.GetAsync("https://ftp.osuosl.org/pub/musescore/soundfont/MuseScore_General/MuseScore_General.sf2"))
            {
                response.EnsureSuccessStatusCode();
                await using (var fileStream = File.Create(targetFile))
                {
                    await response.Content.CopyToAsync(fileStream);
                }
            }
        }
        Console.WriteLine("Downloading Soundfonts done");
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

    private void DownloadSpleeterSharp()
    {
        var downloader = new GitDownloader
        {
            RemoteUrl = "https://github.com/achimmihca/SpleeterSharp",
            CommitHash = "3949952a7eef90c31c86eeb0f59fb103abd4138f",
            Branch = "anst/SpleeterMsvcExe",
            TargetDir = unityProjectDir / "Assets" / "Plugins" / "SpleeterSharp",
            Depth = cloneDepth,
            SparseCheckoutPatterns =
            {
                "Source/SpleeterSharp/*"
            },
            MovePostprocess =
            {
                { "Source/SpleeterSharp/*", "." }
            },
            DeletePostprocess =
            {
                "Source",
                ".git"
            },
        };
        downloader.Download();

        AsmdefFileUtils.Create(downloader.TargetDir / "SpleeterSharp.asmdef");
    }

    private void DownloadUnityStandaloneFileBrowser()
    {
        var downloader = new GitDownloader
        {
            RemoteUrl = "https://github.com/achimmihca/UnityStandaloneFileBrowser.git",
            CommitHash = "e75bcc71eb721b8979de6f4653fb507b5dbc2172",
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
