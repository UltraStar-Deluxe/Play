using System.Text;

public static class StringBuilderExtensions
{
    /**
     * Appends a line via LF character.
     * In contrast, StringBuilder.AppendLine method depends on the operating system.
     */
    public static void AppendLineFeed(this StringBuilder sb, string line)
    {
        sb.Append(line);
        sb.Append("\n");
    }
}
