using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UniRx;
using UniRx.Triggers;

/**
 * Provides methods to create an Observable from InputAction events.
 * Thereby all registered events are removed when the Owner GameObject is Destroyed.
 */
public class ObservableInputAction
{
    public GameObject Owner { get; private set; }
    public InputAction InputAction { get; private set; }
    
    public ObservableInputAction(InputAction inputAction, GameObject owner)
    {
        this.InputAction = inputAction;
        this.Owner = owner;
    }
    
    public IObservable<InputAction.CallbackContext> PerformedAsObservable()
    {
        Action<Action<InputAction.CallbackContext>> removeHandler = h => InputAction.performed -= h;
        Action<Action<InputAction.CallbackContext>> addHandler = h =>
        {
            InputAction.performed += h;
            Owner.OnDestroyAsObservable().Subscribe(_ => removeHandler(h));
        };
        return Observable.FromEvent<InputAction.CallbackContext>(addHandler, removeHandler);
    }
    
    public IObservable<InputAction.CallbackContext> StartedAsObservable()
    {
        Action<Action<InputAction.CallbackContext>> removeHandler = h => InputAction.started -= h;
        Action<Action<InputAction.CallbackContext>> addHandler = h =>
        {
            InputAction.started += h;
            Owner.OnDestroyAsObservable().Subscribe(_ => removeHandler(h));
        };
        return Observable.FromEvent<InputAction.CallbackContext>(addHandler, removeHandler);
    }
    
    public IObservable<InputAction.CallbackContext> CanceledAsObservable()
    {
        Action<Action<InputAction.CallbackContext>> removeHandler = h => InputAction.canceled -= h;
        Action<Action<InputAction.CallbackContext>> addHandler = h =>
        {
            InputAction.canceled += h;
            Owner.OnDestroyAsObservable().Subscribe(_ => removeHandler(h));
        };
        return Observable.FromEvent<InputAction.CallbackContext>(addHandler, removeHandler);
    }
}
