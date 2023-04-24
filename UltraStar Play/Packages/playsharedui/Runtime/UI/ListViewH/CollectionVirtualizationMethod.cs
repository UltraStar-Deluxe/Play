// Decompiled with JetBrains decompiler
// Type: UnityEngine.UIElements.CollectionVirtualizationMethod
// Assembly: UnityEngine.UIElementsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 0619D653-D2D9-4223-8C58-14F58DF39D69
// Assembly location: F:\Dev\Tools\Unity3D\UnityEditor\2022.2.4f1\Editor\Data\Managed\UnityEngine\UnityEngine.UIElementsModule.dll
// XML documentation location: F:\Dev\Tools\Unity3D\UnityEditor\2022.2.4f1\Editor\Data\Managed\UnityEngine\UnityEngine.UIElementsModule.xml

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
