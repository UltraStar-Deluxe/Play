using System.Text;

public class UltraStarSongParserConfig
{
    public Encoding Encoding { get; set; }
    public bool UseUniversalCharsetDetector { get; set; } = true;

    public bool LogIssues { get; set; } = true;

    public UltraStarSongParserConfig()
    {
    }

    public UltraStarSongParserConfig(UltraStarSongParserConfig other)
    {
        Encoding = other.Encoding;
        UseUniversalCharsetDetector = other.UseUniversalCharsetDetector;
        LogIssues = other.LogIssues;
    }
}
