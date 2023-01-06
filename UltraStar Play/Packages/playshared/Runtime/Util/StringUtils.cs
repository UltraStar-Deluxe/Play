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
}
