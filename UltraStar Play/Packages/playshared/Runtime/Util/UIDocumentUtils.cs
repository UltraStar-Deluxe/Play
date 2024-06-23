using UnityEngine.UIElements;

public static class UIDocumentUtils
{
    public static UIDocument FindUIDocumentOrThrow()
    {
        UIDocument uiDocument = GameObjectUtils.FindComponentWithTag<UIDocument>("UIDocument");
        if (uiDocument == null)
        {
            // Try again, now also search inactive UIDocument
            uiDocument = GameObjectUtils.FindObjectOfType<UIDocument>(true);
        }
        
        if (uiDocument == null)
        {
            throw new UltraStarPlayException("No UIDocument found");
        }

        return uiDocument;
    }
}
