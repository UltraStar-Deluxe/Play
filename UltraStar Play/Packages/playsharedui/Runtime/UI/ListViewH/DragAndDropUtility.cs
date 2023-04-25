// Decompiled with JetBrains decompiler
// Assembly: UnityEngine.UIElementsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Unity 2022.2.4f1

using System;
using UnityEngine;

internal static class DragAndDropUtility
{
    private static Func<IDragAndDrop> s_MakeClientFunc;
    private static IDragAndDrop s_DragAndDrop;

    public static IDragAndDrop dragAndDrop
    {
        get
        {
            if (DragAndDropUtility.s_DragAndDrop == null)
                DragAndDropUtility.s_DragAndDrop = DragAndDropUtility.s_MakeClientFunc == null
                    ? (IDragAndDrop)new DefaultDragAndDropClient()
                    : DragAndDropUtility.s_MakeClientFunc();
            return DragAndDropUtility.s_DragAndDrop;
        }
    }

    internal static void RegisterMakeClientFunc(Func<IDragAndDrop> makeClient) => DragAndDropUtility.s_MakeClientFunc =
        DragAndDropUtility.s_MakeClientFunc == null
            ? makeClient
            : throw new UnityException("The MakeClientFunc has already been registered. Registration denied.");
}
