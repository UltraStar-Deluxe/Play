using System.Collections;
using System.Collections.Generic;

/**
 * Comparer that puts null and empty values last.
 */
public class NullOrEmptyValueLastComparer : IComparer<object>
{
    public int Compare(object x, object y)
    {
        if (IsNullOrEmpty(x) && IsNullOrEmpty(y))
        {
            return 0;
        }
        
        if (IsNullOrEmpty(x))
        {
            return 1;
        }
        
        if (IsNullOrEmpty(y))
        {
            return -1;
        }
        
        return Comparer<object>.Default.Compare(x, y);
    }

    private bool IsNullOrEmpty(object x)
    {
        return x == null
            || x is string xString && xString.Length == 0
            || x is IList xList && xList.Count == 0
            || x is IDictionary xDictionary && xDictionary.Count == 0;
    }
}
