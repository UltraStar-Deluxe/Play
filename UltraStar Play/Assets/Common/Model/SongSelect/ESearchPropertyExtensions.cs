public static class SearchPropertyExtensions
{
    public static string TranslatedName(this ESearchProperty property)
    {
        switch (property)
        {
            case ESearchProperty.Artist: return Translation.Get(R.Messages.songProperty_artist);
            case ESearchProperty.Title: return Translation.Get(R.Messages.songProperty_title);
            case ESearchProperty.Language: return Translation.Get(R.Messages.songProperty_language);
            case ESearchProperty.Genre: return Translation.Get(R.Messages.songProperty_genre);
            case ESearchProperty.Tag: return Translation.Get(R.Messages.songProperty_tag);
            case ESearchProperty.Edition: return Translation.Get(R.Messages.songProperty_edition);
            case ESearchProperty.Year: return Translation.Get(R.Messages.songProperty_year);
            case ESearchProperty.Lyrics: return Translation.Get(R.Messages.songProperty_lyrics);
            default: return StringUtils.ToTitleCase(property.ToString());
        }
    }
}
