using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UniRx;
using UniRx.Triggers;

public static class InputActionExtensions
{
    public static IObservable<InputAction.CallbackContext> PerformedAsObservable(this InputAction inputAction)
    {
        Action<Action<InputAction.CallbackContext>> removeHandler = h => inputAction.performed -= h;
        Action<Action<InputAction.CallbackContext>> addHandler = h =>
        {
            inputAction.performed += h;
            InputManager.Instance.OnDestroyAsObservable().Subscribe(_ => removeHandler(h));
        };
        return Observable.FromEvent<InputAction.CallbackContext>(addHandler, removeHandler);
    }
    
    public static IObservable<InputAction.CallbackContext> StartedAsObservable(this InputAction inputAction)
    {
        Action<Action<InputAction.CallbackContext>> removeHandler = h => inputAction.started -= h;
        Action<Action<InputAction.CallbackContext>> addHandler = h =>
        {
            inputAction.started += h;
            InputManager.Instance.OnDestroyAsObservable().Subscribe(_ => removeHandler(h));
        };
        return Observable.FromEvent<InputAction.CallbackContext>(addHandler, removeHandler);
    }
    
    public static IObservable<InputAction.CallbackContext> CanceledAsObservable(this InputAction inputAction)
    {
        Action<Action<InputAction.CallbackContext>> removeHandler = h => inputAction.canceled -= h;
        Action<Action<InputAction.CallbackContext>> addHandler = h =>
        {
            inputAction.canceled += h;
            InputManager.Instance.OnDestroyAsObservable().Subscribe(_ => removeHandler(h));
        };
        return Observable.FromEvent<InputAction.CallbackContext>(addHandler, removeHandler);
    }
}
