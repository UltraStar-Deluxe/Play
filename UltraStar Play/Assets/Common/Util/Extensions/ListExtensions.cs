using System;
using System.Collections.Generic;

public static class ListExtensions
{

    public static string ToCsv<T>(this IEnumerable<T> values, string separator = ",", string prefix = "[", string suffix = "]")
    {
        return prefix + string.Join(separator, values) + suffix;
    }

    public static void AddIfNotContains<T>(this List<T> list, T item)
    {
        if (!list.Contains(item))
        {
            list.Add(item);
        }
    }

    // Returns true if and only if the given collection is null or does not contain any values.
    public static bool IsNullOrEmpty<T>(this ICollection<T> collection)
    {
        return (collection == null) || (collection.Count == 0);
    }

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

    /// Returns the element before the given element in the list.
    /// If wrapAround is true and the given element is the first one in the list, then the last element in the list is returned.
    /// Otherwise returns default if already at the first element at the list. Also returns default if the list is empty.
    public static T ElementBefore<T>(this List<T> list, T element, bool wrapAround)
    {
        if (list.Count == 0)
        {
            return default(T);
        }

        int indexOfElement = list.IndexOf(element);
        if (indexOfElement > 0)
        {
            T elementBefore = list[indexOfElement - 1];
            return elementBefore;
        }
        else if (wrapAround)
        {
            T lastElement = list[list.Count - 1];
            return lastElement;
        }
        else
        {
            return default(T);
        }
    }

    /// Returns the element after the given element in the list.
    /// If wrapAround is true and the given element is the last one in the list, then the first element in the list is returned.
    /// Otherwise returns default if already at the last element at the list. Also returns default if the list is empty.
    public static T ElementAfter<T>(this List<T> list, T element, bool wrapAround)
    {
        if (list.Count == 0)
        {
            return default(T);
        }

        int indexOfElement = list.IndexOf(element);
        if (indexOfElement < list.Count - 1)
        {
            T elementAfter = list[indexOfElement + 1];
            return elementAfter;
        }
        else if (wrapAround)
        {
            T firstElement = list[0];
            return firstElement;
        }
        else
        {
            return default(T);
        }
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
