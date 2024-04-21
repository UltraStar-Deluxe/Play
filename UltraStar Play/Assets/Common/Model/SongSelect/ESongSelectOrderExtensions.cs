public static class SongOrderExtensions
{
    public static string TranslatedName(this ESongOrder songOrder)
    {
        switch (songOrder)
        {
            case ESongOrder.Artist:
                return Translation.Get(R.Messages.order_Artist);
            case ESongOrder.Title:
                return Translation.Get(R.Messages.order_Title);
            case ESongOrder.Genre:
                return Translation.Get(R.Messages.order_Genre);
            case ESongOrder.Language:
                return Translation.Get(R.Messages.order_Language);
            case ESongOrder.Folder:
                return Translation.Get(R.Messages.order_Folder);
            case ESongOrder.Year:
                return Translation.Get(R.Messages.order_Year);
            case ESongOrder.LocalHighScore:
                return Translation.Get(R.Messages.order_Highscore);
            default:
                return Translation.Get(R.Messages.order_Artist);
        }
    }
}
