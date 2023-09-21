public class SongSearchParameters
{
    public string SearchText { get; private set; }

    public SongSearchParameters(string searchText)
    {
        SearchText = searchText;
    }
}
