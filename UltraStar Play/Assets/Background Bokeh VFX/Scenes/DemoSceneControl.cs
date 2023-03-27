using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class DemoSceneControl : MonoBehaviour
{
    private List<GameObject> gameObjects;

    public int index;
    
	private void Start()
    {
        Camera[] cameras = FindObjectsOfType<Camera>(true);
        gameObjects = cameras
            .Select(cam => cam.gameObject)
            .OrderBy(go => go.name)
            .ToList();

        ShowBackground(index);
    }

    private void Update()
    {
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            index = (index + 1) % gameObjects.Count;
           ShowBackground(index);
        }
        else if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            index = (index - 1) % gameObjects.Count;
            if (index < 0)
            {
                index = gameObjects.Count - 1;
            }
            ShowBackground(index);
        }
    }

    private void ShowBackground(int i)
    {
        gameObjects.ForEach(go => go.SetActive(false));
        gameObjects[i].SetActive(true);
    }
}
