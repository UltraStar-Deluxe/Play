namespace ShellCommandRunner
{
    public class ShellCommandResult
    {
        /**
         * Combined output of stdOut and stdError.
         */
        public string Output { get; set; }
        public int ExitCode { get; set; }
    }
}
