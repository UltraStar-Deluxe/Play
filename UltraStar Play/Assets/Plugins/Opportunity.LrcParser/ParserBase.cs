using System;
using System.Collections.Generic;
using System.Threading;

namespace Opportunity.LrcParser
{
    internal abstract class ParserBase<TLine> : IParseResult<TLine>
        where TLine : Line
    {
        protected static readonly char[] LINE_BREAKS = "\r\n\u0085\u2028\u2029".ToCharArray();

        public ParserBase(string data) => this.Data = data ?? "";

        protected readonly string Data;

        public readonly MetaDataDictionary MetaData = new MetaDataDictionary();
        public readonly LineCollection<TLine> Lines = new LineCollection<TLine>();

        private Lyrics<TLine> lyrics;
        Lyrics<TLine> IParseResult<TLine>.Lyrics => LazyInitializer.EnsureInitialized(ref this.lyrics, () => new Lyrics<TLine>(this));

        protected List<ParseException> Exceptions = new List<ParseException>();

        IReadOnlyList<ParseException> IParseResult<TLine>.Exceptions => this.Exceptions;
    }
}
