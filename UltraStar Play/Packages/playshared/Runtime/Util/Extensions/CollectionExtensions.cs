﻿using System;
using System.Collections;
using System.Collections.Generic;
using CircularBuffer;

public static class CollectionExtensions
{
    public static void ForEach<T>(this T[] arr, Action<T> action)
    {
        for (int i = 0; i < arr.Length; i++)
        {
            action(arr[i]);
        }
    }

    public static void ForEach<T>(this CircularBuffer<T> circularBuffer, Action<T> action)
    {
        for (int i = 0; i < circularBuffer.Count; i++)
        {
            action(circularBuffer[i]);
        }
    }

    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        foreach (T item in enumerable)
        {
            action(item);
        }
    }

    public static string JoinWith<T>(this IEnumerable<T> enumerable, string separator, string prefix = "", string suffix = "")
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
        List<T> result = new();

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
            if (Equals(elem, element))
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    public static T FindMinElement<T>(this IEnumerable<T> enumerable, Func<T, float> valueFunction)
    {
        T minElement = default(T);
        float minValue = float.MaxValue;
        foreach (T element in enumerable)
        {
            float currentValue = valueFunction(element);
            if (currentValue < minValue)
            {
                minValue = currentValue;
                minElement = element;
            }
        }
        return minElement;
    }

    public static T FindMinElement<T>(this IEnumerable<T> enumerable, Func<T, double> valueFunction)
    {
        T minElement = default(T);
        double minValue = double.MaxValue;
        foreach (T element in enumerable)
        {
            double currentValue = valueFunction(element);
            if (currentValue < minValue)
            {
                minValue = currentValue;
                minElement = element;
            }
        }
        return minElement;
    }

    public static T FindMaxElement<T>(this IEnumerable<T> enumerable, Func<T, float> valueFunction)
    {
        T maxElement = default(T);
        float maxValue = float.MinValue;
        foreach (T element in enumerable)
        {
            float currentValue = valueFunction(element);
            if (currentValue > maxValue)
            {
                maxValue = currentValue;
                maxElement = element;
            }
        }
        return maxElement;
    }

    public static T FindMaxElement<T>(this IEnumerable<T> enumerable, Func<T, double> valueFunction)
    {
        T maxElement = default(T);
        double maxValue = double.MinValue;
        foreach (T element in enumerable)
        {
            double currentValue = valueFunction(element);
            if (currentValue > maxValue)
            {
                maxValue = currentValue;
                maxElement = element;
            }
        }
        return maxElement;
    }

    public static Dictionary<TValue, TKey> ToInvertedDictionary<TKey, TValue>(this IDictionary<TKey, TValue> source)
    {
        Dictionary<TValue, TKey> result = new();
        foreach (KeyValuePair<TKey, TValue> entry in source)
        {
            result[entry.Value] = entry.Key;
        }
        return result;
    }

    public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> enumerable)
    {
        enumerable.ForEach(item => hashSet.Add(item));
    }

    public static void RemoveRange<T>(this HashSet<T> hashSet, IEnumerable<T> enumerable)
    {
        enumerable.ForEach(item => hashSet.Remove(item));
    }

    public static void AddRange<K, V>(this Dictionary<K, V> targetDictionary, IReadOnlyDictionary<K, V> sourceDictionary)
    {
        sourceDictionary.ForEach(entry => targetDictionary[entry.Key] = entry.Value);
    }

    public static void RemoveRange<K, V>(this Dictionary<K, V> targetDictionary, IReadOnlyDictionary<K, V> sourceDictionary)
    {
        sourceDictionary.ForEach(entry => targetDictionary.Remove(entry.Key));
    }

    public static void Replace<T>(this List<T> list, T item, T replacement)
    {
        int index = list.IndexOf(item);
        if (index < 0)
        {
            return;
        }

        list[index] = replacement;
    }

    public static void RemoveAll<T>(this List<T> list, IEnumerable<T> enumerable)
    {
        enumerable.ForEach(item => list.Remove(item));
    }

    public static void RemoveFirst<T>(this List<T> list)
    {
        if (!list.IsNullOrEmpty())
        {
            list.RemoveAt(0);
        }
    }

    public static void RemoveLast<T>(this List<T> list)
    {
        if (!list.IsNullOrEmpty())
        {
            list.RemoveAt(list.Count - 1);
        }
    }
}
