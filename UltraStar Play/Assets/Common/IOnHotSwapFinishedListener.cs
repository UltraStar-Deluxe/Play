using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Interface with a method that is called in Start() as well as in OnEnable() after a hot-reload (aka. hot-swap).
// This is needed, because sometimes OnEnable() is too early but Start() is not called after a hot-swap.
// For example, InvokeRepeating(...) calls, which are lost in a hot-swap, can be re-created in the method of this interface.
public interface IOnHotSwapFinishedListener
{
    void OnHotSwapFinished();
}
