using ProTrans;

public static class EnumExtensions
{
    public static string GetTranslatedName(this ESongOrder songOrder)
    {
        switch (songOrder)
        {
            case ESongOrder.Artist:
                return TranslationManager.GetTranslation(R.Messages.order_Artist);
            case ESongOrder.Title:
                return TranslationManager.GetTranslation(R.Messages.order_Title);
            case ESongOrder.Genre:
                return TranslationManager.GetTranslation(R.Messages.order_Genre);
            case ESongOrder.Language:
                return TranslationManager.GetTranslation(R.Messages.order_Language);
            case ESongOrder.Folder:
                return TranslationManager.GetTranslation(R.Messages.order_Folder);
            case ESongOrder.Year:
                return TranslationManager.GetTranslation(R.Messages.order_Year);
            case ESongOrder.Highscore:
                return TranslationManager.GetTranslation(R.Messages.order_Highscore);
            default:
                return TranslationManager.GetTranslation(R.Messages.order_Artist);
        }
    }
}
