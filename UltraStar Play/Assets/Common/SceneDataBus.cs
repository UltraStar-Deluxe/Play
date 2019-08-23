using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneDataBus
{
    private static Dictionary<ESceneData, object> dataMap = new Dictionary<ESceneData, object>(); 

    public static void PutData(ESceneData key, object value) {
        dataMap[key] = value;
    }

    public static object GetData(ESceneData key) {
        object value;
        if(dataMap.TryGetValue(key, out value)) {
            return value;
        } else {
            Debug.LogError($"Data object {key} for the scene is not available. Terminating.");
            ApplicationUtils.QuitOrStopPlayMode();
            return null;
        }
    }

    public static T GetData<T>(ESceneData key, Func<T> defaultValueProvider) {
        object value;
        if(dataMap.TryGetValue(key, out value)) {
            if(value is T) {
                return (T)value;
            } else {
                Debug.LogError($"Value on SceneDataBus has wrong type. value for '{key}' is of type {typeof(T)}. Returning default value instead.");
                return defaultValueProvider.Invoke();
            }
        } else {
            return defaultValueProvider.Invoke();
        }
    }
}

public enum ESceneData {
    Song,
    PlayerProfile,
}