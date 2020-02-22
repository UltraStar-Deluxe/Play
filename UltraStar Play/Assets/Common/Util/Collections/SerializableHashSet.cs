using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A HashSet is not serializable by Unity.
// This class wraps a HashSet and implements its serialization by serializing its values as List.
[Serializable]
public class SerializableHashSet<T> : ISerializationCallbackReceiver, ICollection<T>, IReadOnlyCollection<T>, ISet<T>
{
    [SerializeField]
    private List<T> valueList = new List<T>();

    private HashSet<T> hashSet = new HashSet<T>();

    // Save values in HashSet to List
    public void OnBeforeSerialize()
    {
        valueList.Clear();
        foreach (T element in hashSet)
        {
            valueList.Add(element);
        }
    }

    // Load values from List into HashSet
    public void OnAfterDeserialize()
    {
        hashSet.Clear();

        foreach (T element in valueList)
        {
            hashSet.Add(element);
        }
    }

    public int Count
    {
        get
        {
            return hashSet.Count;
        }
    }

    public bool IsReadOnly
    {
        get
        {
            return false;
        }
    }

    public void Add(T item)
    {
        hashSet.Add(item);
    }

    public bool Remove(T item)
    {
        return hashSet.Remove(item);
    }

    public bool Contains(T item)
    {
        return hashSet.Contains(item);
    }

    public void Clear()
    {
        hashSet.Clear();
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        hashSet.CopyTo(array, arrayIndex);
    }

    public IEnumerator<T> GetEnumerator()
    {
        return hashSet.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return hashSet.GetEnumerator();
    }

    bool ISet<T>.Add(T item)
    {
        return hashSet.Add(item);
    }

    public void ExceptWith(IEnumerable<T> other)
    {
        hashSet.ExceptWith(other);
    }

    public void IntersectWith(IEnumerable<T> other)
    {
        hashSet.IntersectWith(other);
    }

    public bool IsProperSubsetOf(IEnumerable<T> other)
    {
        return hashSet.IsProperSubsetOf(other);
    }

    public bool IsProperSupersetOf(IEnumerable<T> other)
    {
        return hashSet.IsProperSubsetOf(other);
    }

    public bool IsSubsetOf(IEnumerable<T> other)
    {
        return hashSet.IsSubsetOf(other);
    }

    public bool IsSupersetOf(IEnumerable<T> other)
    {
        return hashSet.IsSupersetOf(other);
    }

    public bool Overlaps(IEnumerable<T> other)
    {
        return hashSet.Overlaps(other);
    }

    public bool SetEquals(IEnumerable<T> other)
    {
        return hashSet.SetEquals(other);
    }

    public void SymmetricExceptWith(IEnumerable<T> other)
    {
        hashSet.SymmetricExceptWith(other);
    }

    public void UnionWith(IEnumerable<T> other)
    {
        hashSet.UnionWith(other);
    }
}
