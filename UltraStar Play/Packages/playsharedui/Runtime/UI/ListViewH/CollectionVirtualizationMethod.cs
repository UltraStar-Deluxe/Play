// Decompiled with JetBrains decompiler
// Assembly: UnityEngine.UIElementsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Unity 2022.2.4f1

/// <summary>
///        <para>
/// Options to change the virtualization method used by the collection view to display its content.
/// </para>
///      </summary>
public enum CollectionVirtualizationMethod
{
    /// <summary>
    ///        <para>
    /// Collection view won't wait for the layout to update items, as the all have the same width. fixedItemWidth Needs to be set. More performant but less flexible.
    /// </para>
    ///      </summary>
    Fixed,

    /// <summary>
    ///        <para>
    /// Collection view will use the actual width of every item when geometry changes. More flexible but less performant.
    /// </para>
    ///      </summary>
    Dynamic,
}
