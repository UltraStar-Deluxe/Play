using UnityEngine;
using System.Collections.Generic;

public static class ObjectUtils
{
    // Swaps the values of a and b.
    // Example: Let a=1 and b=2. Then Swap(ref a, ref b) will result in a=2 and b=1.
    public static void Swap<T>(ref T a, ref T b)
    {
        T tmp = a;
        a = b;
        b = tmp;
    }

    public static void ShuffleList<T>(IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}
