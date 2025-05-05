using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ShellCommandRunner;

namespace BasicPitchRunner
{
    public class BasicPitchRunner
    {
        private static readonly Regex fileWrittenRegex = new Regex(@".+Saved to (.*)");
        private static readonly Regex errorRegex = new Regex(@"Error: (.*)");

        private readonly string basicPitchExecutable;
        private readonly Action<string> logAction;

        public BasicPitchRunner(string basicPitchExecutable, Action<string> logAction)
        {
            this.logAction = logAction;
            this.basicPitchExecutable = basicPitchExecutable;
        }

        public async Task<BasicPitchResult> RunAsync(
            BasicPitchParameters basicPitchParameters,
            CancellationToken cancellationToken)
        {
            List<string> parameterStringList = GetParameterStringList(basicPitchParameters);
            string fullCommand = $"{basicPitchExecutable} {string.Join(' ', parameterStringList)}";
            ShellCommandRunner.ShellCommandRunner shellCommandRunner = new(logAction, Encoding.UTF8);
            ShellCommandResult result = await shellCommandRunner.RunAsync(fullCommand, cancellationToken);
            return ParseBasicPitchProcessOutput(result.ExitCode, result.Output);
        }

        private static BasicPitchResult ParseBasicPitchProcessOutput(int exitCode, string processOutput)
        {
            BasicPitchResult basicPitchResult = new BasicPitchResult();
            basicPitchResult.ExitCode = exitCode;
            basicPitchResult.Output = processOutput;

            // Parse process output line by line
            void ParseLine(string line)
            {
                Match fileWrittenRegexMatch = fileWrittenRegex.Match(line);
                if (fileWrittenRegexMatch.Success)
                {
                    string filePath = fileWrittenRegexMatch.Groups[1].Value;
                    string absoluteFilePath = new FileInfo(filePath).FullName;
                    basicPitchResult.WrittenFiles.Add(absoluteFilePath);
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
                    basicPitchResult.Errors.Add(errorMessage);
                }
            }

            using StringReader stringReader = new StringReader(processOutput);
            string line;
            while ((line = stringReader.ReadLine()) != null)
            {
                ParseLine(line);
            }

            // Add the whole process output as error if no specific error was found
            if (basicPitchResult.ExitCode != 0
                && basicPitchResult.Errors.Count == 0)
            {
                basicPitchResult.Errors.Add(basicPitchResult.Output);
            }

            return basicPitchResult;
        }

        private static List<string> GetParameterStringList(BasicPitchParameters basicPitchParameters)
        {
            return new List<string>
                {
                    GetInputFileParameter(basicPitchParameters.InputFile),
                    GetOutputFolderParameter(basicPitchParameters.OutputFolder),
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

            return $"--input_file \"{inputFile}\"";
        }

        private static string GetOutputFolderParameter(string outputFolder)
        {
            return !string.IsNullOrEmpty(outputFolder)
                ? $"--output_folder \"{outputFolder}\""
                : "";
        }
    }
}
