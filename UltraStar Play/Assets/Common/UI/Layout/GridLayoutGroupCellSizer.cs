using UniRx.Triggers;
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

    private int lastColumns;
    private int lastRows;
    private Vector2 lastSpacing;
    private RectOffset lastPadding;
    private Rect lastRect;

    private void Awake()
    {
        gridLayoutGroup = GetComponent<GridLayoutGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (lastRows != rows
            || lastColumns != columns
            || lastSpacing != gridLayoutGroup.spacing
            || lastPadding != gridLayoutGroup.padding
            || lastRect != rectTransform.rect)
        {
            float availableWidth = rectTransform.rect.width - (gridLayoutGroup.spacing.x * (columns - 1)) - (gridLayoutGroup.padding.left + gridLayoutGroup.padding.right);
            float availableHeight = rectTransform.rect.height - (gridLayoutGroup.spacing.y * (rows - 1)) - (gridLayoutGroup.padding.top + gridLayoutGroup.padding.bottom);
            gridLayoutGroup.cellSize = new Vector2(availableWidth / columns, availableHeight / rows);

            lastRows = rows;
            lastColumns = columns;
            lastSpacing = gridLayoutGroup.spacing;
            lastPadding = gridLayoutGroup.padding;
            lastRect = rectTransform.rect;
        }
    }
}
