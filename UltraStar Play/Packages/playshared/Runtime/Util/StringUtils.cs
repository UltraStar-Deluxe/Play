using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

public static class StringUtils
{
    private static readonly Regex whitespaceRegex = new(@"^\s+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool StartsWithIgnoreCaseAndDiacritics(string a, string b)
    {
        if (a == null || b == null)
        {
            return false;
        }

        string aNoDiacritics = RemoveDiacritics(a);
        string bNoDiacritics = RemoveDiacritics(b);
        return aNoDiacritics.StartsWith(bNoDiacritics, StringComparison.InvariantCultureIgnoreCase);
    }

    public static bool EndsWithIgnoreCaseAndDiacritics(string a, string b)
    {
        if (a == null || b == null)
        {
            return false;
        }

        string aNoDiacritics = RemoveDiacritics(a);
        string bNoDiacritics = RemoveDiacritics(b);
        return aNoDiacritics.EndsWith(bNoDiacritics, StringComparison.InvariantCultureIgnoreCase);
    }

    public static bool ContainsIgnoreCaseAndDiacritics(string a, string b)
    {
        if (a == null || b == null)
        {
            return false;
        }

        string aNoDiacritics = RemoveDiacritics(a);
        string bNoDiacritics = RemoveDiacritics(b);
        return aNoDiacritics.Contains(bNoDiacritics, StringComparison.InvariantCultureIgnoreCase);
    }

    public static bool EqualsIgnoreCaseAndDiacritics(string a, string b)
    {
        if (a == null && b == null)
        {
            return true;
        }
        else if (a == null || b == null)
        {
            return false;
        }

        string aNoDiacritics = RemoveDiacritics(a);
        string bNoDiacritics = RemoveDiacritics(b);
        return string.Equals(aNoDiacritics, bNoDiacritics, StringComparison.InvariantCultureIgnoreCase);
    }

    public static string RemoveDiacritics(string text)
    {
        if (text == null)
        {
            return null;
        }

        // Solution from https://stackoverflow.com/a/368850/4412885
        string formD = text.Normalize(NormalizationForm.FormD);
        StringBuilder sb = new StringBuilder();

        foreach (char ch in formD)
        {
            UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string ReplaceInvalidChars(string text, char replacement, HashSet<char> invalidCharacters)
    {
        StringBuilder sb = new();
        foreach (char c in text)
        {
            if (invalidCharacters.Contains(c))
            {
                sb.Append(replacement);
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    public static int CountOccurrencesInString(string source, string toFind, bool ignoreCase = false)
    {
        if (source.IsNullOrEmpty()
            || toFind.IsNullOrEmpty())
        {
            return 0;
        }

        StringComparison comparison = ignoreCase
            ? StringComparison.InvariantCultureIgnoreCase
            : StringComparison.InvariantCulture;

        int count = 0;

        // The length of the string is an upper bound for the count.
        // Thus, it can be used as iteration limit here to avoid an endless loop.
        int searchStartIndex = 0;
        for (int i = 0; i < source.Length; i++)
        {
            int occurrenceIndex = source.IndexOf(toFind, searchStartIndex, comparison);
            if (occurrenceIndex >= 0)
            {
                count++;
                searchStartIndex = occurrenceIndex + 1;
            }
            else
            {
                return count;
            }
        }

        return count;
    }

    public static string EscapeLineBreaks(string text)
    {
        if (text.IsNullOrEmpty())
        {
            return text;
        }

        // return Regex.Replace(text, "\n", "\\n", RegexOptions.Multiline);
        return text
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }

    public static bool IsOnlyWhitespace(string newText)
    {
        return string.IsNullOrEmpty(newText) || whitespaceRegex.IsMatch(newText);
    }

    public static string ToTitleCase(string input)
    {
        if (input.IsNullOrEmpty())
        {
            return input;
        }

        if (input.Contains("_") || input.Contains("-"))
        {
            return SnakeCaseToTitleCase(input);
        }

        return CamelCaseToTitleCase(input);
    }

    /**
     * Returns a display name from a snake_case and dash-case text.
     * Example: "this_is_an_example" and "this-is-an-example" become "This Is An Example".
     */
    public static string SnakeCaseToTitleCase(string input)
    {
        if (input.IsNullOrEmpty())
        {
            return input;
        }

        input = input
            .Replace("_", " ")
            .Replace("-", " ");
        char[] chars = input.ToCharArray();
        bool lastWasSpace = true;
        for (int c = 0; c < chars.Length; c++)
        {
            if (lastWasSpace)
            {
                chars[c] = char.ToUpperInvariant(input[c]);
            }
            lastWasSpace = char.IsWhiteSpace(chars[c]);
        }
        return new string(chars);
    }

    /**
     * Returns a display name from a PascalCase and camelCase text.
     * Example: "ThisIsAnExample" and "thisIsAnExample" become "This Is An Example".
     */
    public static string CamelCaseToTitleCase(string input)
    {
        if (input.IsNullOrEmpty())
        {
            return input;
        }

        string withFirstLetterUppercase = input.ToUpperInvariantFirstChar();
        string inputWithSpaces = Regex.Replace(withFirstLetterUppercase, @"([A-Z])", " $1");
        return inputWithSpaces.Trim();
    }

    public static string AddLeadingZeros(int number, int targetLength)
    {
        return string.Format($"{{0:D{targetLength}}}", number);
    }

    public static int MinIndexOf(string text, int startIndex, params char[] characters)
    {
        int minIndex = -1;
        foreach (char character in characters)
        {
            int i = text.IndexOf(character, startIndex);
            if (i >= 0
                && (i < minIndex || minIndex < 0))
            {
                minIndex = i;
            }
        }

        return minIndex;
    }
}
