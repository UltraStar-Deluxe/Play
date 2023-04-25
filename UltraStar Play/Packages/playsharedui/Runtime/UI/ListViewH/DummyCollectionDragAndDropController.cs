// Decompiled with JetBrains decompiler
// Assembly: UnityEngine.UIElementsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Unity 2022.2.4f1

using System.Collections.Generic;

public class DummyCollectionDragAndDropController : ICollectionDragAndDropController
{
    public bool CanStartDrag(IEnumerable<int> itemIndices)
    {
        return false;
    }

    public StartDragArgs SetupDragAndDrop(IEnumerable<int> itemIndices, bool skipText = false)
    {
        throw new System.NotImplementedException();
    }

    public DragVisualMode HandleDragAndDrop(IListDragAndDropArgs args)
    {
        throw new System.NotImplementedException();
    }

    public void OnDrop(IListDragAndDropArgs args)
    {
        throw new System.NotImplementedException();
    }

    public bool enableReordering { get; set; }
}
