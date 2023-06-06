using System.Collections;
using System.Collections.Generic;

/**
 * Comparer that puts null and empty values last.
 */
public class NullOrEmptyValueLastComparer : IComparer<object>
{
    public int Compare(object x, object y)
    {
        if (x == null && y == null)
        {
            return 0;
        }
        
        if (x == null)
        {
            return 1;
        }
        
        if (y == null)
        {
            return -1;
        }
    
        // Null or empty last
        if (x is string xString && string.IsNullOrEmpty(xString)
            || x is IList xList && xList.Count == 0
            || x is IDictionary xDictionary && xDictionary.Count == 0)
        {
            return 1;
        }
    
        return Comparer<object>.Default.Compare(x, y);
    }
}
