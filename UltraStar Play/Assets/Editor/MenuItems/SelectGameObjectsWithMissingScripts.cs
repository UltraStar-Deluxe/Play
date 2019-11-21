using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;

public static class SelectGameObjectsWithMissingScripts
{
    // Hotkey: Alt+M
    [MenuItem("Tools/Select GameObjects With Missing Scripts In Scene &m")]
    public static void SelectGameObjectsWithMissingScriptsInSceneRecursively()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = currentScene.GetRootGameObjects();
        SelectGameObjectsWithMissingScriptsRecursively(rootObjects);
    }

    // Hotkey: Ctrl+Alt+M
    [MenuItem("Tools/Select GameObjects With Missing Scripts In Selection %&m")]
    public static void SelectGameObjectsWithMissingScriptsInSelectionRecursively()
    {
        GameObject[] rootObjects = Selection.gameObjects;
        SelectGameObjectsWithMissingScriptsRecursively(rootObjects);
    }

    public static void SelectGameObjectsWithMissingScriptsRecursively(GameObject[] rootObjects)
    {
        List<UnityEngine.Object> objectsWithDeadLinks = new List<UnityEngine.Object>();
        foreach (GameObject rootObject in rootObjects)
        {
            FindGameObjectsWithMissingScriptsRecursively(rootObject, objectsWithDeadLinks);
        }
        if (objectsWithDeadLinks.Count > 0)
        {
            // Set the selection in the editor.
            // This will also make the objects visible in the hierarchy.
            Selection.objects = objectsWithDeadLinks.ToArray();
        }
        else
        {
            Debug.Log("No GameObjects with missing scripts found! Yay!");
        }
    }

    private static void FindGameObjectsWithMissingScriptsRecursively(GameObject gameObject, List<UnityEngine.Object> resultList)
    {
        // Iterate over Components in the GameObject.
        foreach (Component component in gameObject.GetComponents<Component>())
        {
            // If the component is null, that means it's a missing script!
            if (component == null)
            {
                resultList.Add(gameObject);
                Debug.Log("GameObject '" + gameObject.name + "' has a missing script!");
                break;
            }
        }

        foreach (Transform child in gameObject.transform)
        {
            FindGameObjectsWithMissingScriptsRecursively(child.gameObject, resultList);
        }
    }
}