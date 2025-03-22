using System;

namespace BasicPitchRunner
{
    public class BasicPitchRunnerException : Exception
    {
        public BasicPitchRunnerException(string message) : base(message)
        {
        }

        public BasicPitchRunnerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
