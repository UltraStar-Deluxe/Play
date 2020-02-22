using System;
using System.Collections;
using System.Collections.Generic;

// Simple thread-safe implementation of a List, by locking on every call.
// However, one should be careful when using LINQ.
public class SynchronizedList<T> : ICollection<T>, IEnumerable<T>, IEnumerable, IList<T>, IReadOnlyCollection<T>, IReadOnlyList<T>
{
    private readonly object listLock = new object();
    private readonly List<T> internalList = new List<T>();

    public T this[int index]
    {
        get
        {
            lock (listLock)
            {
                return internalList[index];
            }
        }
        set
        {
            lock (listLock)
            {
                internalList[index] = value;
            }
        }
    }

    public int Count
    {
        get
        {
            lock (listLock)
            {
                return internalList.Count;
            }
        }
    }

    public bool IsReadOnly => false;

    public void Add(T item)
    {
        lock (listLock)
        {
            internalList.Add(item);
        }
    }

    public void Clear()
    {
        lock (listLock)
        {
            internalList.Clear();
        }
    }

    public bool Contains(T item)
    {
        lock (listLock)
        {
            return internalList.Contains(item);
        }
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        lock (listLock)
        {
            internalList.CopyTo(array, arrayIndex);
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        lock (listLock)
        {
            return internalList.GetEnumerator();
        }
    }

    public int IndexOf(T item)
    {
        lock (listLock)
        {
            return internalList.IndexOf(item);
        }
    }

    public void Insert(int index, T item)
    {
        lock (listLock)
        {
            internalList.Insert(index, item);
        }
    }

    public bool Remove(T item)
    {
        lock (listLock)
        {
            return internalList.Remove(item);
        }
    }

    public void RemoveAt(int index)
    {
        lock (listLock)
        {
            internalList.RemoveAt(index);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        lock (listLock)
        {
            return internalList.GetEnumerator();
        }
    }

    public void Sort(Comparison<T> comparison)
    {
        lock (listLock)
        {
            internalList.Sort(comparison);
        }
    }

    public void Sort(int index, int count, IComparer<T> comparer)
    {
        lock (listLock)
        {
            internalList.Sort(index, count, comparer);
        }
    }

    public void Sort()
    {
        lock (listLock)
        {
            internalList.Sort();
        }
    }

    public void Sort(IComparer<T> comparer)
    {
        lock (listLock)
        {
            internalList.Sort(comparer);
        }
    }

    public T Find(Predicate<T> match)
    {
        lock (listLock)
        {
            return internalList.Find(match);
        }
    }

    public List<T> FindAll(Predicate<T> match)
    {
        lock (listLock)
        {
            return internalList.FindAll(match);
        }
    }
}
