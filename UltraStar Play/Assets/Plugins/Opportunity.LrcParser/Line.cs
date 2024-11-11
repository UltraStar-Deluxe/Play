using System;
using System.Collections.Generic;
using System.Text;

namespace Opportunity.LrcParser
{
    /// <summary>
    /// Represents single line of lyrics.
    /// Format: <c>"[mm:ss.ff]Lyrics"</c>
    /// </summary>
    [System.Diagnostics.DebuggerDisplay(@"{ToString(),nq}")]
    public class Line : IComparable<Line>, IComparable
    {
        /// <summary>
        /// Create new instance of <see cref="Line"/>.
        /// </summary>
        public Line() { }

        /// <summary>
        /// Create new instance of <see cref="Line"/>.
        /// </summary>
        /// <param name="timestamp">Timestamp of this line.</param>
        /// <param name="content">Lyrics of this line.</param>
        public Line(DateTime timestamp, string content)
        {
            Timestamp = timestamp;
            this.content = (content ?? "").Trim();
        }

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        internal DateTime InternalTimestamp;
        /// <summary>
        /// Timestamp of this line of lyrics.
        /// </summary>
        /// <exception cref="ArgumentException"><see cref="DateTime.Kind"/> of value is not <see cref="DateTimeKind.Unspecified"/>.</exception>
        public DateTime Timestamp
        {
            get => this.InternalTimestamp;
            set => this.InternalTimestamp = value.ToTimestamp();
        }

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private string content = "";
        /// <summary>
        /// Lyrics of this line.
        /// </summary>
        public virtual string Content { get => this.content; set => this.content = (value ?? "").Trim(); }

        internal StringBuilder ToString(StringBuilder sb)
        {
            return TimestampToString(sb).Append(this.Content);
        }

        internal StringBuilder TimestampToString(StringBuilder sb)
        {
            return sb.Append('[')
                .Append(this.InternalTimestamp.ToLrcString())
                .Append(']');
        }

        /// <inheritdoc/>
        public override string ToString()
           => ToString(new StringBuilder(this.content.Length + 10)).ToString();

        /// <inheritdoc/>
        public int CompareTo(Line other)
        {
            if (other is null)
                return 1;
            var ct = this.InternalTimestamp.CompareTo(other.InternalTimestamp);
            if (ct != 0)
                return ct;
            return string.Compare(this.Content, other.Content);
        }
        int IComparable.CompareTo(object obj) => CompareTo((Line)obj);
    }

    /// <summary>
    /// Represents single line of lyrics with specified speaker.
    /// Format: <c>"[mm:ss.ff]Spearker: Lyrics"</c>
    /// </summary>
    public class LineWithSpeaker : Line
    {
        private static readonly char[] invalidSpeakerChars = ":".ToCharArray();

        /// <summary>
        /// Create new instance of <see cref="Line"/>.
        /// </summary>
        public LineWithSpeaker() { }

        /// <summary>
        /// Create new instance of <see cref="Line"/>.
        /// </summary>
        /// <param name="timestamp">Timestamp of this line.</param>
        /// <param name="speaker">Speaker of this line.</param>
        /// <param name="lyrics">Lyrics of this line.</param>
        public LineWithSpeaker(DateTime timestamp, string speaker, string lyrics)
            : base(timestamp, null)
        {
            this.Speaker = speaker;
            this.Lyrics = lyrics;
        }

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private string speaker = "";
        /// <summary>
        /// Speaker of this line.
        /// </summary>
        public string Speaker
        {
            get => this.speaker;
            set
            {
                value = value ?? "";
                Helper.CheckString(nameof(value), value, invalidSpeakerChars);
                this.speaker = value.Trim();
            }
        }
        /// <summary>
        /// Lyrics of this line.
        /// </summary>
        public string Lyrics { get => base.Content; set => base.Content = value; }

        /// <summary>
        /// Lyrics with speaker of this line.
        /// </summary>
        public override string Content
        {
            get
            {
                if (string.IsNullOrEmpty(this.speaker))
                    return Lyrics;
                if (string.IsNullOrEmpty(this.Lyrics))
                    return Speaker + ":";
                return Speaker + ": " + Lyrics;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    this.speaker = "";
                    this.Lyrics = "";
                    return;
                }
                var pi = value.IndexOf(':');
                if (pi < 0)
                {
                    this.speaker = "";
                    this.Lyrics = value;
                }
                else
                {
                    this.Speaker = value.Substring(0, pi);
                    this.Lyrics = value.Substring(pi + 1);
                }
            }
        }
    }
}
