using System;
using UnityEngine.UIElements;

public static class UIDocumentUtils
{
    public static UIDocument FindUIDocumentOrThrow()
    {
        UIDocument uiDocument = GameObjectUtils.FindComponentWithTag<UIDocument>("UIDocument");
        if (uiDocument == null)
        {
            throw new UltraStarPlayException("No UIDocument found");
        }

        return uiDocument;
    }
}
