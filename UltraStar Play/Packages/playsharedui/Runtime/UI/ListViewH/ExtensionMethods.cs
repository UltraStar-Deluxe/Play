// Decompiled with JetBrains decompiler
// Assembly: UnityEngine.UIElementsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Unity 2022.2.4f1

using System.Collections.Generic;
using UnityEngine.Pool;
using UnityEngine.UIElements;

public static class ExtensionMethods
{
    public static void RemoveItems(this BaseListViewHController self, int itemCount)
    {
        if (itemCount <= 0)
            return;
        int itemsCount = self.GetItemsCount();
        List<int> intList = CollectionPool<List<int>, int>.Get();
        try
        {
            for (int index = itemsCount - itemCount; index < itemsCount; ++index)
                intList.Add(index);
            self.RemoveItems(intList);
        }
        finally
        {
            CollectionPool<List<int>, int>.Release(intList);
        }
    }

    public static int GetItemsMinCount(this CollectionViewController self) => self.GetItemsCount();

    public static bool FindElementInTree(this VisualElement self, VisualElement element, List<int> outChildIndexes)
    {
        VisualElement element1 = element;
        VisualElement.Hierarchy hierarchy;
        for (VisualElement parent = element1.hierarchy.parent; parent != null; parent = hierarchy.parent)
        {
            List<int> intList = outChildIndexes;
            hierarchy = parent.hierarchy;
            int num = hierarchy.IndexOf(element1);
            intList.Insert(0, num);
            if (parent == self)
                return true;
            element1 = parent;
            hierarchy = parent.hierarchy;
        }

        outChildIndexes.Clear();
        return false;
    }

    public static VisualElement ElementAtTreePath(this VisualElement self, List<int> childIndexes)
    {
        VisualElement visualElement = self;
        foreach (int childIndex in childIndexes)
        {
            if (childIndex < 0 || childIndex >= visualElement.hierarchy.childCount)
                return (VisualElement)null;
            visualElement = visualElement.hierarchy[childIndex];
        }

        return visualElement;
    }

    public static ReusableCollectionItem GetRecycledItemFromIndex(
        this BaseHorizontalCollectionView listView,
        int index)
    {
        foreach (ReusableCollectionItem activeItem in listView.activeItems)
        {
            if (activeItem.index.Equals(index))
                return activeItem;
        }

        return (ReusableCollectionItem)null;
    }
}
