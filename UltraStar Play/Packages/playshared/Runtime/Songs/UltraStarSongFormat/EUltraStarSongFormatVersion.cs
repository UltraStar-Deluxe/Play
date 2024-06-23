/**
 * Version for UltraStar songs as specified by https://usdx.eu/format/
 */
public enum EUltraStarSongFormatVersion
{
    Unknown = 0,

    /**
     * v1.0.0
     * Deprecates: ENCODING, RESOLUTION, RELATIVE, NOTESGAP
     *
     * Baseline of tags that most UltraStar games support in 2023.
     * Does not have an explicit VERSION field. Thus, files without explicit VERSION field are assumed to use this format.
     * NOTE: The ENCODING field was deprecated because it was of questionable help and often wrong.
     * Instead, the community decided that all files should be interpreted as UTF-8 (without BOM).
     */
    V100 = 100,

    /**
     * v1.1.0
     * Adds: VERSION, INSTRUMENTAL, VOCALS, TAGS, PROVIDEDBY
     * Changes: AUDIO instead of MP3
     * Deprecates: MEDLEYSTARTBEAT, MEDLEYENDBEAT (to be replaced with MEDLEYSTART and MEDLEYEND in ms)
     *
     * Adds explicit VERSION header field.
     * Comma separated values for selected header fields, e.g., GENRE, CREATOR, EDITION, LANGUAGE, TAGS
     */
    V110 = 110,

    /**
     * v1.2.0
     * Adds: AUDIOURL, COVERURL, BACKGROUNDURL, VIDEOURL, etc.
     *
     *
     */
    V120 = 120,

    /**
     * v2.0.0
     * ADDS: MEDLEYSTART, MEDLEYEND
     *
     * Unities time unites (using milliseconds), affects GAP, VIDEOGAP, START, PREVIEWSTART, MEDLEYSTART, etc.
     */
    V200 = 200,
}
