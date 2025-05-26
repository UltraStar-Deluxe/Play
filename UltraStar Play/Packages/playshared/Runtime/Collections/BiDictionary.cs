using System.Collections.Generic;

public class BiDictionary<TFirst, TSecond>
{
    private readonly IDictionary<TFirst, TSecond> firstToSecond;
    private readonly IDictionary<TSecond, TFirst> secondToFirst;

    public BiDictionary()
    {
        firstToSecond = new Dictionary<TFirst, TSecond>();
        secondToFirst = new Dictionary<TSecond, TFirst>();
    }

    public BiDictionary(
        ICollection<KeyValuePair<TFirst, TSecond>> collection,
        IEqualityComparer<TFirst> firstComparer = null,
        IEqualityComparer<TSecond> secondComparer = null)
    {
        firstComparer = firstComparer ?? EqualityComparer<TFirst>.Default;
        secondComparer = secondComparer ?? EqualityComparer<TSecond>.Default;

        this.firstToSecond = new Dictionary<TFirst, TSecond>(collection.Count, firstComparer);
        this.secondToFirst = new Dictionary<TSecond, TFirst>(collection.Count, secondComparer);

        foreach (var item in collection)
        {
            this.firstToSecond.Add(item.Key, item.Value);
            this.secondToFirst.Add(item.Value, item.Key);
        }
    }

    public void Set(TFirst first, TSecond second)
    {
        firstToSecond[first] = second;
        secondToFirst[second] = first;
    }

    public bool TryGetByFirst(TFirst first, out TSecond second)
    {
        return firstToSecond.TryGetValue(first, out second);
    }

    public bool TryGetBySecond(TSecond second, out TFirst first)
    {
        return secondToFirst.TryGetValue(second, out first);
    }

    public void RemoveByFirst(TFirst first)
    {
        if (!firstToSecond.TryGetValue(first, out TSecond second))
        {
            return;
        }

        firstToSecond.Remove(first);
        secondToFirst.Remove(second);
    }

    public void RemoveBySecond(TSecond second)
    {
        if (!secondToFirst.TryGetValue(second, out TFirst first))
        {
            return;
        }

        firstToSecond.Remove(first);
        secondToFirst.Remove(second);
    }

    public void Clear()
    {
        firstToSecond.Clear();
        secondToFirst.Clear();
    }
}
