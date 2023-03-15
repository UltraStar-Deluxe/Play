using System;
using System.Collections;
using System.Collections.Generic;

public class FixLengthList<T> : IEnumerable<T>
{
    private int fixLength;
    public int FixLength
    {
        get => fixLength;
        set
        {
            fixLength = value;
            UpdateListSize(fixLength);
        }
    }

    public int Count => FixLength;
    
    private List<T> list;

    private Func<T> createInstanceFunction;
    
    public FixLengthList(int fixLength, Func<T> createInstanceFunction)
    {
        this.fixLength = fixLength;
        this.createInstanceFunction = createInstanceFunction;
        list = new List<T>(fixLength);
        UpdateListSize(fixLength);
    }

    public T Get(int index)
    {
        return list[index];
    }

    public void Set(int index, T value)
    {
        list[index] = value;
    }
    
    public T this[int index]
    {
        get => list[index];
        set => list[index] = value;
    }

    public void UpdateListSize(int size)
    {
        // Create new list if needed
        if (list == null)
        {
            list = new List<T>(size);
        }

        // Remove elements if needed
        if (size > 0 && list.Count > size)
        {
            while (list.Count > size)
            {
                list.RemoveAt(list.Count - 1);
            }
        }

        // Add new elements if needed
        for (int i = 0; i < size; i++)
        {
            if (list.Count <= i)
            {
                T instance = createInstanceFunction();
                list.Add(instance);
            }
            else
            {
                if (list[i] == null)
                {
                    list[i] = createInstanceFunction();
                }
            }
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        return list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
