using System;
using System.Collections.Generic;
using System.Text;

namespace Opportunity.LrcParser
{
    internal sealed class Parser<TLine> : ParserBase<TLine>
        where TLine : Line, new()
    {
        private int currentPosition = 0;

        public Parser(string data) : base(data) { }

        private void skipWhitespaces()
        {
            for (; this.currentPosition < this.Data.Length; this.currentPosition++)
            {
                if (!char.IsWhiteSpace(this.Data[this.currentPosition]))
                {
                    break;
                }
            }
        }

        private void trimEnd(ref int end)
        {
            for (var i = end - 1; i >= this.currentPosition; i--)
            {
                if (!char.IsWhiteSpace(this.Data[i]))
                {
                    end = i + 1;
                    break;
                }
            }
        }

        private int readLine()
        {
            if (this.currentPosition >= this.Data.Length)
                return -1;
            this.currentPosition = this.Data.IndexOf('[', this.currentPosition);
            if (this.currentPosition < 0)
            {
                this.currentPosition = this.Data.Length;
                return -1;
            }
            var nextPosition = this.currentPosition + 1;
            if (nextPosition >= this.Data.Length)
                return nextPosition;

            nextPosition = this.Data.IndexOfAny(LINE_BREAKS, nextPosition);
            if (nextPosition < 0)
            {
                return this.Data.Length;
            }

            nextPosition = this.Data.IndexOf('[', nextPosition);
            if (nextPosition < 0)
            {
                return this.Data.Length;
            }
            return nextPosition;
        }

        private bool readTag(int next, out int tagStart, out int tagEnd)
        {
            tagStart = -1;
            tagEnd = -1;
            skipWhitespaces();
            var lbPos = this.currentPosition;
            if (lbPos >= next)
                return false; // empty range
            if (this.Data[lbPos] != '[')
                return false; // not a tag
            this.currentPosition++;
            skipWhitespaces();
            if (this.currentPosition >= next)
            {
                this.currentPosition = lbPos;
                return false; // ']' not found
            }
            tagStart = this.currentPosition;
            var rbPos = default(int);
            if (char.IsDigit(this.Data[tagStart]))
            {
                // timestamp
                rbPos = this.Data.IndexOf(']', tagStart, next - tagStart);
            }
            else
            {
                // ID tag
                rbPos = this.Data.LastIndexOf(']', next - 1, next - tagStart);
            }
            if (rbPos < 0)
            {
                this.currentPosition = lbPos;
                return false; // ']' not found
            }
            tagEnd = rbPos;
            trimEnd(ref tagEnd);
            this.currentPosition = rbPos + 1;
            return true;
        }

        private void analyzeLine(int next)
        {
            var current = this.currentPosition;
            var lineStart = this.Lines.Count;
            var isIdTagLine = true;
            // analyze tag of line
            while (true)
            {
                var oldPos = this.currentPosition;
                if (!readTag(next, out var tagStart, out var tagEnd))
                    break;
                if (DateTimeExtension.TryParseLrcString(this.Data, tagStart, tagEnd, out var time))
                {
                    this.Lines.Add(new TLine { InternalTimestamp = time });
                    isIdTagLine = false;
                    continue;
                }
                if (!isIdTagLine) // not a tag, id tag will not appear after a timestamp
                {
                    this.currentPosition = oldPos;
                    break;
                }

                var colum = this.Data.IndexOf(':', tagStart, tagEnd - tagStart);
                var mdt = colum < 0
                    ? MetaDataType.Create(this.Data.Substring(tagStart, tagEnd - tagStart))
                    : MetaDataType.Create(this.Data.Substring(tagStart, colum - tagStart));
                var mdc = colum < 0
                    ? ""
                    : this.Data.Substring(colum + 1, tagEnd - colum - 1);
                try
                {
                    this.MetaData[mdt] = mdt.Stringify(mdt.Parse(mdc));
                }
                catch (Exception ex)
                {
                    this.MetaData[mdt] = mdc;
                    this.Exceptions.Add(new ParseException(this.Data, colum < 0 ? tagStart : colum + 1, $"Failed to parse ID tag `{mdt}`", ex));
                }
            }

            // analyze content of line
            if (this.Lines.Count != lineStart)
            {
                skipWhitespaces();
                var end = next;
                trimEnd(ref end);
                var content = this.Data.Substring(this.currentPosition, end - this.currentPosition);
                for (var i = lineStart; i < this.Lines.Count; i++)
                {
                    this.Lines[i].Content = content;
                }
            }

            this.currentPosition = next;
        }

        public void Analyze()
        {
            while (true)
            {
                var nextPosition = readLine();
                if (nextPosition < 0)
                    return;
                analyzeLine(nextPosition);
            }
        }
    }
}
