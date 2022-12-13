using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class EnglishSyllableSplitter
{
    private Dictionary<string, string[]> wordToSyllables;

    public List<string> GetSyllables(string word)
    {
        if (wordToSyllables == null)
        {
            InitWordToSyllablesMapping();
        }

        if (wordToSyllables.TryGetValue(word, out string[] syllables))
        {
            return syllables.ToList();
        }

        return new List<string> { word };
    }

    private void InitWordToSyllablesMapping()
    {
        wordToSyllables = new();
        TextAsset textAsset = Resources.Load<TextAsset>("25K-syllabified-sorted-alphabetically");

        // Read TextAsset line by line
        using StringReader reader = new(textAsset.text);
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            // Syllables are separated by semicolon in the TextAsset.
            string[] syllables = line.Split(";");
            string word = line.Replace(";", "");
            wordToSyllables.Add(word, syllables);
        }
    }
}
