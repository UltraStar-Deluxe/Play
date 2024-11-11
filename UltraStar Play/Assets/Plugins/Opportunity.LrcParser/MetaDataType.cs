using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Opportunity.LrcParser
{
    /// <summary>
    /// Type of metadata.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay(@"{Tag}")]
    public abstract class MetaDataType : IEquatable<MetaDataType>
    {
        private static char[] invalidChars = "]:".ToCharArray();

        /// <summary>
        /// Create new instance of <see cref="MetaDataType"/>.
        /// </summary>
        /// <param name="tag">Tag of metadata.</param>
        /// <param name="dataType">Data type of metadata.</param>
        /// <exception cref="ArgumentNullException"><paramref name="tag"/> or <paramref name="dataType"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="tag"/> contains invalid character.</exception>
        protected MetaDataType(string tag, Type dataType)
            : this(tag, dataType, false) { }

        internal MetaDataType(string tag, Type dataType, bool isSafe)
        {
            if (!isSafe)
            {
                tag = checkTag(tag);
            }
            this.DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
            this.Tag = tag;
        }

        private static string checkTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                throw new ArgumentNullException(tag);
            Helper.CheckString(nameof(tag), tag, invalidChars);
            tag = tag.Trim();
            return tag;
        }

        /// <summary>
        /// Tag of metadata.
        /// </summary>
        public string Tag { get; }

        /// <inheritdoc/>
        public override string ToString() => Tag;

        /// <summary>
        /// Parse metadata content string.
        /// </summary>
        /// <param name="mataDataContent">Metadata content string.</param>
        /// <returns>Parsed metadata content, should be of <see cref="DataType"/>.</returns>
        protected internal abstract object Parse(string mataDataContent);

        /// <summary>
        /// Convert metadata content to string.
        /// </summary>
        /// <param name="mataDataContent">Metadata content of <see cref="DataType"/>.</param>
        /// <returns>String representation of <paramref name="mataDataContent"/>.</returns>
        protected internal virtual string Stringify(object mataDataContent)
            => mataDataContent?.ToString();

        /// <summary>
        /// Data type of metadata.
        /// </summary>
        public Type DataType { get; }

        /// <summary>
        /// Default value of metadata.
        /// </summary>
        public virtual object Default => Activator.CreateInstance(DataType);

        /// <summary>
        /// Create new instance of <see cref="MetaDataType"/>, if <paramref name="tag"/> is known, value from <see cref="PreDefined"/> will be returned.
        /// </summary>
        /// <param name="tag">Tag of metadata.</param>
        /// <returns>New instance of <see cref="MetaDataType"/>, or instance from <see cref="PreDefined"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tag"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="tag"/> contains invalid character.</exception>
        public static MetaDataType Create(string tag)
        {
            tag = checkTag(tag);
            if (PreDefined.TryGetValue(tag, out var r))
                return r;
            return new NoValidateMetaDataType(tag);
        }

        /// <summary>
        /// Create new instance of <see cref="MetaDataType"/>, if <paramref name="tag"/> is known, value from <see cref="PreDefined"/> will be returned.
        /// </summary>
        /// <param name="tag">Tag of metadata.</param>
        /// <param name="parser">Parser for <see cref="Parse(string)"/> method.</param>
        /// <returns>New instance of <see cref="MetaDataType"/>, or instance from <see cref="PreDefined"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tag"/> or <paramref name="parser"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="tag"/> contains invalid character.</exception>
        /// <remarks>
        /// If instance from <see cref="PreDefined"/> is returned, <paramref name="parser"/> will be ignored.
        /// </remarks>
        public static MetaDataType Create<T>(string tag, Func<string, T> parser)
            => Create(tag, parser, null, default);

        /// <summary>
        /// Create new instance of <see cref="MetaDataType"/>, if <paramref name="tag"/> is known, value from <see cref="PreDefined"/> will be returned.
        /// </summary>
        /// <param name="tag">Tag of metadata.</param>
        /// <param name="parser">Parser for <see cref="Parse(string)"/> method.</param>
        /// <param name="defaultValue">Default value of metadata.</param>
        /// <returns>New instance of <see cref="MetaDataType"/>, or instance from <see cref="PreDefined"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tag"/> or <paramref name="parser"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="tag"/> contains invalid character.</exception>
        /// <remarks>
        /// If instance from <see cref="PreDefined"/> is returned, <paramref name="parser"/> will be ignored.
        /// </remarks>
        public static MetaDataType Create<T>(string tag, Func<string, T> parser, T defaultValue)
            => Create(tag, parser, null, defaultValue);

        /// <summary>
        /// Create new instance of <see cref="MetaDataType"/>, if <paramref name="tag"/> is known, value from <see cref="PreDefined"/> will be returned.
        /// </summary>
        /// <param name="tag">Tag of metadata.</param>
        /// <param name="parser">Parser for <see cref="Parse(string)"/> method.</param>
        /// <param name="stringifier">Stringifier for <see cref="Stringify(object)"/> method.</param>
        /// <returns>New instance of <see cref="MetaDataType"/>, or instance from <see cref="PreDefined"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tag"/> or <paramref name="parser"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="tag"/> contains invalid character.</exception>
        /// <remarks>
        /// If instance from <see cref="PreDefined"/> is returned, <paramref name="parser"/> and <paramref name="stringifier"/> will be ignored.
        /// </remarks>
        public static MetaDataType Create<T>(string tag, Func<string, T> parser, Func<T, string> stringifier)
            => Create(tag, parser, stringifier, default);

        /// <summary>
        /// Create new instance of <see cref="MetaDataType"/>, if <paramref name="tag"/> is known, value from <see cref="PreDefined"/> will be returned.
        /// </summary>
        /// <param name="tag">Tag of metadata.</param>
        /// <param name="parser">Parser for <see cref="Parse(string)"/> method.</param>
        /// <param name="stringifier">Stringifier for <see cref="Stringify(object)"/> method.</param>
        /// <param name="defaultValue">Default value of metadata.</param>
        /// <returns>New instance of <see cref="MetaDataType"/>, or instance from <see cref="PreDefined"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tag"/> or <paramref name="parser"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="tag"/> contains invalid character.</exception>
        /// <remarks>
        /// If instance from <see cref="PreDefined"/> is returned, <paramref name="parser"/> and <paramref name="stringifier"/> will be ignored.
        /// </remarks>
        public static MetaDataType Create<T>(string tag, Func<string, T> parser, Func<T, string> stringifier, T defaultValue)
        {
            if (parser is null)
                throw new ArgumentNullException(nameof(parser));
            tag = checkTag(tag);
            if (PreDefined.TryGetValue(tag, out var r))
                return r;
            return new DelegateMetaDataType<T>(tag, parser, stringifier, defaultValue);
        }

        /// <inheritdoc/>
        public bool Equals(MetaDataType other) => this.Tag.Equals(other?.Tag);

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is MetaDataType dt ? Equals(dt) : false;

        /// <inheritdoc/>
        public override int GetHashCode() => Tag.GetHashCode();

        private sealed class NoValidateMetaDataType : MetaDataType
        {
            public NoValidateMetaDataType(string tag)
                : base(tag, typeof(string), true) { }

            protected internal override object Parse(string mataDataContent) => mataDataContent;

            public override object Default => "";
        }

        private sealed class DelegateMetaDataType<T> : MetaDataType
        {
            public DelegateMetaDataType(string tag, Func<string, T> parser, Func<T, string> stringifier, T def)
                : base(tag, typeof(T), true)
            {
                this.parser = parser;
                this.stringifier = stringifier;
                this.def = def;
            }

            private readonly Func<string, T> parser;
            private readonly Func<T, string> stringifier;
            private readonly T def;

            public override object Default => this.def;

            protected internal override object Parse(string mataDataContent)
            {
                return this.parser(mataDataContent);
            }

            protected internal override string Stringify(object mataDataContent)
            {
                if (mataDataContent is T data && this.stringifier is Func<T, string> func)
                    return func(data);
                return base.Stringify(mataDataContent);
            }
        }

        #region Pre-defined
        /// <summary>
        /// Pre-defined <see cref="MetaDataType"/>.
        /// </summary>
        public static IReadOnlyDictionary<string, MetaDataType> PreDefined { get; }
            = new ReadOnlyDictionary<string, MetaDataType>(new Dictionary<string, MetaDataType>(StringComparer.OrdinalIgnoreCase)
            {
                ["ar"] = new NoValidateMetaDataType("ar"),
                ["al"] = new NoValidateMetaDataType("al"),
                ["ti"] = new NoValidateMetaDataType("ti"),
                ["au"] = new NoValidateMetaDataType("au"),
                ["by"] = new NoValidateMetaDataType("by"),
                ["offset"] = new DelegateMetaDataType<TimeSpan>("offset", v => TimeSpan.FromTicks((long)(double.Parse(v, System.Globalization.NumberStyles.Any) * 10000)), ts => ts.TotalMilliseconds.ToString("+0.#;-0.#"), default),
                ["re"] = new NoValidateMetaDataType("re"),
                ["ve"] = new NoValidateMetaDataType("ve"),
                ["length"] = new DelegateMetaDataType<DateTime>("length", v =>
                {
                    if (DateTimeExtension.TryParseLrcString(v, 0, v.Length, out var r))
                        return r;
                    throw new ArgumentException("Invalid length string.");
                }, d => d.ToTimestamp().ToLrcStringShort(), default),
            });

        /// <summary>
        /// Lyrics artist, "ar" field of ID Tags.
        /// </summary>
        public static MetaDataType Artist => PreDefined["ar"];
        /// <summary>
        /// Album where the song is from, "al" field of ID Tags.
        /// </summary>
        public static MetaDataType Album => PreDefined["al"];
        /// <summary>
        /// Lyrics(song) title, "ti" field of ID Tags.
        /// </summary>
        public static MetaDataType Title => PreDefined["ti"];
        /// <summary>
        /// Creator of the songtext, "au" field of ID Tags.
        /// </summary>
        public static MetaDataType Author => PreDefined["au"];
        /// <summary>
        /// Creator of the LRC file, "by" field of ID Tags.
        /// </summary>
        public static MetaDataType Creator => PreDefined["by"];
        /// <summary>
        /// Overall timestamp adjustment, "offset" field of ID Tags.
        /// </summary>
        public static MetaDataType Offset => PreDefined["offset"];
        /// <summary>
        /// The player or editor that created the LRC file, "re" field of ID Tags.
        /// </summary>
        public static MetaDataType Editor => PreDefined["re"];
        /// <summary>
        /// Version of program, "ve" field of ID Tags.
        /// </summary>
        public static MetaDataType Version => PreDefined["ve"];
        /// <summary>
        /// Length of song, "length" field of ID Tags.
        /// </summary>
        public static MetaDataType Length => PreDefined["length"];
        #endregion Pre-defined
    }
}
