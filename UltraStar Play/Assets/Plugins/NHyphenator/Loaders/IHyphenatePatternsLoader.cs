namespace NHyphenator.Loaders
{
    public interface IHyphenatePatternsLoader
    {
        string LoadExceptions();
        string LoadPatterns();
    }
}