using System;
using System.Collections;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class ListViewReorderByDragAndDropControl
{
    private readonly ListView listView;

    private readonly Subject<ReorderedEvent> reorderEventStream = new();
    public IObservable<ReorderedEvent> ReorderEventStream => reorderEventStream;

    private int? dragStartSelectedIndex = new();

    public ListViewReorderByDragAndDropControl(ListView listView)
    {
        this.listView = listView;
        listView.reorderable = true;
        listView.setupDragAndDrop += OnSetupDragAndDrop;
        listView.handleDrop += OnHandleDrop;
    }

    private UnityEngine.UIElements.StartDragArgs OnSetupDragAndDrop(SetupDragAndDropArgs args)
    {
        dragStartSelectedIndex = args.selectedIds.FirstOrDefault();
        return args.startDragArgs;
    }

    private UnityEngine.UIElements.DragVisualMode OnHandleDrop(HandleDragAndDropArgs args)
    {
        if (!dragStartSelectedIndex.HasValue)
        {
            return UnityEngine.UIElements.DragVisualMode.Move;
        }

        int sourceIndex = dragStartSelectedIndex.Value;
        int targetIndex = args.insertAtIndex;

        if (targetIndex > sourceIndex)
        {
            // When dragging downwards then one uses one index above the actual target index.
            targetIndex--;
        }

        if (targetIndex == sourceIndex)
        {
            return UnityEngine.UIElements.DragVisualMode.Move;
        }

        MoveItem(listView.itemsSource, sourceIndex, targetIndex);
        listView.RefreshItems();

        Debug.Log($"OnHandleDrop: sourceIndex: {sourceIndex}, targetIndex: {targetIndex}, items: {JsonConverter.ToJson(listView.itemsSource)}");

        reorderEventStream.OnNext(new ReorderedEvent(sourceIndex, targetIndex, listView.itemsSource));

        dragStartSelectedIndex = null;
        return UnityEngine.UIElements.DragVisualMode.Move;
    }

    private static void MoveItem(IList list, int sourceIndex, int targetIndex)
    {
        if (sourceIndex < 0
            || sourceIndex >= list.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceIndex));
        }
        if (targetIndex < 0
            || targetIndex >= list.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(targetIndex));
        }

        object item = list[sourceIndex];
        list.RemoveAt(sourceIndex);

        list.Insert(targetIndex, item);
    }

    public class ReorderedEvent
    {
        public int SourceIndex { get; private set; }
        public int TargetIndex { get; private set; }
        public IList UpdatedItemsSource { get; private set; }

        public ReorderedEvent(int sourceIndex, int targetIndex, IList updatedItemsSource)
        {
            SourceIndex = sourceIndex;
            TargetIndex = targetIndex;
            UpdatedItemsSource = updatedItemsSource;
        }
    }
}
