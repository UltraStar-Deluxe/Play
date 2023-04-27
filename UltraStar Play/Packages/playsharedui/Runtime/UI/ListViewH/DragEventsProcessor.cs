// Decompiled with JetBrains decompiler
// Assembly: UnityEngine.UIElementsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Unity 2022.2.4f1

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

public abstract class DragEventsProcessor
{
    private bool m_IsRegistered;
    internal DragEventsProcessor.DragState m_DragState;
    private Vector3 m_Start;
    internal readonly VisualElement m_Target;
    private const int k_DistanceToActivation = 5;
    internal DefaultDragAndDropClient dragAndDropClient;

    internal bool isRegistered => this.m_IsRegistered;

    internal virtual bool supportsDragEvents => true;

    internal bool useDragEvents => this.isEditorContext && this.supportsDragEvents;

    private bool isEditorContext
    {
        get
        {
            Assert.IsNotNull<VisualElement>(this.m_Target);
            Assert.IsNotNull<VisualElement>(this.m_Target.parent);
            return this.m_Target.panel.contextType == ContextType.Editor;
        }
    }

    internal DragEventsProcessor(VisualElement target)
    {
        this.m_Target = target;
        this.m_Target.RegisterCallback<AttachToPanelEvent>(
            new EventCallback<AttachToPanelEvent>(this.RegisterCallbacksFromTarget));
        this.m_Target.RegisterCallback<DetachFromPanelEvent>(
            new EventCallback<DetachFromPanelEvent>(this.UnregisterCallbacksFromTarget));
        this.RegisterCallbacksFromTarget();
    }

    private void RegisterCallbacksFromTarget(AttachToPanelEvent evt) => this.RegisterCallbacksFromTarget();

    private void RegisterCallbacksFromTarget()
    {
        if (this.m_IsRegistered)
            return;
        this.m_IsRegistered = true;
        this.m_Target.RegisterCallback<PointerDownEvent>(new EventCallback<PointerDownEvent>(this.OnPointerDownEvent),
            TrickleDown.TrickleDown);
        this.m_Target.RegisterCallback<PointerUpEvent>(new EventCallback<PointerUpEvent>(this.OnPointerUpEvent),
            TrickleDown.TrickleDown);
        this.m_Target.RegisterCallback<PointerLeaveEvent>(
            new EventCallback<PointerLeaveEvent>(this.OnPointerLeaveEvent));
        this.m_Target.RegisterCallback<PointerMoveEvent>(new EventCallback<PointerMoveEvent>(this.OnPointerMoveEvent));
        this.m_Target.RegisterCallback<PointerCancelEvent>(
            new EventCallback<PointerCancelEvent>(this.OnPointerCancelEvent));
        this.m_Target.RegisterCallback<PointerCaptureOutEvent>(
            new EventCallback<PointerCaptureOutEvent>(this.OnPointerCapturedOut));
        // this.m_Target.RegisterCallback<DragUpdatedEvent>(new EventCallback<DragUpdatedEvent>(this.OnDragUpdate));
        // this.m_Target.RegisterCallback<DragPerformEvent>(new EventCallback<DragPerformEvent>(this.OnDragPerformEvent));
        // this.m_Target.RegisterCallback<DragExitedEvent>(new EventCallback<DragExitedEvent>(this.OnDragExitedEvent));
    }

    private void UnregisterCallbacksFromTarget(DetachFromPanelEvent evt) => this.UnregisterCallbacksFromTarget();

    internal void UnregisterCallbacksFromTarget(bool unregisterPanelEvents = false)
    {
        this.m_IsRegistered = false;
        this.m_Target.UnregisterCallback<PointerDownEvent>(new EventCallback<PointerDownEvent>(this.OnPointerDownEvent),
            TrickleDown.TrickleDown);
        this.m_Target.UnregisterCallback<PointerUpEvent>(new EventCallback<PointerUpEvent>(this.OnPointerUpEvent),
            TrickleDown.TrickleDown);
        this.m_Target.UnregisterCallback<PointerLeaveEvent>(
            new EventCallback<PointerLeaveEvent>(this.OnPointerLeaveEvent));
        this.m_Target.UnregisterCallback<PointerMoveEvent>(
            new EventCallback<PointerMoveEvent>(this.OnPointerMoveEvent));
        this.m_Target.UnregisterCallback<PointerCancelEvent>(
            new EventCallback<PointerCancelEvent>(this.OnPointerCancelEvent));
        this.m_Target.UnregisterCallback<PointerCaptureOutEvent>(
            new EventCallback<PointerCaptureOutEvent>(this.OnPointerCapturedOut));
        // this.m_Target.UnregisterCallback<DragUpdatedEvent>(new EventCallback<DragUpdatedEvent>(this.OnDragUpdate));
        // this.m_Target.UnregisterCallback<DragPerformEvent>(new EventCallback<DragPerformEvent>(this.OnDragPerformEvent));
        // this.m_Target.UnregisterCallback<DragExitedEvent>(new EventCallback<DragExitedEvent>(this.OnDragExitedEvent));
        if (!unregisterPanelEvents)
            return;
        this.m_Target.UnregisterCallback<AttachToPanelEvent>(
            new EventCallback<AttachToPanelEvent>(this.RegisterCallbacksFromTarget));
        this.m_Target.UnregisterCallback<DetachFromPanelEvent>(
            new EventCallback<DetachFromPanelEvent>(this.UnregisterCallbacksFromTarget));
    }

