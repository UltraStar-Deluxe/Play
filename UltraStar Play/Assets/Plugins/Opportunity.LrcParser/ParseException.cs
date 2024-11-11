using System;
using System.Collections.Generic;
using System.Text;

namespace Opportunity.LrcParser
{
    /// <summary>
    /// Exception in parsing.
    /// </summary>
    public sealed class ParseException : Exception
    {
        private static string generateMessage(string data, int pos, string message)
        {
            return $@"{message}
Position: {pos}";
        }

        internal ParseException(string data, int pos, string message, Exception innerException)
            : base(generateMessage(data, pos, message), innerException)
        {
            this.RawLyrics = data;
            this.Position = pos;
        }

        /// <summary>
        /// Position of exception.
        /// </summary>
        public int Position { get; }

        /// <summary>
        /// Raw lrc data of exception.
        /// </summary>
        public string RawLyrics { get; }
    }
}
