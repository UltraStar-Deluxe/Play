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

    private void CreateVersionTxtFile()
    {
        var path = unityProjectDir / "Assets" / "VERSION.txt";
        if (!File.Exists(path))
        {
            Console.WriteLine("Create empty VERSION.txt file");
            File.WriteAllText(path, "");
        }
    }

    private void DownloadCompileTimeTracker()
    {
        AbsolutePath targetDir = unityProjectDir / "Assets" / "Plugins" / "CompileTimeTracker";
        GitDownloader downloader = new GitDownloader
        {
            RemoteUrl = "https://github.com/DarrenTsung/DTCompileTimeTracker",
            CommitHash = "276095b3b212d7c33106b53d71b93b5a72d1e1d3",
            Branch = "master",
            TargetDir = targetDir,
            Depth = cloneDepth,
            SparseCheckoutPatterns =
            {
                "CompileTimeTracker/*",
                "README.md"
            },
            MovePostprocess =
            {
                { "CompileTimeTracker/*", "." }
            },
            DeletePostprocess =
            {
                "CompileTimeTracker",
                ".git"
            }
        };
        downloader.Download();

        AsmdefFileUtils.Create(targetDir / "CompileTimeTrackerEditor.asmdef");
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

    private void DownloadSerilog()
    {
        DownloadSerilogCore();
        DownloadSerilogSinksFile();
    }

    private void DownloadSerilogCore()
    {
        var downloader = new GitDownloader
        {
            RemoteUrl = "https://github.com/serilog/serilog.git",
            CommitHash = "655778f74384f682d2c8705ab4883c39ef17e44d",
            TargetDir = unityProjectDir / "Assets" / "Plugins" / "Serilog" / "Serilog",
            Depth = cloneDepth,
            SparseCheckoutPatterns =
            {
                "LICENSE",
                "src/Serilog/*"
            },
            MovePostprocess =
            {
                { "src/Serilog/*", "." }
            },
            DeletePostprocess =
            {
                "src",
                ".git",
                "Properties/AssemblyInfo.cs"
            }
        };
        downloader.Download();
    }

    private void DownloadSerilogSinksFile()
    {
        var downloader = new GitDownloader
        {
            RemoteUrl = "https://github.com/serilog/serilog-sinks-file.git",
            CommitHash = "272085f4c9440e62448b65829ad35cc3dea15ab1",
            TargetDir = unityProjectDir / "Assets" / "Plugins" / "Serilog" / "Serilog.Sinks.File",
            Depth = cloneDepth,
            SparseCheckoutPatterns =
            {
                "LICENSE",
                "src/Serilog.Sinks.File/*"
            },
            MovePostprocess =
            {
                { "src/Serilog.Sinks.File/*", "." }
            },
            DeletePostprocess =
            {
                "src",
                ".git",
                "Properties/AssemblyInfo.cs"
            }
        };
        downloader.Download();
    }

    private void DownloadUniRx()
    {
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
