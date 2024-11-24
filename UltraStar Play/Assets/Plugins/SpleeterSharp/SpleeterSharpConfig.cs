using System;

namespace SpleeterSharp
{
    public class SpleeterSharpConfig
    {
        private static SpleeterSharpConfig config;
        public static SpleeterSharpConfig Config
        {
            get
            {
                if (config == null)
                {
                    throw new SpleeterSharpException("SpleeterSharp config not initialized");
                }
                return config;
            }
        }

        public static SpleeterSharpConfig Create()
        {
            config = new SpleeterSharpConfig();
            return config;
        }

        public string SpleeterCommand { get; private set; } = "spleeter";
        public bool IsWindows { get; private set; }
        public Action<string> LogAction { get; private set; }

        private SpleeterSharpConfig()
        {
        }

        public SpleeterSharpConfig SetIsWindows(bool isWindows)
        {
            IsWindows = isWindows;
            return this;
        }

        public SpleeterSharpConfig SetLogAction(Action<string> logAction)
        {
            LogAction = logAction;
            return this;
        }

        public SpleeterSharpConfig SetSpleeterCommand(string spleeterCommand)
        {
            SpleeterCommand = spleeterCommand;
            return this;
        }
    }
}
