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
        DownloadDynamicQueryable();
        DownloadJokenizer();
        DownloadJsonNetContractResolvers();
        DownloadLiteNetLib();
        DownloadLrcParser();
        DownloadNHyphenator();
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

    private void DownloadDynamicQueryable()
    {
        var downloader = new GitDownloader
        {
            RemoteUrl = "https://github.com/umutozel/DynamicQueryable.git",
            CommitHash = "c4832d4a69127074699b8d4a6c8d897fff644bdb",
            TargetDir = unityProjectDir / "Assets" / "Plugins" / "DynamicQueryable",
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
            TargetDir = unityProjectDir / "Assets" / "Plugins" / "Jokenizer.Net",
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
            TargetDir = unityProjectDir / "Packages" / "playshared" / "Runtime" / "Plugins" / "JsonNet.ContractResolvers",
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

    private void DownloadLiteNetLib()
    {
        var downloader = new GitDownloader
        {
            RemoteUrl = "https://github.com/RevenantX/LiteNetLib.git",
            CommitHash = "47d57213baa96d4b343741c6ff3c2eedde961d46",
            TargetDir = unityProjectDir / "Packages" / "playshared" / "Runtime" / "Plugins" / "LiteNetLib",
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
            TargetDir = unityProjectDir / "Assets" / "Plugins" / "Opportunity.LrcParser",
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
            TargetDir = unityProjectDir / "Assets" / "Plugins" / "NHyphenator",
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

    private async Task DownloadSoundfontsAsync()
    {
        var targetDir = unityProjectDir / "Assets" / "Plugins" / "Soundfonts";
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
        var targetDir = unityProjectDir / "Assets" / "StreamingAssets" / "SpleeterMsvcExe";
        DirectoryUtils.EnsureExistingDirectory(targetDir);
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
