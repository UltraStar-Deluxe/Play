using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUiArea : MonoBehaviour
{

    void Awake()
    {
        // Remove dummies from the Editor scene
        foreach (PlayerUiController playerUiController in GetComponentsInChildren<PlayerUiController>())
        {
            GameObject.Destroy(playerUiController.gameObject);
        }
    }

}
