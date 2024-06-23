using System;
using System.Collections.Generic;
using System.Linq;

public class ObjectPool<T>
    where T : class
{
    private readonly HashSet<T> usedObjects = new();
    private readonly HashSet<T> freeObjects = new();

    private readonly Action<T> onFreeObject;
    private readonly Action<T> onUseObject;

    public int FreeObjectsCount => freeObjects.Count;
    public int UsedObjectsCount => usedObjects.Count;
    public int Count => FreeObjectsCount + UsedObjectsCount;

    public ObjectPool(Action<T> onFreeObject = null, Action<T> onUseObject = null)
    {
        this.onFreeObject = onFreeObject;
        this.onUseObject = onUseObject;
    }

    public bool TryGetFreeObject(out T obj)
    {
        if (freeObjects.Count > 0)
        {
            obj = freeObjects.FirstOrDefault();
            freeObjects.Remove(obj);
            usedObjects.Add(obj);

            if (onUseObject != null)
            {
                onUseObject(obj);
            }

            return true;
        }

        obj = null;
        return false;
    }

    public void AddFreeObject(T obj, bool runOnFreeObject = true)
    {
        if (freeObjects.Contains(obj))
        {
            return;
        }

        freeObjects.Add(obj);
        usedObjects.Remove(obj);

        if (runOnFreeObject
            && onFreeObject != null)
        {
            onFreeObject(obj);
        }
    }

    public void AddUsedObject(T obj, bool runOnUsedObject = true)
    {
        if (usedObjects.Contains(obj))
        {
            return;
        }

        freeObjects.Remove(obj);
        usedObjects.Add(obj);

        if (runOnUsedObject
            && onUseObject != null)
        {
            onUseObject(obj);
        }
    }

    public void FreeAllObjects()
    {
        new List<T>(usedObjects).ForEach(obj => AddFreeObject(obj));
    }

    public void ForEach(Action<T> action)
    {
        usedObjects.ForEach(action);
        freeObjects.ForEach(action);
    }
}
