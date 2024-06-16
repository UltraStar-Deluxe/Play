// Decompiled with JetBrains decompiler
// Assembly: UnityEngine.UIElementsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Unity 2022.2.4f1

using System.Collections.Generic;
using UnityEngine;

public class DefaultDragAndDropClient : IDragAndDrop, IDragAndDropData
{
    private StartDragArgs m_StartDragArgs;

    public object userData => this.m_StartDragArgs?.userData;

    public IEnumerable<Object> unityObjectReferences => this.m_StartDragArgs?.unityObjectReferences;

    public void StartDrag(StartDragArgs args) => this.m_StartDragArgs = args;

    public void AcceptDrag() => this.m_StartDragArgs = (StartDragArgs)null;

    public void SetVisualMode(DragVisualMode visualMode)
    {
    }

    public IDragAndDropData data => (IDragAndDropData)this;

    public object GetGenericData(string key) => this.m_StartDragArgs == null
        ? (object)null
        : (this.m_StartDragArgs.genericData.ContainsKey((object)key)
            ? this.m_StartDragArgs.genericData[(object)key]
            : (object)null);
}
