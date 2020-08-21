using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class PointerEnterExitListener : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Action<PointerEventData> onEnterAction;
    public Action<PointerEventData> onExitAction;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (onEnterAction != null)
        {
            onEnterAction(eventData);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (onExitAction != null)
        {
            onExitAction(eventData);
        }
    }
}
