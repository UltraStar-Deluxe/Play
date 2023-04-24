// Decompiled with JetBrains decompiler
// Assembly: UnityEngine.UIElementsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Unity 2022.2.4f1

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class CollectionVirtualizationController
{
    protected readonly ScrollView m_ScrollView;

    public abstract int firstVisibleIndex { get; protected set; }

    public abstract int visibleItemCount { get; }

    protected CollectionVirtualizationController(ScrollView scrollView) => this.m_ScrollView = scrollView;

    public abstract void Refresh(bool rebuild);

    public abstract void ScrollToItem(int id);

    public abstract void Resize(Vector2 size);

    public abstract void OnScroll(Vector2 offset);

    public abstract int GetIndexFromPosition(Vector2 position);

    public abstract float GetExpectedItemWidth(int index);

    public abstract float GetExpectedContentWidth();

    public abstract void OnFocus(VisualElement leafTarget);

    public abstract void OnBlur(VisualElement willFocus);

    public abstract void UpdateBackground();

    public abstract IEnumerable<ReusableCollectionItem> activeItems { get; }

    internal abstract void StartDragItem(ReusableCollectionItem item);

    internal abstract void EndDrag(int dropIndex);
}
