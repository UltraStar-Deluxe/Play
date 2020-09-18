using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.UI;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlayerUiArea : MonoBehaviour, INeedInjection
{
    void Awake()
    {
        // Remove dummies from the Editor scene
        foreach (PlayerUiController playerUiController in GetComponentsInChildren<PlayerUiController>())
        {
            GameObject.Destroy(playerUiController.gameObject);
        }
    }

    public static void SetupPlayerUiGrid(int playerCount, GridLayoutGroupCellSizer gridLayoutGroupCellSizer)
    {
        GridLayoutGroup gridLayoutGroup = gridLayoutGroupCellSizer.GetComponent<GridLayoutGroup>();
        int gridSpacing = 20;

        int columns = 1;
        int rows = 2;
        gridLayoutGroup.spacing = new Vector2(0, gridSpacing);

        gridLayoutGroup.childAlignment = (playerCount == 1) ? TextAnchor.LowerLeft : TextAnchor.UpperLeft;

        if (playerCount >= 3)
        {
            columns = 1;
            rows = 3;
            gridLayoutGroup.spacing = new Vector2(0, gridSpacing);
        }
        if (playerCount >= 4)
        {
            columns = 2;
            rows = 2;
            gridLayoutGroup.spacing = new Vector2(gridSpacing, gridSpacing);
        }
        if (playerCount >= 5)
        {
            columns = 2;
            rows = 3;
            gridLayoutGroup.spacing = new Vector2(gridSpacing, gridSpacing);
        }
        if (playerCount >= 7)
        {
            columns = 3;
            rows = 3;
            gridLayoutGroup.spacing = new Vector2(gridSpacing, gridSpacing);
        }
        gridLayoutGroupCellSizer.columns = columns;
        gridLayoutGroupCellSizer.rows = rows;
    }
}
