using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
[ExecuteInEditMode]
public class GridLayoutGroupCellSizer : MonoBehaviour
{
    [Min(1)]
    public int columns = 1;

    [Min(1)]
    public int rows = 1;

    private GridLayoutGroup gridLayoutGroup;
    private RectTransform rectTransform;

    void Awake()
    {
        gridLayoutGroup = GetComponent<GridLayoutGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        gridLayoutGroup.cellSize = new Vector2(rectTransform.rect.width / columns, rectTransform.rect.height / rows);
    }
}
