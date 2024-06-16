using System.IO;
using System.Linq;

public static class AudioFileMetaTagUtils
{
    public static bool TryGetArtist(string audioFile, out string artist)
    {
        // TODO: use https://github.com/Zeugma440/atldotnet to read tags from audio file
        artist = Path.GetFileNameWithoutExtension(audioFile)
            .Split("-")
            .FirstOrDefault()
            .Trim();
        return true;
    }

    public static bool TryGetTitle(string audioFile, out string title)
    {
        // TODO: use https://github.com/Zeugma440/atldotnet to read tags from audio file
        title = Path.GetFileNameWithoutExtension(audioFile)
            .Split("-")
            .LastOrDefault()
            .Trim();
        return true;
    }
}
