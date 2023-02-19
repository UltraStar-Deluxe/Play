using System.Collections.Generic;
using System.IO;

public class M3UPlaylist : IPlaylist
{
    public string FilePath { get; private set; }

    public string FileName => Path.GetFileNameWithoutExtension(FilePath);
    public string Name => FileName;
    public virtual bool IsEmpty => audioFilePaths.Count == 0;
    
    private readonly HashSet<string> audioFilePaths = new();

    public M3UPlaylist(string filePath)
    {
        FilePath = filePath;
    }
    
    public bool HasSongEntry(SongMeta songMeta)
    {
        if (WebRequestUtils.IsHttpOrHttpsUri(songMeta.Mp3)
            || WebRequestUtils.IsNetworkPath(songMeta.Mp3))
        {
            return false;
        }

        string normalizedSongMetaAbsoluteAudioFilePath = new FileInfo(SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.Mp3)).FullName;
        
        return audioFilePaths.AnyMatch(audioFilePath =>
        {
            string audioFileAbsolutePath = GetAbsoluteAudioFilePath(audioFilePath);
            string normalizedAudioFileAbsolutePath = new FileInfo(audioFileAbsolutePath).FullName;
            return Equals(normalizedSongMetaAbsoluteAudioFilePath, normalizedAudioFileAbsolutePath);
        });
    }

    private string GetAbsoluteAudioFilePath(string audioFilePath)
    {
        if (PathUtils.IsAbsolutePath(audioFilePath))
        {
            return audioFilePath;
        }

        return Path.GetDirectoryName(FilePath) + $"/{audioFilePath}";
    }

    public void AddAudioFilePath(string audioFilePath)
    {
        if (audioFilePath.IsNullOrEmpty())
        {
            return;
        }
        audioFilePaths.Add(audioFilePath);
    }
}
