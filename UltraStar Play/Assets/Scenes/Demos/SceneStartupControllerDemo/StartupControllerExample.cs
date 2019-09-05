using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSceneStartupController : MonoBehaviour
{
    public string DependsOn;

    void Awake()
    {
        StartupAction action = new StartupAction(name, () => Debug.Log($"{name} executed"));
        if (!string.IsNullOrEmpty(DependsOn))
        {
            action.DependsOn(DependsOn);
        }
        StartupController.AddStartupAction(action);
    }
}
