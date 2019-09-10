using System.Collections.Generic;
using UnityEngine;

public static class MapExtensions
{

    /// Adds the new element to the list that is associated with the given key in the map.
    /// If there is not yet an associated list with that key,
    /// then a list will be created with the new value and put in the map.
    public static void AddInsideList<K, V>(this Dictionary<K, List<V>> map, K key, V newValue)
    {
        if (map.TryGetValue(key, out List<V> existingList))
        {
            existingList.Add(newValue);
        }
        else
        {
            List<V> newList = new List<V>();
            newList.Add(newValue);
            map.Add(key, newList);
        }
    }

}