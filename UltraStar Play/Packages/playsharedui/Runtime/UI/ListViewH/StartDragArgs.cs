// Decompiled with JetBrains decompiler
// Assembly: UnityEngine.UIElementsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Unity 2022.2.4f1

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartDragArgs
{
    private readonly Hashtable m_GenericData = new Hashtable();

    public string title { get; }

    public object userData { get; }

    internal Hashtable genericData => this.m_GenericData;

    internal IEnumerable<Object> unityObjectReferences { get; private set; } = (IEnumerable<Object>)null;

    internal StartDragArgs() => this.title = string.Empty;

    public StartDragArgs(string title, object userData)
    {
        this.title = title;
        this.userData = userData;
    }

    public void SetGenericData(string key, object data) => this.m_GenericData[(object)key] = data;

    public void SetUnityObjectReferences(IEnumerable<Object> references) => this.unityObjectReferences = references;
}
