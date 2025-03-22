using System.Collections;
using System.Collections.Generic;

public class ListWithHashSetLookup<T> : IEnumerable<T>
{
    private readonly List<T> list;
    private readonly HashSet<T> hashSet;

    public bool Contains(T t) => hashSet.Contains(t);
    public int IndexOf(T item) => list.IndexOf(item);
    public T this[int index] => list[index];
    public int Count => list.Count;
    public IEnumerator<T> GetEnumerator() => list.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();

    public ListWithHashSetLookup()
    {
        list = new();
        hashSet = new();
    }

    public ListWithHashSetLookup(int capacity)
    {
        list = new(capacity);
        hashSet = new(capacity);
    }
    
    public void Add(T t)
    {
        if (hashSet.Add(t))
        {
            list.Add(t);
        }
    }

    public void AddRange(IEnumerable<T> ts)
    {
        foreach (T t in ts)
        {
            Add(t);
        }
    }
    
    public bool Remove(T item)
    {
        list.Remove(item);
        return hashSet.Remove(item);
    }
    
    public void Clear()
    {
        list.Clear();
        hashSet.Clear();
    }
}
