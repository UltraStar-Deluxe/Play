using System;

namespace SpleeterSharp
{
    public class SpleeterSharpException : Exception
    {
        public SpleeterSharpException(
            string message,
            Exception innerException = null) : base(message, innerException)
        {
        }
    }
}
