using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Opportunity.LrcParser
{
    /// <summary>
    /// Dictionary of lrc metadata.
    /// </summary>
    public sealed class MetaDataDictionary : Dictionary<MetaDataType, string>
    {
        internal MetaDataDictionary() { }

        private object tryGet(MetaDataType key)
        {
            try
            {
                if (TryGetValue(key, out var r) && r != null)
                    return key.Parse(r);
            }
            catch { }
            return key.Default;
        }

        private void set(MetaDataType key, object value)
        {
            if (value is null || Equals(value, key.Default))
            {
                this.Remove(key);
                return;
            }
            var str = key.Stringify(value);
            if (str is null)
                this.Remove(key);
            else
                this[key] = str;
        }

        /// <summary>
        /// Lyrics artist, "ar" field of ID Tags.
        /// </summary>
        public string Artist
        {
            get => (string)tryGet(MetaDataType.Artist);
            set => set(MetaDataType.Artist, value);
        }

        /// <summary>
        /// Album where the song is from, "al" field of ID Tags.
        /// </summary>
        public string Album
        {
            get => (string)tryGet(MetaDataType.Album);
            set => set(MetaDataType.Album, value);
        }

        /// <summary>
        /// Lyrics(song) title, "ti" field of ID Tags.
        /// </summary>
        public string Title
        {
            get => (string)tryGet(MetaDataType.Title);
            set => set(MetaDataType.Title, value);
        }

        /// <summary>
        /// Creator of the songtext, "au" field of ID Tags.
        /// </summary>
        public string Author
        {
            get => (string)tryGet(MetaDataType.Author);
            set => set(MetaDataType.Author, value);
        }

        /// <summary>
        /// Creator of the LRC file, "by" field of ID Tags.
        /// </summary>
        public string Creator
        {
            get => (string)tryGet(MetaDataType.Creator);
            set => set(MetaDataType.Creator, value);
        }

        /// <summary>
        /// Overall timestamp adjustment, "offset" field of ID Tags.
        /// </summary>
        public TimeSpan Offset
        {
            get => (TimeSpan)tryGet(MetaDataType.Offset);
            set => set(MetaDataType.Offset, value);
        }

        /// <summary>
        /// The player or editor that created the LRC file, "re" field of ID Tags.
        /// </summary>
        public string Editor
        {
            get => (string)tryGet(MetaDataType.Editor);
            set => set(MetaDataType.Editor, value);
        }

        /// <summary>
        /// Version of program, "ve" field of ID Tags.
        /// </summary>
        public string Version
        {
            get => (string)tryGet(MetaDataType.Version);
            set => set(MetaDataType.Version, value);
        }

        /// <summary>
        /// Length of song, "length" field of ID Tags.
        /// </summary>
        public DateTime Length
        {
            get => (DateTime)tryGet(MetaDataType.Length);
            set => set(MetaDataType.Length, value);
        }

        internal StringBuilder ToString(StringBuilder sb, LyricsFormat format)
        {
            var datasource = this.AsEnumerable();
            if (format.Flag(LyricsFormat.MetadataSortByContent))
                datasource = datasource.OrderBy(d => d.Key.Stringify(d.Value));
            if (format.Flag(LyricsFormat.LinesSortByTimestamp))
                datasource = (datasource is IOrderedEnumerable<KeyValuePair<MetaDataType, string>> od)
                    ? od.ThenBy(l => l.Key.Tag)
                    : datasource.OrderBy(l => l.Key.Tag);
            var skip = format.Flag(LyricsFormat.SkipEmptyMetadata);
            foreach (var item in datasource)
            {
                var v = item.Value;
                if (skip && string.IsNullOrEmpty(v))
                    continue;
                sb.Append('[')
                    .Append(item.Key.Tag)
                    .Append(':')
                    .Append(v)
                    .Append(']')
                    .AppendLine();
            }
            return sb;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder(this.Count * 10);
            ToString(sb, LyricsFormat.Default);
            return sb.ToString();
        }
    }
}
