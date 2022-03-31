using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CoroutineManager : MonoBehaviour
{
    public static CoroutineManager Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<CoroutineManager>("CoroutineManager");
        }
    }

    public List<IEnumerator> CoroutinesInProgress { get; private set; } = new();

    /**
     * Starts a coroutine that will also be execute even when in Edit Mode.
     * This is handled by the EditorCoroutineManager script.
     */
    public void StartCoroutineAlsoForEditor(IEnumerator coroutine)
    {
        StartCoroutine(coroutine);
        CoroutinesInProgress.Add(coroutine);
    }
}
