using System.Collections.Generic;
using UnityEngine;

public interface IDragAndDropData
{
    object GetGenericData(string key);

    object userData { get; }

    IEnumerable<Object> unityObjectReferences { get; }
}
