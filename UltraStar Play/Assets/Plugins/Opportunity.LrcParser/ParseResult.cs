using System;
using System.Collections.Generic;
using System.Text;

namespace Opportunity.LrcParser
{
    /// <summary>
    /// Result of parsing.
    /// </summary>
    /// <typeparam name="TLine">Type of lyrics line.</typeparam>
    public interface IParseResult<TLine>
        where TLine : Line
    {
        /// <summary>
        /// Lyrcis of parsing result.
        /// </summary>
        Lyrics<TLine> Lyrics { get; }

        /// <summary>
        /// Exceptions during parsing.
        /// </summary>
        IReadOnlyList<ParseException> Exceptions { get; }
    }
}
