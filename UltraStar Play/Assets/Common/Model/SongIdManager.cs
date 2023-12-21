using System.Collections.Generic;
using UnityEngine;

public static class SongIdManager
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        songMetaToScoreRelevantHash.Clear();
        songMetaToGloballyUniqueHash.Clear();
        songMetaToLocallyUniqueHash.Clear();
        stringToMd5Hash.Clear();
    }

    private static readonly BiDictionary<SongMeta, string> songMetaToScoreRelevantHash = new();
    private static readonly BiDictionary<SongMeta, string> songMetaToGloballyUniqueHash = new();
    private static readonly BiDictionary<SongMeta, string> songMetaToLocallyUniqueHash = new();
    private static readonly Dictionary<string, string> stringToMd5Hash = new();

    public static void ClearSongIds(SongMeta songMeta)
    {
        songMetaToScoreRelevantHash.RemoveByFirst(songMeta);
        songMetaToGloballyUniqueHash.RemoveByFirst(songMeta);
        songMetaToLocallyUniqueHash.RemoveByFirst(songMeta);
    }

    public static string GetAndCacheScoreRelevantId(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return "";
        }

        if (songMetaToScoreRelevantHash.TryGetByFirst(songMeta, out string scoreRelevantHash))
        {
            return scoreRelevantHash;
        }

        scoreRelevantHash = SongMetaUtils.ComputeScoreRelevantSongHash(songMeta);
        songMetaToScoreRelevantHash.Set(songMeta, scoreRelevantHash);
        return scoreRelevantHash;
    }

    /**
     * Returns a hash that identifies this song globally, e.g.,
     * for online multiplayer.
     */
    public static string GetAndCacheGloballyUniqueId(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return "";
        }

        if (songMetaToGloballyUniqueHash.TryGetByFirst(songMeta, out string cachedHash))
        {
            return cachedHash;
        }

        // Prefix with artist and title for an efficient check whether a song may equal the hash.
        string artistAndTitleHash = GetArtistAndTitleHash(songMeta);
        string computedHash = SongMetaUtils.ComputeUniqueSongHash(songMeta);
        string hashPrefixedWithArtistAndTitle = $"{artistAndTitleHash}:{computedHash}";

        songMetaToGloballyUniqueHash.Set(songMeta, hashPrefixedWithArtistAndTitle);
        return hashPrefixedWithArtistAndTitle;
    }

    /**
     * Returns a hash that identifies this song on this local machine, e.g.,
     * for use with the Companion App.
     */
    public static string GetAndCacheLocallyUniqueId(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return "";
        }

        if (songMetaToLocallyUniqueHash.TryGetByFirst(songMeta, out string cachedHash))
        {
            return cachedHash;
        }

        // Prefix with artist and title for an efficient check whether a song may equal the hash.
        string artistAndTitleHash = GetArtistAndTitleHash(songMeta);
        string computedHash = songMeta.FileInfo != null
            // Using the file path is faster because is does not require to read the file content.
            // However, the file path can only be used to identify the song locally on this machine.
            ? HashingUtils.Md5Hash(songMeta.FileInfo.FullName)
            : SongMetaUtils.ComputeUniqueSongHash(songMeta);
        string hashPrefixedWithArtistAndTitle = $"{artistAndTitleHash}:{computedHash}";

        songMetaToLocallyUniqueHash.Set(songMeta, hashPrefixedWithArtistAndTitle);
        return hashPrefixedWithArtistAndTitle;
    }

    public static bool SongMetaMatchesGloballyUniqueSongId(SongMeta songMeta, string songId)
    {
        if (songMetaToGloballyUniqueHash.TryGetBySecond(songId, out SongMeta _))
        {
            return true;
        }

        // Efficient check whether this song may equal the full hash.
        string artistAndTitleHash = GetArtistAndTitleHash(songMeta);
        if (!songId.StartsWith(artistAndTitleHash))
        {
            // The artist and title did not match.
            // Thus, the rest of the hash cannot match, so we don't need to compute the full hash.
            return false;
        }

        return GetAndCacheGloballyUniqueId(songMeta) == songId;
    }

    public static bool SongMetaMatchesLocallyUniqueSongId(SongMeta songMeta, string songId)
    {
        if (songMetaToLocallyUniqueHash.TryGetBySecond(songId, out SongMeta _))
        {
            return true;
        }

        // Efficient check whether this song may equal the full hash.
        string artistAndTitleHash = GetArtistAndTitleHash(songMeta);
        if (!songId.StartsWith(artistAndTitleHash))
        {
            // The artist and title did not match.
            // Thus, the rest of the hash cannot match, so we don't need to compute the full hash.
            return false;
        }

        return GetAndCacheLocallyUniqueId(songMeta) == songId;
    }

    private static string GetArtistAndTitleHash(SongMeta songMeta)
    {
        string artistAndTitle = SongMetaUtils.GetArtistAndTitle(songMeta, ":");
        if (stringToMd5Hash.TryGetValue(artistAndTitle, out string cachedHash))
        {
            return cachedHash;
        }

        string computedHash = HashingUtils.Md5Hash(artistAndTitle);
        stringToMd5Hash[artistAndTitle] = computedHash;
        return computedHash;
    }

    public static bool TryGetSongMetaByGloballyUniqueId(string songId, out SongMeta songMeta)
    {
        return songMetaToGloballyUniqueHash.TryGetBySecond(songId, out songMeta);
    }

    public static bool TryGetSongMetaByLocallyUniqueId(string songId, out SongMeta songMeta)
    {
        return songMetaToLocallyUniqueHash.TryGetBySecond(songId, out songMeta);
    }
}
