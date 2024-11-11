using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SpleeterSharp
{
    internal class SpleeterCommandLineRunner
    {
        private static readonly Regex fileWrittenRegex = new Regex(@"INFO:spleeter:File\s(.*)\swritten");
        private static readonly Regex errorRegex = new Regex(@"ERROR:spleeter:(.*)|Error: (.*)");

        public static async Task<SpleeterResult> SplitAsync(
            SpleeterParameters spleeterParameters,
            CancellationToken cancellationToken)
        {
            List<string> parameterStringList = GetParameterStringList(spleeterParameters);
            string args = string.Join(' ', parameterStringList);
            ShellExecutionResult shellExecutionResult = await ShellUtils.ExecuteAsync(SpleeterSharpConfig.Config.SpleeterCommand, args, cancellationToken);
            return ParseSpleeterProcessOutput(shellExecutionResult.ExitCode, shellExecutionResult.Output);
        }

        private static SpleeterResult ParseSpleeterProcessOutput(int exitCode, string processOutput)
        {
            SpleeterResult spleeterResult = new SpleeterResult();
            spleeterResult.ExitCode = exitCode;
            spleeterResult.Output = processOutput;

            bool startOfOutputFiles = false;
            
            // Parse process output line by line
            void ParseLine(string line)
            {
                // Parse output of SpleeterMsvcExe
                string trimmedLine = line.Trim();
                if (startOfOutputFiles)
                {
                    if (trimmedLine.EndsWith(".vocals.ogg")
                        || trimmedLine.EndsWith(".vocals.mp3")
                        || trimmedLine.EndsWith(".accompaniment.ogg")
                        || trimmedLine.EndsWith(".accompaniment.mp3"))
                    {
                        string filePath = trimmedLine;
                        string absoluteFilePath = new FileInfo(filePath).FullName;
                        spleeterResult.WrittenFiles.Add(absoluteFilePath);
                    }
                    else if (trimmedLine.StartsWith("["))
                    {
                        startOfOutputFiles = false;
                    }
                }
                else if (trimmedLine.StartsWith("Output files:"))
                {
                    startOfOutputFiles = true;
                }
                
                // Parse output of Spleeter as Python module
                Match fileWrittenRegexMatch = fileWrittenRegex.Match(line);
                if (fileWrittenRegexMatch.Success)
                {
                    string filePath = fileWrittenRegexMatch.Groups[1].Value;
                    string absoluteFilePath = new FileInfo(filePath).FullName;
                    spleeterResult.WrittenFiles.Add(absoluteFilePath);
                    return;
                }

                Match errorRegexMatch = errorRegex.Match(line);
                if (errorRegexMatch.Success)
                {
                    string errorMessageGroup1 = errorRegexMatch.Groups[1].Value;
                    string errorMessageGroup2 = errorRegexMatch.Groups[2].Value;
                    string errorMessage = !string.IsNullOrEmpty(errorMessageGroup1)
                        ? errorMessageGroup1
                        : errorMessageGroup2;
                    spleeterResult.Errors.Add(errorMessage);
                }
            }

            using StringReader stringReader = new StringReader(processOutput);
            string line;
            while ((line = stringReader.ReadLine()) != null)
            {
                ParseLine(line);
            }

            // Add the whole process output as error if no specific error was found
            if (spleeterResult.ExitCode != 0
                && spleeterResult.Errors.Count == 0)
            {
                spleeterResult.Errors.Add(spleeterResult.Output);
            }
            
            return spleeterResult;
        }

        private static List<string> GetParameterStringList(SpleeterParameters spleeterParameters)
        {
            return new List<string>
                {
                    GetInputFileParameter(spleeterParameters.InputFile),
                    GetOutputFolderParameter(spleeterParameters.OutputFolder),
                    GetOutputFileBitrateParameter(spleeterParameters.OutputFileBitrate),
                    GetOutputFileCodecParameter(spleeterParameters.OutputFileCodec),
                    GetFileNameFormatParameter(spleeterParameters.FileNameFormat),
                    GetAudioAdapterParameter(spleeterParameters.AudioAdapter),
                    GetParamsFileParameter(spleeterParameters.ParamsFileName),
                    GetMaxDurationParameter(spleeterParameters.MaxDuration),
                    GetOffsetParameter(spleeterParameters.Offset),
                    GetMultiChannelWienerFilteringParameter(spleeterParameters.MultiChannelWienerFiltering),
                    GetOverwriteParameter(spleeterParameters.Overwrite),
                }
                .Where(param => !string.IsNullOrEmpty(param))
                .ToList();
        }

        private static string GetInputFileParameter(string inputFile)
        {
            if (string.IsNullOrEmpty(inputFile))
            {
                throw new ArgumentNullException(nameof(inputFile));
            }
            return $"\"{inputFile}\"";
        }

        private static string GetOutputFolderParameter(string outputFolder)
        {
            return !string.IsNullOrEmpty(outputFolder)
                ? $"-o \"{outputFolder}\""
                : "";
        }

        private static string GetOutputFileCodecParameter(string outputFileCodec)
        {
            return !string.IsNullOrEmpty(outputFileCodec)
                ? $"--codec \"{outputFileCodec}\""
                : "";
        }

        private static string GetOutputFileBitrateParameter(string outputFileBitrate)
        {
            return !string.IsNullOrEmpty(outputFileBitrate)
                ? $"--bitrate \"{outputFileBitrate}\""
                : "";
        }

        private static string GetAudioAdapterParameter(string audioAdapter)
        {
            return !string.IsNullOrEmpty(audioAdapter)
                ? $"--adapter \"{audioAdapter}\""
                : "";
        }

        private static string GetParamsFileParameter(string paramsFileName)
        {
            return !string.IsNullOrEmpty(paramsFileName)
                ? $"--params_filename {paramsFileName}"
                : "";
        }

        private static string GetMaxDurationParameter(float maxDuration)
        {
            return maxDuration > 0
                ? $"--duration {maxDuration}"
                : "";
        }

        private static string GetOffsetParameter(float offset)
        {
            return offset > 0
                ? $"--offset {offset}"
                : "";
        }

        private static string GetFileNameFormatParameter(string fileNameFormat)
        {
            return !string.IsNullOrEmpty(fileNameFormat)
                ? $"--filename_format {fileNameFormat}"
                : "";
        }

        private static string GetMultiChannelWienerFilteringParameter(bool useMultiChannelWienerFiltering)
        {
            return useMultiChannelWienerFiltering
                ? "--mwf"
                : "";
        }
        
        private static string GetOverwriteParameter(bool overwrite)
        {
            return overwrite
                ? "--overwrite"
                : "";
        }
    }
}
