using System.Collections.Generic;
using UnityEngine;

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

    public static bool TryAddUntilCount<T>(List<T> targetList, List<T> sourceList, int targetCount)
    {
        if (targetList.Count >= targetCount)
        {
            return true;
        }

        for (int i = 0; i < sourceList.Count; i++)
        {
            targetList.Add(sourceList[i]);
            if (targetList.Count >= targetCount)
            {
                return true;
            }
        }
        return false;
    }

    public static List<T> Shuffle<T>(List<T> list)
    {
        List<T> shuffled = new List<T>(list);
        int remainingElementCount = shuffled.Count;
        while (remainingElementCount > 1)
        {
            int k = Random.Range(0, remainingElementCount);
            T temp = shuffled[remainingElementCount];
            shuffled[remainingElementCount] = shuffled[k];
            shuffled[k] = temp;
            remainingElementCount--;
        }
        return shuffled;
    }
}
