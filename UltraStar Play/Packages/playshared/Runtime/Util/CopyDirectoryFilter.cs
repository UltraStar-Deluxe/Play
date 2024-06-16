using System;

public class CopyDirectoryFilter
{
    public Func<string, bool> IsIncluded { get; private set; }
    public Func<string, bool> IsExcluded { get; private set; }

    public CopyDirectoryFilter(Func<string, bool> isIncluded, Func<string, bool> isExcluded)
    {
        IsIncluded = isIncluded;
        IsExcluded = isExcluded;
    }

    public static CopyDirectoryFilter Include(Func<string, bool> predicate)
    {
        return new CopyDirectoryFilter(predicate, null);
    }

    public static CopyDirectoryFilter Exclude(Func<string, bool> predicate)
    {
        return new CopyDirectoryFilter(null, predicate);
    }
}
