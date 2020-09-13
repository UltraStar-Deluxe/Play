using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;

public class PlayerUiArea : MonoBehaviour, INeedInjection
{
    [Inject(searchMethod = SearchMethods.GetComponent)]
    private GridLayoutGroupCellSizer gridLayoutGroupCellSizer;

    void Awake()
    {
        // Remove dummies from the Editor scene
        foreach (PlayerUiController playerUiController in GetComponentsInChildren<PlayerUiController>())
        {
            GameObject.Destroy(playerUiController.gameObject);
        }
    }

    public void SetPlayerCount(int playerCount)
    {
        int columns = 1;
        int rows = 2;
        if (playerCount >= 3)
        {
            columns = 2;
            rows = 2;
        }
        if (playerCount >= 5)
        {
            columns = 2;
            rows = 3;
        }
        if (playerCount >= 7)
        {
            columns = 3;
            rows = 3;
        }
        gridLayoutGroupCellSizer.columns = columns;
        gridLayoutGroupCellSizer.rows = rows;
    }

}
