using System.Collections.Generic;

public interface IDragAndDropController<in TArgs>
{
    bool CanStartDrag(IEnumerable<int> itemIndices);

    StartDragArgs SetupDragAndDrop(IEnumerable<int> itemIndices, bool skipText = false);

    DragVisualMode HandleDragAndDrop(TArgs args);

    void OnDrop(TArgs args);
}
