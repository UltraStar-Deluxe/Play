using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUiArea : MonoBehaviour
{

    void OnEnable()
    {
        foreach (PlayerUiController playerUiController in GetComponentsInChildren<PlayerUiController>())
        {
            GameObject.Destroy(playerUiController.gameObject);
        }
    }

}
