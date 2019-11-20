using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;

public static class SelectGameObjectsWithMissingScripts
{
    // Hotkey: Alt+M
    [MenuItem("Tools/Select GameObjects With Missing Scripts &m")]
    public static void SelectGameObjectsWithMissingScriptsRecursively()
    {
        //Get the current scene and all top-level GameObjects in the scene hierarchy
        Scene currentScene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = currentScene.GetRootGameObjects();

        List<UnityEngine.Object> objectsWithDeadLinks = new List<UnityEngine.Object>();
        foreach (GameObject rootObject in rootObjects)
        {
            FindGameObjectsWithMissingScriptsRecursively(rootObject, objectsWithDeadLinks);
        }
        if (objectsWithDeadLinks.Count > 0)
        {
            //Set the selection in the editor
            Selection.objects = objectsWithDeadLinks.ToArray();
        }
        else
        {
            Debug.Log("No GameObjects in '" + currentScene.name + "' have missing scripts! Yay!");
        }
    }

    private static void FindGameObjectsWithMissingScriptsRecursively(GameObject gameObject, List<UnityEngine.Object> resultList)
    {
        // Get all components on the GameObject, then loop through them 
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