using System.Collections.Generic;

public static class CollectionUtils
{
    public static void SafeSet<T>(IList<T> list, T value, int index)
    {
        if (list == null
            || index < 0
            || index >= list.Count)
        {
            return;
        }

        list[index] = value;
    }

    public static T SafeGet<T>(IReadOnlyList<T> list, int index, T fallbackValue)
    {
        if (list == null
            || index < 0
            || index >= list.Count)
        {
            return fallbackValue;
        }

        return list[index];
    }
}
