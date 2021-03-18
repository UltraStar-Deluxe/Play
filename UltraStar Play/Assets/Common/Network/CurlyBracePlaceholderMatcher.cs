using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class CurlyBracePlaceholderMatcher
{
    private Regex regex;
    private List<string> placeholderNames;
    
    public CurlyBracePlaceholderMatcher(string pattern)
    {
        string regexPattern = pattern;
        MatchCollection placeholderMatches = Regex.Matches(pattern, @"\{[^/]+\}");
        placeholderNames = new List<string>(placeholderMatches.Count);
        foreach(Match match in placeholderMatches)
        {
            // Remove curly braces from the placeholder name
            string placeholderName = match.Value.Substring(1, match.Length - 2);
            placeholderNames.Add(placeholderName);
            regexPattern = regexPattern.Replace(match.Value, "(?<" + placeholderName + ">[^/]+)");
        }

        Debug.Log(regexPattern);
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
        
        // Start from group 1 because group 0 is the complete text.
        placeholderValues = new Dictionary<string, string>();
        for (int i = 1; i < match.Groups.Count; i++)
        {
            Group group = match.Groups[i];
            Debug.Log($"placeholder: {group.Name}, value: {group.Value}");
            placeholderValues[group.Name] = group.Value;
        }
        return true;
    }

    public int GetPlaceholderCount()
    {
        return placeholderNames.Count;
    }
}
