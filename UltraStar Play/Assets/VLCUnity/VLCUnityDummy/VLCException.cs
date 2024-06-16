using System;

namespace LibVLCSharp
{
    public class VLCException : Exception
    {
        public VLCException(string message) : base(message)
        {
        }

        public VLCException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
