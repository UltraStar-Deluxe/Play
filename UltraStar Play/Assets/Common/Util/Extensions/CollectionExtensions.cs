using System;
using System.Collections;
using System.Collections.Generic;

public static class CollectionExtensions
{

    public static string ToCsv<T>(this IEnumerable<T> enumerable, string separator = ",", string prefix = "[", string suffix = "]")
    {
        return prefix + string.Join(separator, enumerable) + suffix;
    }

    public static void AddIfNotContains<T>(this ICollection<T> collection, T item)
    {
        if (!collection.Contains(item))
        {
            collection.Add(item);
        }
    }

    // Returns true if and only if the given collection is null or does not contain any values.
    public static bool IsNullOrEmpty<T>(this IReadOnlyCollection<T> collection)
    {
        return (collection == null) || (collection.Count == 0);
    }

    // Returns true if the predicate is true for all elements in the enumerable. Otherwise, returns false.
    public static bool AllMatch<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
    {
        foreach (T t in enumerable)
        {
            if (!predicate(t))
            {
                return false;
            }
        }
        return true;
    }

    // Returns true if the predicate is true for any element in the enumerable. Otherwise, returns false.
    public static bool AnyMatch<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
    {
        foreach (T t in enumerable)
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
    public static T GetElementBefore<T>(this IList<T> list, T element, bool wrapAround)
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
    public static T GetElementAfter<T>(this IList<T> list, T element, bool wrapAround)
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
    public static List<T> GetElementsBefore<T>(this IEnumerable<T> enumerable, T element, bool inclusive)
    {
        List<T> result = new List<T>();

        int indexOfElement = enumerable.IndexOf(element);
        if (indexOfElement < 0)
        {
            return result;
        }

        int index = 0;
        foreach (T elem in enumerable)
        {
            if (index == indexOfElement)
            {
                if (inclusive)
                {
                    result.Add(elem);
                }
                return result;
            }
            result.Add(elem);
            index++;
        }
        return result;
    }

    public static int IndexOf<T>(this IEnumerable<T> enumerable, T element)
    {
        if (enumerable is IList)
        {
            return (enumerable as IList).IndexOf(element);
        }

        int index = 0;
        foreach (T elem in enumerable)
        {
            if (object.Equals(elem, element))
            {
                return index;
            }
            index++;
        }
        return -1;
    }
}
