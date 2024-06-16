using System;
using System.Collections.Generic;
using System.Linq;
using NHyphenator.Loaders;
using UnityEngine;

/**
 * You can find the hyphenation patterns here:
 * https://github.com/hyphenation/tex-hyphen/tree/master/hyph-utf8/tex/generic/hyph-utf8/patterns/txt
 *
 * .pat.txt files contain patterns, .hyp.txt files contain exceptions
 */
public class HyphenationPatternsProvider : IHyphenatePatternsLoader
{
    private readonly string hyphenationPatternsText;
    private readonly string hyphenationExceptionsText;

    public HyphenationPatternsProvider(string hyphenationPatternsText, string hyphenationExceptionsText)
    {
        this.hyphenationPatternsText = hyphenationPatternsText;
        this.hyphenationExceptionsText = hyphenationExceptionsText;
    }

    public string LoadExceptions() => hyphenationExceptionsText;

    public string LoadPatterns() => hyphenationPatternsText;

    public static IHyphenatePatternsLoader CreateHyphenationPatternsLoader(string language)
    {
        string twoLetterCountryCode = LocaleInfoUtils.GetTwoLetterCountryCode(language);
        if (twoLetterCountryCode.IsNullOrEmpty())
        {
            return null;
        }

        // Hyphenation patterns from https://github.com/hyphenation/tex-hyphen/tree/master/hyph-utf8/tex/generic/hyph-utf8/patterns/txt
        TextAsset fileNamesTextAsset = Resources.Load<TextAsset>("HyphenationPatterns/HyphenationPatternFileNames");
        string fileNamesText = fileNamesTextAsset.text.Replace("\r\n", "\n");
        List<string> fileNames = fileNamesText
            .Split('\n')
            .Select(fileName => fileName.Trim())
            .ToList();

        string patternFileName = fileNames.FirstOrDefault(fileName =>
            fileName.EndsWith(".pat.txt")
            && (fileName.Contains($"-{twoLetterCountryCode}-")
                || fileName.Contains($"-{twoLetterCountryCode}.")));
        if (patternFileName.IsNullOrEmpty())
        {
            Debug.Log($"No pattern file found for language {language} two letter country code {twoLetterCountryCode}");
            return null;
        }
        Debug.Log($"Loading hyphenation patterns for language: {language}, two letter country code: {twoLetterCountryCode} from file {patternFileName}");

        string exceptionsFileName = fileNames.FirstOrDefault(fileName =>
            fileName.EndsWith(".hyp.txt")
            && (fileName.Contains($"-{twoLetterCountryCode}-")
                || fileName.Contains($"-{twoLetterCountryCode}.")));

        string patternsText = Resources.Load<TextAsset>(GetHyphenationPatternFilePathInResources(patternFileName))?.text;
        string exceptionsText = Resources.Load<TextAsset>(GetHyphenationPatternFilePathInResources(exceptionsFileName))?.text;
        return new HyphenationPatternsProvider(patternsText, exceptionsText);
    }

    private static string GetHyphenationPatternFilePathInResources(string fileName)
    {
        if (fileName.IsNullOrEmpty())
        {
            return "";
        }
        string fileNameWithoutTxt = fileName.Replace(".txt", "");
        return $"HyphenationPatterns/txt/{fileNameWithoutTxt}";
    }
}
