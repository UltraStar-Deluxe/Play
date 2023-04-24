// Decompiled with JetBrains decompiler
// Assembly: UnityEngine.UIElementsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Unity 2022.2.4f1

using System.Collections.Generic;

public interface IDragAndDropController<in TArgs>
{
    bool CanStartDrag(IEnumerable<int> itemIndices);

    StartDragArgs SetupDragAndDrop(IEnumerable<int> itemIndices, bool skipText = false);

    DragVisualMode HandleDragAndDrop(TArgs args);

    void OnDrop(TArgs args);
}
