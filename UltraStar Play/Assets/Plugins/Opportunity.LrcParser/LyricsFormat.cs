using System;
using System.Runtime.CompilerServices;

namespace Opportunity.LrcParser
{
    /// <summary>
    /// Flags for lyrics serialization.
    /// </summary>
    [Flags]
    public enum LyricsFormat
    {
        /// <summary>
        /// Default flags were set.
        /// </summary>
        Default = NewLineAtEndOfFile | LinesSortByTimestamp | MetadataSortByTag | SkipEmptyMetadata,
        /// <summary>
        /// No flags were set.
        /// </summary>
        None = 0,
        /// <summary>
        /// Add new line at begin of file.
        /// </summary>
        NewLineAtBeginOfFile = 0b_0000_0000_0000_0000_0000_0000_0000_0001,
        /// <summary>
        /// Add new line at end of file.
        /// </summary>
        NewLineAtEndOfFile = 0b_0000_0000_0000_0000_0000_0000_0000_0010,
        /// <summary>
        /// Add new line at end of ID tags section.
        /// </summary>
        NewLineAtEndOfMetadata = 0b_0000_0000_0000_0000_0000_0000_0000_0100,

        /// <summary>
        /// Sort lines by <see cref="Line.Timestamp"/>.
        /// </summary>
        LinesSortByTimestamp = 0b_0000_0000_0000_0000_0000_0001_0000_0000,
        /// <summary>
        /// Sort lines by <see cref="Line.Content"/>.
        /// </summary>
        LinesSortByContent = 0b_0000_0000_0000_0000_0000_0010_0000_0000,
        /// <summary>
        /// Merge <see cref="Line"/> with same contents.
        /// </summary>
        MergeTimestamp = 0b_0000_0000_0000_0000_0001_0000_0000_0000,

        /// <summary>
        /// Sort metadata by <see cref="MetaDataType.Tag"/>.
        /// </summary>
        MetadataSortByTag = 0b_0000_0000_0000_0001_0000_0000_0000_0000,
        /// <summary>
        /// Sort metadata by content.
        /// </summary>
        MetadataSortByContent = 0b_0000_0000_0000_0010_0000_0000_0000_0000,
        /// <summary>
        /// Skip metadata whose content is empty.
        /// </summary>
        SkipEmptyMetadata = 0b_0000_0000_0001_0000_0000_0000_0000_0000,
    }

    internal static class LyricsFormatExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Flag(this LyricsFormat value, LyricsFormat flag)
        {
            return (value & flag) == flag;
        }
    }
}