    protected abstract bool CanStartDrag(Vector3 pointerPosition);

    protected internal abstract StartDragArgs StartDrag(Vector3 pointerPosition);

    protected internal abstract DragVisualMode UpdateDrag(Vector3 pointerPosition);

    protected internal abstract void OnDrop(Vector3 pointerPosition);

    protected abstract void ClearDragAndDropUI();

    private void OnPointerDownEvent(PointerDownEvent evt)
    {
        if (evt.button != 0)
        {
            this.m_DragState = DragEventsProcessor.DragState.None;
        }
        else
        {
            if (!this.CanStartDrag(evt.position))
                return;
            this.m_DragState = DragEventsProcessor.DragState.CanStartDrag;
            this.m_Start = evt.position;
        }
    }

    internal void OnPointerUpEvent(PointerUpEvent evt)
    {
        if (!this.useDragEvents && this.m_DragState == DragEventsProcessor.DragState.Dragging)
        {
            this.m_Target.ReleasePointer(evt.pointerId);
            this.OnDrop(evt.position);
            this.ClearDragAndDropUI();
            evt.StopPropagation();
        }

        this.m_DragState = DragEventsProcessor.DragState.None;
    }

    private void OnPointerLeaveEvent(PointerLeaveEvent evt)
    {
        if (evt.target != this.m_Target)
            return;
        this.ClearDragAndDropUI();
    }

    private void OnPointerCancelEvent(PointerCancelEvent evt)
    {
        if (this.useDragEvents)
            return;
        this.ClearDragAndDropUI();
    }

    private void OnPointerCapturedOut(PointerCaptureOutEvent evt)
    {
        if (!this.useDragEvents)
            this.ClearDragAndDropUI();
        this.m_DragState = DragEventsProcessor.DragState.None;
    }

    // private void OnDragExitedEvent(DragExitedEvent evt)
    // {
    //     if (!this.useDragEvents)
    //         return;
    //     this.ClearDragAndDropUI();
    // }
    //
    // private void OnDragPerformEvent(DragPerformEvent evt)
    // {
    //     if (!this.useDragEvents)
    //         return;
    //     this.m_DragState = DragEventsProcessor.DragState.None;
    //     this.OnDrop((Vector3)evt.mousePosition);
    //     this.ClearDragAndDropUI();
    //     DragAndDropUtility.dragAndDrop.AcceptDrag();
    // }
    //
    // private void OnDragUpdate(DragUpdatedEvent evt)
    // {
    //     if (!this.useDragEvents)
    //         return;
    //     DragAndDropUtility.dragAndDrop.SetVisualMode(this.UpdateDrag((Vector3)evt.mousePosition));
    // }

    private void OnPointerMoveEvent(PointerMoveEvent evt)
    {
        if (this.useDragEvents)
        {
            if (this.m_DragState != DragEventsProcessor.DragState.CanStartDrag)
                return;
        }
        else
        {
            if (this.m_DragState == DragEventsProcessor.DragState.Dragging)
            {
                int num = (int)this.UpdateDrag(evt.position);
                return;
            }

            if (this.m_DragState != DragEventsProcessor.DragState.CanStartDrag)
                return;
        }

        if ((double)Mathf.Abs(this.m_Start.x - evt.position.x) <= 5.0 &&
            (double)Mathf.Abs(this.m_Start.y - evt.position.y) <= 5.0)
            return;
        StartDragArgs args = this.StartDrag(this.m_Start);
        if (this.useDragEvents)
        {
            if (Event.current.type != EventType.MouseDown && Event.current.type != EventType.MouseDrag)
                return;
            DragAndDropUtility.dragAndDrop.StartDrag(args);
        }
        else
        {
            this.m_Target.CapturePointer(evt.pointerId);
            evt.StopPropagation();
            this.dragAndDropClient = new DefaultDragAndDropClient();
            this.dragAndDropClient.StartDrag(args);
        }

        this.m_DragState = DragEventsProcessor.DragState.Dragging;
    }

    internal enum DragState
    {
        None,
        CanStartDrag,
        Dragging,
    }
}
