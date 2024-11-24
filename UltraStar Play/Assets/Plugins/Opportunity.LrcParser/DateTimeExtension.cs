using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Opportunity.LrcParser
{
    internal static class DateTimeExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string ToString(this DateTime dateTime, string mFormat, string smSep, string sFormat)
        {
            var t = dateTime.Ticks;
            var m = t / TICKS_PER_MINUTE;
            t -= m * TICKS_PER_MINUTE;
            var s = (double)t / TICKS_PER_SECOND;
            return m.ToString(mFormat) + smSep + s.ToString(sFormat);
        }

        public static string ToLrcString(this DateTime dateTime, int minDecimalDigits, int maxDecimalDigits)
            => dateTime.ToString("D2", ":", "00." + new string('0', minDecimalDigits) + new string('#', maxDecimalDigits - minDecimalDigits));

        public static string ToLrcString(this DateTime dateTime)
            => dateTime.ToString("D2", ":", "00.00");

        public static string ToLrcStringRaw(this DateTime dateTime)
            => dateTime.ToString("D2", ":", "00.00######");

        public static string ToLrcStringShort(this DateTime dateTime)
            => dateTime.ToString("D2", ":", "00");

        public static bool TryParseLrcString(string value, int start, int end, out DateTime result)
        {
            var m = 0;
            var s = 0;
            var t = 0;

            var i = start;
            for (; i < end; i++)
            {
                var v = value[i] - '0';
                if (v >= 0 && v <= 9)
                    m = m * 10 + v;
                else if (value[i] == ':')
                {
                    i++;
                    break;
                }
                else if (char.IsWhiteSpace(value, i))
                {
                    continue;
                }
                else
                {
                    goto ERROR;
                }
            }

            for (; i < end; i++)
            {
                var v = value[i] - '0';
                if (v >= 0 && v <= 9)
                    s = s * 10 + v;
                else if (value[i] == '.')
                {
                    i++;
                    break;
                }
                else if (char.IsWhiteSpace(value, i))
                {
                    continue;
                }
                else
                {
                    goto ERROR;
                }
            }

            var weight = (int)(TICKS_PER_SECOND / 10);
            for (; i < end; i++)
            {
                var v = value[i] - '0';
                if (v >= 0 && v <= 9)
                {
                    t += weight * v;
                    weight /= 10;
                }
                else if (char.IsWhiteSpace(value, i))
                {
                    continue;
                }
                else
                {
                    goto ERROR;
                }
            }

            result = new DateTime(t + TICKS_PER_SECOND * s + TICKS_PER_MINUTE * m, DateTimeKind.Unspecified);
            return true;

            ERROR:
            result = default;
            return false;
        }

        public const long TICKS_PER_MINUTE = TICKS_PER_SECOND * 60;
        public const long TICKS_PER_SECOND = TICKS_PER_MILLISECOND * 1000;
        public const long TICKS_PER_MILLISECOND = 10_000;

        private static DateTime ONE_YEAR = new DateTime(2, 1, 1, 0, 0, 0);

        /// <exception cref="ArgumentException">Kind of value should be DateTimeKind.Unspecified.</exception>
        public static DateTime ToTimestamp(this DateTime dateTime)
        {
            if (dateTime.Kind != DateTimeKind.Unspecified)
                throw new ArgumentException("Kind of value should be DateTimeKind.Unspecified");
            if (dateTime >= ONE_YEAR) //Auto correct.
                dateTime = new DateTime(dateTime.TimeOfDay.Ticks);
            return dateTime;
        }
    }
}
