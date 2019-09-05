using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneDataBus
{
    private static readonly Dictionary<ESceneData, object> dataMap = new Dictionary<ESceneData, object>();

    private static readonly Dictionary<ESceneData, List<Action>> dataListenersMap = new Dictionary<ESceneData, List<Action>>();

    public static void PutData(ESceneData key, object value)
    {
        dataMap[key] = value;
        // Run all actions that were waiting for this data.
        if (dataListenersMap.TryGetValue(key, out List<Action> listeners))
        {
            var listenersCopy = new List<Action>(listeners);
            foreach (Action listener in listenersCopy)
            {
                listener();
                listeners.Remove(listener);
            }
        }
    }

    public static bool HasData(ESceneData key)
    {
        return dataMap.ContainsKey(key);
    }

    public static object GetData(ESceneData key)
    {
        if (dataMap.TryGetValue(key, out object value))
        {
            return value;
        }
        else
        {
            Debug.LogError($"Data object {key} for the scene is not available. Terminating.");
            ApplicationUtils.QuitOrStopPlayMode();
            return null;
        }
    }

    public static T GetData<T>(ESceneData key, Func<T> defaultValueProvider)
    {
        if (dataMap.TryGetValue(key, out object value))
        {
            if (value is T t)
            {
                return t;
            }
            else
            {
                Debug.LogError($"Value on SceneDataBus has wrong type. value for '{key}' is of type {typeof(T)}. Returning default value instead.");
                return defaultValueProvider.Invoke();
            }
        }
        else
        {
            return defaultValueProvider.Invoke();
        }
    }

    public static T GetData<T>(ESceneData key, T defaultValue)
    {
        if (dataMap.TryGetValue(key, out object value))
        {
            if (value is T t)
            {
                return t;
            }
            else
            {
                Debug.LogError($"Value on SceneDataBus has wrong type. value for '{key}' is of type {typeof(T)}. Returning default value instead.");
                return defaultValue;
            }
        }
        else
        {
            return defaultValue;
        }
    }

    public static void AwaitData(ESceneData key, Action action)
    {
        if (HasData(key))
        {
            action();
        }
        else
        {
            dataListenersMap.AddInsideList(key, action);
        }
    }
}

public enum ESceneData
{
    SelectedSong,
    SelectedPlayerProfile,
    AllPlayerProfiles,
    AllSongMetas
}