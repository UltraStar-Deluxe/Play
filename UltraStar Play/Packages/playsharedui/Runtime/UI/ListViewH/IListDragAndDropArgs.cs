// Decompiled with JetBrains decompiler
// Assembly: UnityEngine.UIElementsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Unity 2022.2.4f1

public interface IListDragAndDropArgs
{
    object target { get; }

    int insertAtIndex { get; }

    IDragAndDropData dragAndDropData { get; }

    DragAndDropPosition dragAndDropPosition { get; }
}
