using System;
using System.Collections.Generic;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class DontDestroyOnLoadManager : AbstractSingletonBehaviour
{
    private static DontDestroyOnLoadManager instance;
    public static DontDestroyOnLoadManager Instance
    {
        get
        {
            if (instance == null)
            {
                DontDestroyOnLoadManager instanceInScene = GameObjectUtils.FindComponentWithTag<DontDestroyOnLoadManager>("DontDestroyOnLoadManager");
                if (instanceInScene != null)
                {
                    GameObjectUtils.TryInitSingleInstanceWithDontDestroyOnLoad(ref instance, ref instanceInScene);
                    return instanceInScene;
                }
            }

            // Return proper null value when GameObject has been destroyed (otherwise, Assert.IsNull fails)
            if (instance == null)
            {
                return null;
            }

            return instance;
        }
    }

    private readonly Dictionary<Type, Component> typeToComponentCache = new();

    protected override object GetInstance()
    {
        return Instance;
    }

    public static T FindComponentOrThrow<T>() where T : Component
    {
        if (Instance == null)
        {
            return null;
        }
        return Instance.DoFindComponentOrThrow<T>();
    }

    public T DoFindComponentOrThrow<T>() where T : Component
    {
        // Search in cache
        Type typeOfT = typeof(T);
        if (typeToComponentCache.TryGetValue(typeOfT, out Component component))
        {
            return component as T;
        }

        // Search in children
        T componentInChildren = GetComponentInChildren<T>();
        if (componentInChildren == null)
        {
            throw new SingletonNotFoundException($"Did not find Component '{typeOfT}' in {nameof(DontDestroyOnLoadManager)}");
        }
        typeToComponentCache[typeOfT] = componentInChildren;
        return componentInChildren;
    }
}
