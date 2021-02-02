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

    private int cancelledInFrame;

    private List<InputActionSubscriber> performedSubscribers;
    
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
        
        void OnAddPerformedSubscriber(Action<InputAction.CallbackContext> onNext)
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

        return Observable.FromEvent<InputAction.CallbackContext>(OnAddPerformedSubscriber, OnRemoveSubscriber);
    }

    public void CancelForThisFrame()
    {
        cancelledInFrame = Time.frameCount;
    }

    private void NotifyPerformedSubscribers(InputAction.CallbackContext callbackContext)
    {
        performedSubscribers.ForEach(subscriber =>
        {
            if (cancelledInFrame != Time.frameCount)
            {
                subscriber.OnNext.Invoke(callbackContext);
            }
        });
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
