using UnityEngine;
using UniRx;
using UnityEngine.EventSystems;
using System;

public class OnSelectObserver : MonoBehaviour, ISelectHandler
{
    private Subject<BaseEventData> onSelectEventStream = new Subject<BaseEventData>();
    public IObservable<BaseEventData> OnSelectEventStream => onSelectEventStream;

    public void OnSelect(BaseEventData eventData)
    {
        onSelectEventStream.OnNext(eventData);
    }
}
