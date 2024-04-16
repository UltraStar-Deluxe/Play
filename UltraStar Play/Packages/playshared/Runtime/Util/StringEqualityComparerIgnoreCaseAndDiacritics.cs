using System.Collections.Generic;

public class StringEqualityComparerIgnoreCaseAndDiacritics : IEqualityComparer<string>
{
    public bool Equals(string x, string y)
    {
        return StringUtils.EqualsIgnoreCaseAndDiacritics(x, y);
    }

    public int GetHashCode(string obj)
    {
        return StringUtils.RemoveDiacritics(obj).GetHashCode();
    }
}
