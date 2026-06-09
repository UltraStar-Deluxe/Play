using System;
using System.Collections.Generic;

public class WordTimestampPadder
{
    public double StartPaddingMs { get; set; }
    public double EndPaddingMs { get; set; }
    public double? WordLengthFilterMs { get; set; }
    public double? MaxEndTimeMs { get; set; }

    public WordTimestampPadder(double startPaddingMs = 0, double endPaddingMs = 0, double? wordLengthFilterMs = null,
        double? maxEndTimeMs = null)
    {
        StartPaddingMs = startPaddingMs;
        EndPaddingMs = endPaddingMs;
        WordLengthFilterMs = wordLengthFilterMs;
        MaxEndTimeMs = maxEndTimeMs;
    }

    public NemoForcedAligner.ForcedAlignmentResult PadTimestamps(NemoForcedAligner.ForcedAlignmentResult alignment)
    {
        if (alignment == null)
        {
            return null;
        }

        if (alignment.Words == null || alignment.Words.Count == 0 || (StartPaddingMs == 0 && EndPaddingMs == 0))
        {
            return alignment.Clone();
        }

        var clonedAlignment = alignment.Clone();
        var words = clonedAlignment.Words;
        int n = words.Count;

        double startPaddingSec = StartPaddingMs / 1000.0;
        double endPaddingSec = EndPaddingMs / 1000.0;

        var shouldPad = GetShouldPadArray(words);

        PadFirstWordStart(words[0], shouldPad[0], startPaddingSec);
        PadGaps(words, shouldPad, startPaddingSec, endPaddingSec);
        PadLastWordEnd(words[n - 1], shouldPad[n - 1], endPaddingSec);

        return clonedAlignment;
    }

    private bool[] GetShouldPadArray(List<NemoForcedAligner.WordTimestamp> words)
    {
        var shouldPad = new bool[words.Count];
        for (int i = 0; i < words.Count; i++)
        {
            double durationMs = (words[i].EndTime - words[i].StartTime) * 1000.0;
            shouldPad[i] = !WordLengthFilterMs.HasValue || durationMs < WordLengthFilterMs.Value;
        }

        return shouldPad;
    }

    private void PadFirstWordStart(NemoForcedAligner.WordTimestamp word, bool shouldPad, double paddingSec)
    {
        if (shouldPad)
        {
            double availableStart = word.StartTime;
            double actualPadding = Math.Min(paddingSec, availableStart);
            word.StartTime -= actualPadding;
            UpdateFirstTokenStart(word);
        }
    }

    private void PadGaps(List<NemoForcedAligner.WordTimestamp> words, bool[] shouldPad, double startPaddingSec,
        double endPaddingSec)
    {
        for (int i = 0; i < words.Count - 1; i++)
        {
            double currentEnd = words[i].EndTime;
            double nextStart = words[i + 1].StartTime;
            double gap = Math.Max(0, nextStart - currentEnd);

            double requestedEndPadding = shouldPad[i] ? endPaddingSec : 0;
            double requestedStartPadding = shouldPad[i + 1] ? startPaddingSec : 0;

            double totalRequested = requestedEndPadding + requestedStartPadding;

            if (totalRequested > 0)
            {
                double actualEndPadding;
                double actualStartPadding;

                if (totalRequested <= gap)
                {
                    actualEndPadding = requestedEndPadding;
                    actualStartPadding = requestedStartPadding;
                }
                else
                {
                    double factor = gap / totalRequested;
                    actualEndPadding = requestedEndPadding * factor;
                    actualStartPadding = requestedStartPadding * factor;
                }

                words[i].EndTime += actualEndPadding;
                words[i + 1].StartTime -= actualStartPadding;

                UpdateLastTokenEnd(words[i]);
                UpdateFirstTokenStart(words[i + 1]);
            }
        }
    }

    private void PadLastWordEnd(NemoForcedAligner.WordTimestamp word, bool shouldPad, double paddingSec)
    {
        if (shouldPad)
        {
            double actualPadding = paddingSec;
            if (MaxEndTimeMs.HasValue)
            {
                double maxEndSec = MaxEndTimeMs.Value / 1000.0;
                double available = Math.Max(0, maxEndSec - word.EndTime);
                actualPadding = Math.Min(actualPadding, available);
            }

            word.EndTime += actualPadding;
            UpdateLastTokenEnd(word);
        }
    }

    private void UpdateFirstTokenStart(NemoForcedAligner.WordTimestamp word)
    {
        if (word.Tokens != null && word.Tokens.Count > 0)
        {
            word.Tokens[0].StartTime = word.StartTime;
        }
    }

    private void UpdateLastTokenEnd(NemoForcedAligner.WordTimestamp word)
    {
        if (word.Tokens != null && word.Tokens.Count > 0)
        {
            word.Tokens[word.Tokens.Count - 1].EndTime = word.EndTime;
        }
    }
}
