using System;
using System.Collections.Generic;

public static class ListExtensions
{

    // Returns true if the predicate is true for all elements in the list. Otherwise, returns false.
    public static bool All<T>(this IList<T> list, Func<T, bool> predicate)
    {
        foreach (T t in list)
        {
            if (!predicate(t))
            {
                return false;
            }
        }
        return true;
    }

    // Returns true if the predicate is true for any element in the list. Otherwise, returns false.
    public static bool Any<T>(this IList<T> list, Func<T, bool> predicate)
    {
        foreach (T t in list)
        {
            if (predicate(t))
            {
                return true;
            }
        }
        return false;
    }

    // Returns the elements of the list that are before the given element.
    // Thereby, the given element is included in the result list if inclusive is true.
    // If the given element is not in the list, then an empty list is returned.
    public static List<T> ElementsBefore<T>(this List<T> list, T element, bool inclusive)
    {
        int indexOfElement = list.IndexOf(element);
        if (indexOfElement < 0)
        {
            return new List<T>();
        }
        else
        {
            List<T> result = list.GetRange(0, (inclusive) ? indexOfElement + 1 : indexOfElement);
            return result;
        }
    }

    // Returns the element in the list that comes after the given element.
    // If the given element is not in the list, then null is returned.
    public static T ElementAfter<T>(this List<T> list, T element)
    {
        int indexOfElement = list.IndexOf(element);
        if (indexOfElement >= 0 && list.Count > indexOfElement + 1)
        {
            return list[indexOfElement + 1];
        }
        else
        {
            return default(T);
        }
    }
}
