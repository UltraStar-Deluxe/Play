using System.Text.RegularExpressions;

public static class StringUtils
{
    public static string ToTitleCase(string input)
    {
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
