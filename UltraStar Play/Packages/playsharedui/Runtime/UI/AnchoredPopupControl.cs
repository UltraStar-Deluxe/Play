using UnityEngine;
using UnityEngine.UIElements;

public class AnchoredPopupControl
{
    public VisualElement PopupElement { get; private set; }
    public VisualElement AnchorElement { get; private set; }
    public Corner2D AnchorCorner { get; private set; }

    private readonly PanelHelper panelHelper;
    private Vector2 lastPopupSize;
    private Vector2 lastPopupPosition;
    
    public AnchoredPopupControl(VisualElement popupElement, VisualElement anchorElement, Corner2D anchorCorner)
    {
        PopupElement = popupElement;
        AnchorElement = anchorElement;
        AnchorCorner = anchorCorner;

        panelHelper = new PanelHelper(popupElement.panel, UIDocumentUtils.FindUIDocumentOrThrow().panelSettings);
        
        popupElement.RegisterCallback<GeometryChangedEvent>(_ =>
        {
            if (Vector2.Distance(lastPopupSize, PopupElement.contentRect.size) > 1f)
            {
                UpdatePosition();
                lastPopupSize = PopupElement.contentRect.size;
            }
        });
        anchorElement.RegisterCallback<GeometryChangedEvent>(_ => UpdatePosition());
        UpdatePosition();
    }

    public void UpdatePosition()
    {
        Vector2 anchorPosition = GetAnchorPosition();
        PopupElement.style.position = new StyleEnum<Position>(Position.Absolute);

        Vector2 screenSizeInPanelCoordinates = ApplicationUtils.GetScreenSizeInPanelCoordinates(panelHelper);
        if (AnchorCorner is Corner2D.BottomLeft or Corner2D.BottomRight)
        {
            // Grow content downwards
            PopupElement.style.top = anchorPosition.y;
            PopupElement.style.bottom = new StyleLength(StyleKeyword.Auto);
        }
        else
        {
            // Grow content upwards
            PopupElement.style.bottom = screenSizeInPanelCoordinates.y - anchorPosition.y;
            PopupElement.style.top = new StyleLength(StyleKeyword.Auto);
        }

        if (AnchorCorner is Corner2D.BottomRight or Corner2D.TopRight)
        {
            // Grow content to the left
            PopupElement.style.right = screenSizeInPanelCoordinates.x - anchorPosition.x;
            PopupElement.style.left = new StyleLength(StyleKeyword.Auto);
        }
        else
        {
            // Grow content to the right
            PopupElement.style.left = anchorPosition.x;
            PopupElement.style.right = new StyleLength(StyleKeyword.Auto);
        }
        
        VisualElementUtils.MoveVisualElementFullyInsideScreen(PopupElement, panelHelper);
    }
    
    private Vector2 GetAnchorPosition()
    {
        if (AnchorElement == null)
        {
            return Vector2.zero;
        }

        switch (AnchorCorner)
        {
            case Corner2D.TopLeft:
                return new Vector2(AnchorElement.worldBound.xMin, AnchorElement.worldBound.yMin);
            case Corner2D.TopRight:
                return new Vector2(AnchorElement.worldBound.xMax, AnchorElement.worldBound.yMin);
            case Corner2D.BottomLeft:
                return new Vector2(AnchorElement.worldBound.xMin, AnchorElement.worldBound.yMax);
            case Corner2D.BottomRight:
                return new Vector2(AnchorElement.worldBound.xMax, AnchorElement.worldBound.yMax);
            default: 
                return Vector2.zero;
        }
    }
}
