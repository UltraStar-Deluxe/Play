using System.Diagnostics;
using System.Text;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SpleeterSharp
{
    internal static class ShellUtils
    {
        /**
         * Executes a command using the system shell (cmd.exe on Windows, bash on Linux and macOS).
         */
        public static Task<ShellExecutionResult> ExecuteAsync(
            string program,
            string args,
            CancellationToken cancellationToken,
            Action<string> stdErrDataReceivedCallback = null,
            Action<string> stdOutDataReceivedCallback = null)
        {
            SpleeterSharpConfig.Config.LogAction?.Invoke($"Executing command: {program} {args}");

            Encoding encoding = Encoding.GetEncoding(
                "Latin1",
                Encoding.UTF8.EncoderFallback,
                Encoding.UTF8.DecoderFallback
            );

            StringBuilder outputBuilder = new StringBuilder();
            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = program,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = encoding,
                    StandardErrorEncoding = encoding,
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
                    SpleeterSharpConfig.Config.LogAction?.Invoke(e.Data);
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
                    SpleeterSharpConfig.Config.LogAction?.Invoke(e.Data);
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
                            SpleeterSharpConfig.Config.LogAction?.Invoke($"Canceled command of process {process.Id}: {program} {args}");
                            process.Kill();
                            SpleeterSharpConfig.Config.LogAction?.Invoke($"Killed process {process.Id}");
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
