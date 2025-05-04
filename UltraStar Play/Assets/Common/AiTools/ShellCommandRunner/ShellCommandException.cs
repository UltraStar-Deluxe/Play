using System;

namespace ShellCommandRunner
{
    public class ShellCommandException : Exception
    {
        public ShellCommandException(string message) : base(message)
        {
        }

        public ShellCommandException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
