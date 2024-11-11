using System.IO;

namespace NHyphenator.Loaders
{
    public class FilePatternsLoader : IHyphenatePatternsLoader
    {
        private readonly string _exceptionsFilePath; 
        private readonly string _patternsFilePath;

        public FilePatternsLoader(string patternsFilePath, string exceptionsFilePath = null)
        {
            _patternsFilePath = patternsFilePath;
            _exceptionsFilePath = exceptionsFilePath;
        }

        public string LoadPatterns() => File.ReadAllText(_patternsFilePath);

        public string LoadExceptions() => _exceptionsFilePath == null ? null : File.ReadAllText(_exceptionsFilePath);
    }
}