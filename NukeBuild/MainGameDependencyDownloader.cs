using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Nuke.Common.IO;

namespace DefaultNamespace;

public class MainGameDependencyDownloader(
    AbsolutePath mainGameDir,
    uint cloneDepth
) {
    public async Task DownloadAsync()
    {
        DownloadCompileTimeTracker();
        DownloadCSharpSynthForUnity();
        DownloadDynamicQueryable();
        DownloadJokenizer();
        DownloadJsonNetContractResolvers();
        DownloadLeanTween();
        DownloadLiteNetLib();
        DownloadLrcParser();
        DownloadNHyphenator();
        DownloadSerilog();
        DownloadSerilogCore();
        DownloadSerilogSinksFile();
        DownloadSpleeterSharp();
        DownloadUniRx();
        DownloadUnityStandaloneFileBrowser();

        await DownloadSoundfontsAsync();
        await DownloadSpleeterMsvcExeAsync();
    }

    private void DownloadCompileTimeTracker()
    {
        AbsolutePath targetDir = mainGameDir / "Assets" / "Plugins" / "CompileTimeTracker";
        GitDownloader downloader = new GitDownloader
        {
            RemoteUrl = "https://github.com/DarrenTsung/DTCompileTimeTracker",
            CommitHash = "276095b3b212d7c33106b53d71b93b5a72d1e1d3",
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

    private void DownloadCSharpSynthForUnity()
    {
        var downloader = new GitDownloader
        {
            RemoteUrl = "https://github.com/KNCarnage/CSharpSynthForUnity2.0.git",
            CommitHash = "806bce0820d2611f804066e06e1cd3842439addd",
            TargetDir = mainGameDir / "Assets" / "Plugins" / "CSharpSynthForUnity",
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

    private void DownloadDynamicQueryable()
    {
        var downloader = new GitDownloader
        {
            RemoteUrl = "https://github.com/umutozel/DynamicQueryable.git",
            CommitHash = "c4832d4a69127074699b8d4a6c8d897fff644bdb",
            TargetDir = mainGameDir / "Assets" / "Plugins" / "DynamicQueryable",
            Depth = cloneDepth,
            SparseCheckoutPatterns =
            {
                "src/DynamicQueryable/*"
            },
            MovePostprocess =
            {
                { "src/DynamicQueryable/*", "." }
            },
            DeletePostprocess =
            {
                "src",
                "Properties",
                ".git"
            }
        };

        downloader.Download();
    }

    private void DownloadJokenizer()
    {
        var downloader = new GitDownloader
        {
            RemoteUrl = "https://github.com/umutozel/Jokenizer.Net.git",
            CommitHash = "8fb6452eea609f5de46911be73d982056080e658",
            TargetDir = mainGameDir / "Assets" / "Plugins" / "Jokenizer.Net",
            Depth = cloneDepth,
            SparseCheckoutPatterns =
            {
                "src/Jokenizer.Net/*"
            },
            MovePostprocess =
            {
                { "src/Jokenizer.Net/*", "." }
            },
            DeletePostprocess =
            {
                "src",
                "Properties",
                ".git"
            }
        };
        downloader.Download();
    }

    private void DownloadJsonNetContractResolvers()
    {
        var downloader = new GitDownloader
        {
            RemoteUrl = "https://github.com/danielwertheim/jsonnet-contractresolvers.git",
            CommitHash = "91f03b4b302db94f695d98ba8fc9ed25efa616c5",
            TargetDir = mainGameDir / "Packages" / "playshared" / "Runtime" / "Plugins" / "JsonNet.ContractResolvers",
            Depth = cloneDepth,
            SparseCheckoutPatterns =
            {
                "src/main/JsonNet.ContractResolvers/*"
            },
            MovePostprocess =
            {
                { "src/main/JsonNet.ContractResolvers/*", "." }
            },
            DeletePostprocess =
            {
                "src",
                ".git"
            }
        };
        downloader.Download();

        AsmdefFileUtils.Create(downloader.TargetDir / "JsonNet.ContractResolvers.asmdef");
    }

    private void DownloadLeanTween()
    {
        var downloader = new GitDownloader
        {
            RemoteUrl = "https://github.com/UltraStar-Deluxe/LeanTween.git",
            CommitHash = "ea745c3f94d8682327c912030dfc6b65cbe1ced5",
            TargetDir = mainGameDir / "Assets" / "Plugins" / "LeanTween",
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

    private void DownloadLiteNetLib()
    {
        var downloader = new GitDownloader
        {
            RemoteUrl = "https://github.com/RevenantX/LiteNetLib.git",
            CommitHash = "47d57213baa96d4b343741c6ff3c2eedde961d46",
            TargetDir = mainGameDir / "Packages" / "playshared" / "Runtime" / "Plugins" / "LiteNetLib",
            Depth = cloneDepth,
            SparseCheckoutPatterns =
            {
                "LiteNetLib/*"
            },
            MovePostprocess =
            {
                { "LiteNetLib/*", "." }
            },
            DeletePostprocess =
            {
                "LiteNetLib",
                ".git"
            }
        };
        downloader.Download();
    }

    private void DownloadLrcParser()
    {
        var downloader = new GitDownloader
        {
            RemoteUrl = "https://github.com/OpportunityLiu/LrcParser.git",
            CommitHash = "a10a8422a53552e7870e41f6a1e751e9ae65956c",
            TargetDir = mainGameDir / "Assets" / "Plugins" / "Opportunity.LrcParser",
            Depth = cloneDepth,
            SparseCheckoutPatterns =
            {
                "Opportunity.LrcParser/*"
            },
            MovePostprocess =
            {
                { "Opportunity.LrcParser/*", "." }
            },
            DeletePostprocess =
            {
                "Opportunity.LrcParser",
                ".git"
            }
        };
        downloader.Download();
    }

    private void DownloadNHyphenator()
    {
        var downloader = new GitDownloader
        {
            RemoteUrl = "https://github.com/alkozko/NHyphenator.git",
            CommitHash = "a10a8422a53552e7870e41f6a1e751e9ae65956c", // Update with the correct commit hash if needed
            TargetDir = mainGameDir / "Assets" / "Plugins" / "NHyphenator",
            Depth = cloneDepth,
            SparseCheckoutPatterns =
            {
                "NHyphenator/*"
            },
            MovePostprocess =
            {
                { "NHyphenator/*", "." }
            },
            DeletePostprocess =
            {
                "NHyphenator",
                ".git"
            }
        };
        downloader.Download();
    }

    private void DownloadSerilog()
    {
        DownloadSerilogCore();
        DownloadSerilogSinksFile();

        Console.WriteLine("Downloading Serilog complete.");
    }


    private void DownloadSerilogCore()
    {
        var downloader = new GitDownloader
        {
            RemoteUrl = "https://github.com/serilog/serilog.git",
            CommitHash = "655778f74384f682d2c8705ab4883c39ef17e44d",
            TargetDir = mainGameDir / "Assets" / "Plugins" / "Serilog" / "Serilog",
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
            TargetDir = mainGameDir / "Assets" / "Plugins" / "Serilog" / "Serilog.Sinks.File",
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

    private async Task DownloadSoundfontsAsync()
    {
        var targetDir = mainGameDir / "Assets" / "Plugins" / "Soundfonts";
        DirectoryUtils.EnsureExistingDirectory(targetDir);
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
        var targetDir = mainGameDir / "Assets" / "StreamingAssets" / "SpleeterMsvcExe";
        DirectoryUtils.EnsureExistingDirectory(targetDir);
        var zipFile = targetDir / "SpleeterMsvcExe.zip";


        using (var client = new HttpClient())
        {
            using (var response = await client.GetAsync("https://github.com/achimmihca/SpleeterMsvcExe/releases/download/v1.0/SpleeterMsvcExe-v1.0-2stems-only.zip"))
            {
                response.EnsureSuccessStatusCode();
                using (var fileStream = File.Create(zipFile))
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
            TargetDir = mainGameDir / "Assets" / "Plugins" / "SpleeterSharp",
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

    private void DownloadUniRx()
    {
        var downloader = new GitDownloader
        {
            RemoteUrl = "https://github.com/neuecc/UniRx.git",
            CommitHash = "66205df49631860dd8f7c3314cb518b54c944d30",
            TargetDir = mainGameDir / "Assets" / "Plugins" / "UniRx",
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

    private void DownloadUnityStandaloneFileBrowser()
    {
        var downloader = new GitDownloader
        {
            RemoteUrl = "https://github.com/achimmihca/UnityStandaloneFileBrowser.git",
            CommitHash = "e75bcc71eb721b8979de6f4653fb507b5dbc2172",
            TargetDir = mainGameDir / "Assets" / "Plugins" / "UnityStandaloneFileBrowser",
            Depth = cloneDepth,
            SparseCheckoutPatterns = { "Assets/*" },
            MovePostprocess = { { "Assets/*", "." } },
            DeletePostprocess = { "Assets", ".git" },
        };
        downloader.Download();

        AsmdefFileUtils.Create(downloader.TargetDir / "UnityStandaloneFileBrowser.asmdef");
    }
}
