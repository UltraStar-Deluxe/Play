using System;
using System.IO;
using System.Threading.Tasks;
using Nuke.Common.IO;

namespace DefaultNamespace;

public abstract class BaseDependencyDownloader(
    AbsolutePath unityProjectDir,
    uint cloneDepth
) {
    protected readonly AbsolutePath unityProjectDir = unityProjectDir;
    protected readonly uint cloneDepth = cloneDepth;

    public virtual async Task DownloadAsync()
    {
        DownloadLeanTween();
        DownloadUniRx();

        CreateVersionTxtFile();
    }

    /**
     * Creates an empty file if it does not exist.
     * Reason is that VERSION.txt is not under version control because it changes frequently when the Unity project is built,
     * but its .meta file is under version control to preserve references to the file.
     */
    private void CreateVersionTxtFile()
    {
        var path = unityProjectDir / "Assets" / "VERSION.txt";
        if (!File.Exists(path))
        {
            Console.WriteLine("Create empty VERSION.txt file");
            File.WriteAllText(path, "");
        }
    }

    private void DownloadLeanTween()
    {
        var downloader = new GitDownloader
        {
            RemoteUrl = "https://github.com/UltraStar-Deluxe/LeanTween.git",
            CommitHash = "ea745c3f94d8682327c912030dfc6b65cbe1ced5",
            Branch = "master",
            TargetDir = unityProjectDir / "Assets" / "Plugins" / "LeanTween",
            Depth = cloneDepth,
            SparseCheckoutPatterns =
            {
                "Assets/LeanTween/Framework/*",
                "Assets/LeanTween/Editor/*",
                "Assets/LeanTween/Documentation/*",
                "Assets/LeanTween/License.txt",
                "Assets/LeanTween/ReadMe.txt"
            },
            MovePostprocess =
            {
                { "Assets/LeanTween/*", "." }
            },
            DeletePostprocess =
            {
                "Assets",
                ".git"
            }
        };
        downloader.Download();
    }

    private void DownloadUniRx()
    {
        // Download directly from GitHub because
        // - Cannot use the NuGet version of UniRx because it is outdated
        // - Cannot use prepared Unity package from GitHub because Unity dependency resolution inside a package (inside playshared) does not work with GitHub repositories
        var downloader = new GitDownloader
        {
            RemoteUrl = "https://github.com/neuecc/UniRx.git",
            CommitHash = "66205df49631860dd8f7c3314cb518b54c944d30",
            Branch = "master",
            TargetDir = unityProjectDir / "Assets" / "Plugins" / "UniRx",
            Depth = cloneDepth,
            SparseCheckoutPatterns =
            {
                "Assets/Plugins/UniRx/Scripts/*",
                "Assets/Plugins/UniRx/ReadMe.txt"
            },
            MovePostprocess =
            {
                { "Assets/Plugins/UniRx/*", "." }
            },
            DeletePostprocess =
            {
                "Assets",
                ".git"
            }

        };
        downloader.Download();
    }
}
