using System;
using static Opportunity.LrcParser.DateTimeExtension;

namespace Opportunity.LrcParser
{
    /// <summary>
    /// Factory for timestamps.
    /// </summary>
    public static class Timestamp
    {
        /// <summary>
        /// Create new <see cref="DateTime"/> of timestamp.
        /// </summary>
        /// <param name="minute">Minute, > 0.</param>
        /// <param name="second">Second, 0 ~ 59.</param>
        /// <param name="millisecond">Millisecond, 0 ~ 999.</param>
        /// <returns><see cref="DateTime"/> of timestamp.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Argument out of range.</exception>
        public static DateTime Create(int minute, int second, int millisecond)
        {
            if (minute < 0)
                throw new ArgumentOutOfRangeException(nameof(minute));
            if (unchecked((uint)second >= 60U))
                throw new ArgumentOutOfRangeException(nameof(second));
            if (unchecked((uint)millisecond >= 1000U))
                throw new ArgumentOutOfRangeException(nameof(millisecond));
            return new DateTime(minute * TICKS_PER_MINUTE
                + second * TICKS_PER_SECOND
                + millisecond * TICKS_PER_MILLISECOND);
        }

        /// <summary>
        /// Create new <see cref="DateTime"/> of timestamp.
        /// </summary>
        /// <param name="second">Second, > 0.</param>
        /// <param name="millisecond">Millisecond, 0 ~ 999.</param>
        /// <returns><see cref="DateTime"/> of timestamp.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Argument out of range.</exception>
        public static DateTime Create(int second, int millisecond)
        {
            if (second < 0)
                throw new ArgumentOutOfRangeException(nameof(second));
            if (unchecked((uint)millisecond >= 1000U))
                throw new ArgumentOutOfRangeException(nameof(millisecond));
            return new DateTime(second * TICKS_PER_SECOND
                + millisecond * TICKS_PER_MILLISECOND);
        }

        /// <summary>
        /// Create new <see cref="DateTime"/> of timestamp.
        /// </summary>
        /// <param name="millisecond">Millisecond, > 0.</param>
        /// <returns><see cref="DateTime"/> of timestamp.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Argument out of range.</exception>
        public static DateTime Create(int millisecond)
        {
            if (millisecond < 0)
                throw new ArgumentOutOfRangeException(nameof(millisecond));
            return new DateTime(millisecond * TICKS_PER_MILLISECOND);
        }
    }
}
