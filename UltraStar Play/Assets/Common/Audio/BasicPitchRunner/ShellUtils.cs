using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BasicPitchRunner
{
    internal static class ShellUtils
    {
        /**
         * Executes a command using the system shell (cmd.exe on Windows, bash on Linux and macOS).
         */
        public static Task<ShellExecutionResult> ExecuteAsync(
            string cmd,
            CancellationToken cancellationToken,
            Action<string> stdErrDataReceivedCallback = null,
            Action<string> stdOutDataReceivedCallback = null)
        {
            BasicPitchRunnerConfig.Config.LogAction?.Invoke($"Executing command: {cmd}");

            string escapedArgs = BasicPitchRunnerConfig.Config.IsWindows
                ? cmd
                : cmd.Replace("\"", "\\\"");
            StringBuilder outputBuilder = new StringBuilder();
            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = BasicPitchRunnerConfig.Config.IsWindows
                        ? "cmd.exe"
                        : "/bin/bash",
                    Arguments = BasicPitchRunnerConfig.Config.IsWindows
                        ? $"/C \"{escapedArgs}\""
                        : $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    StandardInputEncoding = Encoding.UTF8,
                    EnvironmentVariables =
                    {
                        { "PYTHONIOENCODING", "utf8" },
                        { "PYTHONUTF8", "1" },
                    }
                },
                EnableRaisingEvents = true
            };
            process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
            {
                if (string.IsNullOrWhiteSpace(e.Data))
                {
                    return;
                }

                if (stdErrDataReceivedCallback == null)
                {
                    BasicPitchRunnerConfig.Config.LogAction?.Invoke(e.Data);
                }
                else
                {
                    stdErrDataReceivedCallback.Invoke(e.Data);
                }

                outputBuilder.AppendLine(e.Data);
            };
            process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
            {
                if (string.IsNullOrWhiteSpace(e.Data))
                {
                    return;
                }

                if (stdOutDataReceivedCallback == null)
                {
                    BasicPitchRunnerConfig.Config.LogAction?.Invoke(e.Data);
                }
                else
                {
                    stdOutDataReceivedCallback.Invoke(e.Data);
                }

                outputBuilder.AppendLine(e.Data);
            };

            return Task.Run( () =>
                {
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    bool isKilled = false;
                    while (!process.HasExited
                            && !isKilled)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            BasicPitchRunnerConfig.Config.LogAction?.Invoke($"Canceled command of process {process.Id}: {cmd}");
                            process.Kill();
                            BasicPitchRunnerConfig.Config.LogAction?.Invoke($"Killed process {process.Id}");
                            isKilled = true;
                        }
                        else
                        {
                            Thread.Sleep(200);
                        }
                    }

                    process.CancelOutputRead();
                    process.CancelErrorRead();
                    cancellationToken.ThrowIfCancellationRequested();

                    return new ShellExecutionResult()
                    {
                        ExitCode = process.ExitCode,
                        Output = outputBuilder.ToString()
                    };
                },
                cancellationToken);
        }
    }
}
