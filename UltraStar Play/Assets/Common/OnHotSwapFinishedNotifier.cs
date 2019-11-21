using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Notifies all instances of IOnHotSwapFinishedListener in the scene at the appropriate times.
public class OnHotSwapFinishedNotifier : MonoBehaviour
{
    private bool startWasCalled;

    void OnEnable()
    {
        // Start() is not called after hot-swap, but OnEnable() is. 
        // Thus, if OnEnable() is called and Start() has been called before,
        // then it is assumed that we are called after hot-swap.
        if (startWasCalled)
        {
            NotifyListeners();
        }
    }

    void Start()
    {
        startWasCalled = true;
    }

    private void NotifyListeners()
    {
        IEnumerable<IOnHotSwapFinishedListener> listeners = FindObjectsOfType<MonoBehaviour>().OfType<IOnHotSwapFinishedListener>();
        foreach (IOnHotSwapFinishedListener listener in listeners)
        {
            listener.OnHotSwapFinished();
        }
    }
}