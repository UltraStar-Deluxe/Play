using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class CurlyBracePlaceholderMatcher
{
    public string Pattern { get; private set; }
    private readonly Regex regex;
    private readonly List<string> placeholderNames;
    
    public CurlyBracePlaceholderMatcher(string pattern)
    {
        Pattern = pattern;
        
        MatchCollection placeholderMatches = Regex.Matches(pattern, @"\{[^/]+\}");
        string patternNoCurlyBraces = pattern
            .Replace("{", "CURLY_OPEN")
            .Replace("}", "CURLY_CLOSE");
        string regexPattern = Regex.Escape(patternNoCurlyBraces);
        
        placeholderNames = new List<string>(placeholderMatches.Count);
        foreach(Match match in placeholderMatches)
        {
            // Remove curly braces from the placeholder name
            string placeholderName = match.Value.Substring(1, match.Length - 2);
            placeholderNames.Add(placeholderName);
            // Replace the placeholder with a match group
            regexPattern = regexPattern.Replace("CURLY_OPEN" + placeholderName + "CURLY_CLOSE", "(?<" + placeholderName + ">[^/]+)");
        }

        regex = new Regex(regexPattern);
    }

    public bool TryMatch(string text, out Dictionary<string, string> placeholderValues)
    {
        Match match = regex.Match(text);
        if (!match.Success)
        {
            Debug.Log("No match");
            placeholderValues = null;
            return false;
        }
        
        // Start from group 1 because group 0 is the complete match.
        placeholderValues = new Dictionary<string, string>();
        for (int i = 1; i < match.Groups.Count; i++)
        {
            Group group = match.Groups[i];
            placeholderValues[group.Name] = group.Value;
        }
        return true;
    }

    public int GetPlaceholderCount()
    {
        return placeholderNames.Count;
    }
}
