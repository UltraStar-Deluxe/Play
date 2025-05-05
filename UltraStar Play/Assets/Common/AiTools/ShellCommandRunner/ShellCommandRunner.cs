using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShellCommandRunner
{
    /**
     * Executes a command using the system shell (cmd.exe on Windows, bash on Linux and macOS).
     */
    public class ShellCommandRunner
    {
        private readonly Action<string> logAction;
        private readonly Encoding encoding;

        private bool IsWindows => PlatformUtils.IsWindows;

        public ShellCommandRunner(Action<string> logAction, Encoding encoding)
        {
            this.logAction = logAction;
            this.encoding = encoding;
        }

        public Task<ShellCommandResult> RunAsync(
            string cmd,
            CancellationToken cancellationToken)
        {
            logAction($"Executing command: {cmd}");

            StringBuilder outputBuilder = new StringBuilder();
            Process process = GetProcess(cmd, outputBuilder);

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
                            logAction($"Canceled command of process {process.Id}: {cmd}");
                            process.Kill();
                            logAction($"Killed process {process.Id}");
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

                    return new ShellCommandResult()
                    {
                        ExitCode = process.ExitCode,
                        Output = outputBuilder.ToString()
                    };
                },
                cancellationToken);
        }

        private Process GetProcess(string cmd, StringBuilder outputBuilder)
        {
            Process process = new Process()
            {
                StartInfo = GetProcessStartInfo(cmd),
                EnableRaisingEvents = true
            };

            process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
            {
                if (string.IsNullOrWhiteSpace(e.Data))
                {
                    return;
                }

                logAction(e.Data);
                outputBuilder.AppendLine(e.Data);
            };

            process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
            {
                if (string.IsNullOrWhiteSpace(e.Data))
                {
                    return;
                }

                logAction(e.Data);
                outputBuilder.AppendLine(e.Data);
            };
            return process;
        }

        private ProcessStartInfo GetProcessStartInfo(string cmd)
        {
            string escapedArgs = IsWindows
                ? cmd
                : cmd.Replace("\"", "\\\"");

            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = IsWindows
                    ? "cmd.exe"
                    : "/bin/bash",
                Arguments = IsWindows
                    ? $"/C \"{escapedArgs}\""
                    : $"-c \"{escapedArgs}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = encoding,
                StandardErrorEncoding = encoding,
                StandardInputEncoding = encoding,
            };

            Dictionary<string,string> environmentVariables = GetEnvironmentVariables();
            foreach (KeyValuePair<string, string> entry in environmentVariables)
            {
                processStartInfo.EnvironmentVariables.Add(entry.Key, entry.Value);
            }

            return processStartInfo;
        }

        private Dictionary<string, string> GetEnvironmentVariables()
        {
            if (Equals(encoding, Encoding.UTF8))
            {
                // Set encoding to UTF8 to avoid issues with Python executables (e.g. BasicPitch compiled via PyInstaller)
                return new()
                {
                    { "PYTHONIOENCODING", "utf8" },
                    { "PYTHONUTF8", "1" },
                };
            }
            return new();
        }
    }
}
