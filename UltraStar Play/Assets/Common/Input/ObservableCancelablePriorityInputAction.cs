using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UniRx;
using UniRx.Triggers;

/**
 * Wrapper for an InputAction.
 * Provides methods to create an Observable for the InputAction's events.
 * Thereby, subscribers are notified in the order of their priority.
 * When cancelled, then following subscribers in the queue will not be notified for this frame.
 * Furthermore, all registered events are removed when the Owner GameObject is Destroyed.
 */
public class ObservableCancelablePriorityInputAction
{
    public GameObject Owner { get; private set; }
    public InputAction InputAction { get; private set; }

    private int notifyCancelledInFrame;

    private List<InputActionSubscriber> performedSubscribers;
    private List<InputActionSubscriber> startedSubscribers;
    private List<InputActionSubscriber> canceledSubscribers;
    
    public ObservableCancelablePriorityInputAction(InputAction inputAction, GameObject owner)
    {
        this.InputAction = inputAction;
        this.Owner = owner;
    }
    
    public IObservable<InputAction.CallbackContext> PerformedAsObservable(int priority = 0)
    {
        void OnRemoveSubscriber(Action<InputAction.CallbackContext> onNext)
        {
            InputActionSubscriber subscriber = performedSubscribers
                .FirstOrDefault(it => it.Priority == priority && it.OnNext == onNext);
            performedSubscribers.Remove(subscriber);

            // No subscriber left in event queue. Thus, remove the callback from the InputAction.
            if (performedSubscribers.Count == 0)
            {
                InputAction.performed -= NotifyPerformedSubscribers;
                performedSubscribers = null;
            }
        }
        
        void OnAddSubscriber(Action<InputAction.CallbackContext> onNext)
        {
            // Add the one callback that will update all subscribers
            if (performedSubscribers == null)
            {
                performedSubscribers = new List<InputActionSubscriber>();
                InputAction.performed += NotifyPerformedSubscribers;
                Owner.OnDestroyAsObservable().Subscribe(_ => InputAction.performed -= NotifyPerformedSubscribers);
            }

            performedSubscribers.Add(new InputActionSubscriber(priority, onNext));
            // Sort by priority
            performedSubscribers.Sort();
        }

        return Observable.FromEvent<InputAction.CallbackContext>(OnAddSubscriber, OnRemoveSubscriber);
    }

    public IObservable<InputAction.CallbackContext> StartedAsObservable(int priority = 0)
    {
        void OnRemoveSubscriber(Action<InputAction.CallbackContext> onNext)
        {
            InputActionSubscriber subscriber = startedSubscribers
                .FirstOrDefault(it => it.Priority == priority && it.OnNext == onNext);
            startedSubscribers.Remove(subscriber);

            // No subscriber left in event queue. Thus, remove the callback from the InputAction.
            if (startedSubscribers.Count == 0)
            {
                InputAction.started -= NotifyStartedSubscribers;
                startedSubscribers = null;
            }
        }
        
        void OnAddSubscriber(Action<InputAction.CallbackContext> onNext)
        {
            // Add the one callback that will update all subscribers
            if (startedSubscribers == null)
            {
                startedSubscribers = new List<InputActionSubscriber>();
                InputAction.started += NotifyStartedSubscribers;
                Owner.OnDestroyAsObservable().Subscribe(_ => InputAction.started -= NotifyStartedSubscribers);
            }

            startedSubscribers.Add(new InputActionSubscriber(priority, onNext));
            // Sort by priority
            startedSubscribers.Sort();
        }

        return Observable.FromEvent<InputAction.CallbackContext>(OnAddSubscriber, OnRemoveSubscriber);
    }
    
    public IObservable<InputAction.CallbackContext> CanceledAsObservable(int priority = 0)
    {
        void OnRemoveSubscriber(Action<InputAction.CallbackContext> onNext)
        {
            InputActionSubscriber subscriber = canceledSubscribers
                .FirstOrDefault(it => it.Priority == priority && it.OnNext == onNext);
            canceledSubscribers.Remove(subscriber);

            // No subscriber left in event queue. Thus, remove the callback from the InputAction.
            if (canceledSubscribers.Count == 0)
            {
                InputAction.canceled -= NotifyCanceledSubscribers;
                canceledSubscribers = null;
            }
        }
        
        void OnAddSubscriber(Action<InputAction.CallbackContext> onNext)
        {
            // Add the one callback that will update all subscribers
            if (canceledSubscribers == null)
            {
                canceledSubscribers = new List<InputActionSubscriber>();
                InputAction.canceled += NotifyCanceledSubscribers;
                Owner.OnDestroyAsObservable().Subscribe(_ => InputAction.canceled -= NotifyCanceledSubscribers);
            }

            canceledSubscribers.Add(new InputActionSubscriber(priority, onNext));
            // Sort by priority
            canceledSubscribers.Sort();
        }

        return Observable.FromEvent<InputAction.CallbackContext>(OnAddSubscriber, OnRemoveSubscriber);
    }
    
    public void CancelNotifyForThisFrame()
    {
        notifyCancelledInFrame = Time.frameCount;
    }

    private void NotifyStartedSubscribers(InputAction.CallbackContext callbackContext)
    {
        // Iteration over index to enable removing subscribers as part of the callback (i.e., during iteration).
        for (int i = 0; startedSubscribers != null && i < startedSubscribers.Count; i++)
        {
            InputActionSubscriber subscriber = startedSubscribers[i];
            if (notifyCancelledInFrame != Time.frameCount)
            {
                subscriber.OnNext.Invoke(callbackContext);
            }
        }
    }
    
    private void NotifyPerformedSubscribers(InputAction.CallbackContext callbackContext)
    {
        // Iteration over index to enable removing subscribers as part of the callback (i.e., during iteration).
        for (int i = 0; performedSubscribers != null && i < performedSubscribers.Count; i++)
        {
            InputActionSubscriber subscriber = performedSubscribers[i];
            if (notifyCancelledInFrame != Time.frameCount)
            {
                subscriber.OnNext.Invoke(callbackContext);
            }
        }
    }
    
    private void NotifyCanceledSubscribers(InputAction.CallbackContext callbackContext)
    {
        // Iteration over index to enable removing subscribers as part of the callback (i.e., during iteration).
        for (int i = 0; canceledSubscribers != null && i < canceledSubscribers.Count; i++)
        {
            InputActionSubscriber subscriber = canceledSubscribers[i];
            if (notifyCancelledInFrame != Time.frameCount)
            {
                subscriber.OnNext.Invoke(callbackContext);
            }
        }
    }

    private class InputActionSubscriber : IComparable<InputActionSubscriber>
    {
        public int Priority { get; private set; }
        public Action<InputAction.CallbackContext> OnNext { get; private set; }

        public InputActionSubscriber(int priority, Action<InputAction.CallbackContext> onNext)
        {
            Priority = priority;
            OnNext = onNext;
        }

        public int CompareTo(InputActionSubscriber other)
        {
            return Priority.CompareTo(other.Priority);
        }
    }
}
