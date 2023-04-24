// Decompiled with JetBrains decompiler
// Assembly: UnityEngine.UIElementsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Unity 2022.2.4f1

using System;
using UnityEngine.UIElements;

/// <summary>
///        <para>
/// List view controller. View controllers of this type are meant to take care of data virtualized by any ListView inheritor.
/// </para>
///      </summary>
public class ListViewHController : BaseListViewHController
{
    /// <summary>
    ///        <para>
    /// View for this controller, cast as a ListView.
    /// </para>
    ///      </summary>
    protected ListViewH listView => this.view as ListViewH;

    protected override VisualElement MakeItem()
    {
        if (this.listView.makeItem != null)
            return this.listView.makeItem();
        if (this.listView.bindItem != null)
            throw new NotImplementedException("You must specify makeItem if bindItem is specified.");
        return (VisualElement)new Label();
    }

    protected override void BindItem(VisualElement element, int index)
    {
        if (this.listView.bindItem == null)
        {
            if (this.listView.makeItem != null)
                throw new NotImplementedException("You must specify bindItem if makeItem is specified.");
            ((TextElement)element).text = this.listView.itemsSource[index]?.ToString() ?? "null";
        }
        else
            this.listView.bindItem(element, index);
    }

    protected override void UnbindItem(VisualElement element, int index)
    {
        Action<VisualElement, int> unbindItem = this.listView.unbindItem;
        if (unbindItem == null)
            return;
        unbindItem(element, index);
    }

    protected override void DestroyItem(VisualElement element)
    {
        Action<VisualElement> destroyItem = this.listView.destroyItem;
        if (destroyItem == null)
            return;
        destroyItem(element);
    }
}
