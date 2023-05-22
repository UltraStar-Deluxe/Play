using System;

namespace BasicPitchRunner
{
    public class BasicPitchRunnerConfig
    {
        private static BasicPitchRunnerConfig config;
        public static BasicPitchRunnerConfig Config
        {
            get
            {
                if (config == null)
                {
                    throw new BasicPitchRunnerException("BasicPitchRunnerConfig config not initialized");
                }
                return config;
            }
        }

        public static BasicPitchRunnerConfig Create()
        {
            config = new BasicPitchRunnerConfig();
            return config;
        }

        public string Command { get; private set; } = "spleeter";
        public bool IsWindows { get; private set; }
        public Action<string> LogAction { get; private set; }

        private BasicPitchRunnerConfig()
        {
        }

        public BasicPitchRunnerConfig SetIsWindows(bool isWindows)
        {
            IsWindows = isWindows;
            return this;
        }

        public BasicPitchRunnerConfig SetLogAction(Action<string> logAction)
        {
            LogAction = logAction;
            return this;
        }

        public BasicPitchRunnerConfig SetBasicPitchCommand(string command)
        {
            Command = command;
            return this;
        }
    }
}
