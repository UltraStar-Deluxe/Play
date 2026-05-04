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

    private void DownloadUnityStandaloneFileBrowser()
    {
        var downloader = new GitDownloader
        {
            RemoteUrl = "https://github.com/achimmihca/UnityStandaloneFileBrowser.git",
            CommitHash = "1f4b7ad992221647dcce029ecefe201b12fb079a",
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
