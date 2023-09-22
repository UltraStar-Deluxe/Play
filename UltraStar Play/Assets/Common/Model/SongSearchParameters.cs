public class SongRepositorySearchParameters
{
    public string SearchText { get; private set; }

    public SongRepositorySearchParameters(string searchText)
    {
        SearchText = searchText;
    }
}
