using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common.IO;

namespace DefaultNamespace;

public class MainGameDependencyDownloader(
    AbsolutePath unityProjectDir,
    uint cloneDepth,
    bool downloadAiModels = false,
    bool downloadFfmpegLibraries = false
) : BaseDependencyDownloader(unityProjectDir, cloneDepth) {

    public override async Task DownloadAsync()
    {
        await base.DownloadAsync();

        DownloadCSharpSynthForUnity();
        DownloadUnityStandaloneFileBrowser();
        if (downloadAiModels)
        {
            await DownloadAiModelsAsync();
        }
        if (downloadFfmpegLibraries)
        {
            await DownloadFfmpegLibrariesAsync();
        }
    }

    private async Task DownloadAiModelsAsync()
    {
        await DownloadNeMoForcedAlignerModelAsync();
        await DownloadRmvpeModelAsync();
        await DownloadSherpaOnnxSpeechRecognitionModelAsync();
        await DownloadSherpaOnnxSourceSeparationModelAsync();
    }

    private async Task DownloadNeMoForcedAlignerModelAsync()
    {
        await FileDownloader.DownloadFilesAsync(
            "https://huggingface.co/anstdev/nemo-forced-aligner-onnx/resolve/main",
            unityProjectDir / "Assets/StreamingAssets/AiModels/NeMoForcedAligner",
            "stt_en_conformer_ctc_large.onnx",
            "stt_en_conformer_ctc_large.txt");
    }

    private async Task DownloadRmvpeModelAsync()
    {
        await FileDownloader.DownloadFilesAsync(
            "https://huggingface.co/wok000/vcclient_modules/resolve/main/rmvpe",
            unityProjectDir / "Assets/StreamingAssets/AiModels/rmvpe",
            "rmvpe_20231006.onnx");
    }

    private async Task DownloadSherpaOnnxSpeechRecognitionModelAsync()
    {
        await FileDownloader.DownloadFilesAsync(
            "https://huggingface.co/csukuangfj/sherpa-onnx-nemo-parakeet-tdt-0.6b-v3-int8/resolve/main",
            unityProjectDir / "Assets/StreamingAssets/sherpa-onnx/models/speech-recognition/sherpa-onnx-nemo-parakeet-tdt-0.6b-v3-int8",
            "decoder.int8.onnx",
            "encoder.int8.onnx",
            "joiner.int8.onnx",
            "tokens.txt",
            "test_wavs/de.wav",
            "test_wavs/en.wav",
            "test_wavs/es.wav",
            "test_wavs/fr.wav");
    }

    private async Task DownloadSherpaOnnxSourceSeparationModelAsync()
    {
        await FileDownloader.DownloadFilesAsync(
            "https://huggingface.co/k2-fsa/sherpa-onnx-models/resolve/main/source-separation-models",
            unityProjectDir / "Assets/StreamingAssets/sherpa-onnx/models/source-separation/UVR_MDXNET_KARA_2",
            "UVR_MDXNET_KARA_2.onnx");
    }

    private async Task DownloadFfmpegLibrariesAsync()
    {
        // For now, it is enough to download the DLL files for Windows only
        await DownloadFfmpegWindowsLibrariesAsync();
    }

    private async Task DownloadFfmpegWindowsLibrariesAsync()
    {
        AbsolutePath targetDir = unityProjectDir / "Assets/StreamingAssets/FfmpegLibraries/Windows";
        string[] expectedDlls =
        [
            "avcodec-62.dll",
            "avdevice-62.dll",
            "avfilter-11.dll",
            "avformat-62.dll",
            "avutil-60.dll",
            "swresample-6.dll",
            "swscale-9.dll",
        ];

        bool allDllsExist = expectedDlls.All(dll => File.Exists(targetDir / dll)
                                                         && new FileInfo(targetDir / dll).Length > 0);
        if (allDllsExist)
        {
            Console.WriteLine($"FFmpeg Windows libraries already exist, skipping download: '{targetDir}'");
            return;
        }

        string version = "8.1";
        string archiveName = $"ffmpeg-{version}-full_build-shared.zip";
        string url = $"https://github.com/GyanD/codexffmpeg/releases/download/{version}/{archiveName}";

        AbsolutePath tempZipFile = (AbsolutePath)Path.Combine(Path.GetTempPath(), archiveName);
        try
        {
            await FileDownloader.DownloadFileAsync(url, tempZipFile);

            Console.WriteLine($"Extracting '{tempZipFile}' to '{targetDir}'");
            DirectoryUtils.CreateDirectory(targetDir);

            using var archive = ZipFile.OpenRead(tempZipFile);
            foreach (var entry in archive.Entries)
            {
                if ((entry.FullName.Contains("/bin/") || entry.FullName.Contains(@"\bin\"))
                    && entry.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    AbsolutePath destinationPath = targetDir / entry.Name;
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }
        }
        finally
        {
            FileUtils.DeleteFile(tempZipFile);
        }
    }
    
    private void DownloadCSharpSynthForUnity()
    {
        var downloader = new GitDownloader
        {
            RemoteUrl = "https://github.com/KNCarnage/CSharpSynthForUnity2.0.git",
            CommitHash = "806bce0820d2611f804066e06e1cd3842439addd",
            TargetDir = unityProjectDir / "Assets/Plugins/CSharpSynthForUnity",
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
            TargetDir = unityProjectDir / "Assets/Plugins/UnityStandaloneFileBrowser",
            Depth = cloneDepth,
            SparseCheckoutPatterns = { "Assets/*" },
            MovePostprocess = { { "Assets/*", "." } },
            DeletePostprocess = { "Assets", ".git" },
        };
        downloader.Download();

        AsmdefFileUtils.Create(downloader.TargetDir / "UnityStandaloneFileBrowser.asmdef");
    }
}
