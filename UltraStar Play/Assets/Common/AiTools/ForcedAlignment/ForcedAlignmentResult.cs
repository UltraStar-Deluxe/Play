using System.Collections.Generic;

public class ForcedAlignmentResult
{
    public List<WordTimestamp> Words { get; set; } = new List<WordTimestamp>();
    public List<TokenTimestamp> Tokens { get; set; } = new List<TokenTimestamp>();
}

public class TokenTimestamp
{
    public string Token { get; set; }
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public override string ToString() => $"'{Token}': {StartTime:F2} - {EndTime:F2}";
}

public class WordTimestamp
{
    public string Word { get; set; }
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public List<TokenTimestamp> Tokens { get; set; } = new List<TokenTimestamp>();
    public override string ToString() => $"{Word}: {StartTime:F2} - {EndTime:F2} ({Tokens.Count} tokens)";
}
