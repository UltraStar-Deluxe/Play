using System.Text;

public class UltraStarSongVoicesParserConfig
{
    public Encoding Encoding { get; set; }
    public bool UseUniversalCharsetDetector { get; set; } = true;

    /**
     * Whether the UltraStar song uses relative beat positions.
     */
    public bool IsRelativeSongFormat { get; set; }
}
