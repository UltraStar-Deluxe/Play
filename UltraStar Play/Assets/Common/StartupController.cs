using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartupController : MonoBehaviour
{
    private static StartupController Instance
    {
        get
        {
            var obj = GameObject.FindGameObjectWithTag("SceneStartupController");
            if (obj)
            {
                return obj.GetComponent<StartupController>();
            }
            else
            {
                Debug.LogError("No SceneStartupController. Add the object to the scene and make sure it has the corresponding tag.");
                return null;
            }
        }
    }

    public static void AddStartupAction(StartupAction startupAction)
    {
        Instance.m_startupActionMap.Add(startupAction.Name, startupAction);
    }

    private Dictionary<string, StartupAction> m_startupActionMap = new Dictionary<string, StartupAction>();

    void Start()
    {
        ExecuteAllActionsRespectingDependencies();
    }

    private void ExecuteAllActionsRespectingDependencies()
    {
        var seenStartupActionMap = new Dictionary<StartupAction, int>();
        foreach (var startupAction in m_startupActionMap.Values)
        {
            ExecuteActionRespectingDependencies(startupAction, seenStartupActionMap, 0);
        }
    }

    private void ExecuteActionRespectingDependencies(StartupAction startupAction, Dictionary<StartupAction, int> seenStartupActionMap, int dependencyLevel)
    {
        int seenValue = 0;
        seenStartupActionMap.TryGetValue(startupAction, out seenValue);
        switch (seenValue)
        {
            case 0:
                // The action has not been visited yet.
                // Debug.Log($"Executing StartupAction {startupAction.Name} (DFS depth: {dependencyLevel})");
                // Execute its dependencies.
                seenStartupActionMap[startupAction] = 1;
                foreach (string dependencyActionName in startupAction.Dependencies)
                {
                    StartupAction dependencyAction;
                    if (m_startupActionMap.TryGetValue(dependencyActionName, out dependencyAction))
                    {
                        ExecuteActionRespectingDependencies(dependencyAction, seenStartupActionMap, ++dependencyLevel);
                    }
                    else
                    {
                        Debug.LogError($"StartupAction dependency not found: {dependencyActionName}. Terminating.");
                        ApplicationUtils.QuitOrStopPlayMode();
                        return;
                    }
                }
                // Execute the action itself.
                startupAction.Run();
                // This action is done
                seenStartupActionMap[startupAction] = 2;
                break;
            case 1:
                // The action's dependencies are beeing executed and the action itself should be executed again now.
                // Getting here means there is a loop in the dependencies.
                Debug.LogError($"Detected loop in StartupAction dependencies of {startupAction.Name}. Terminating.");
                ApplicationUtils.QuitOrStopPlayMode();
                return;
            case 2:
                // The action has already been executed. Nothing to do.
                return;
        }
    }
}