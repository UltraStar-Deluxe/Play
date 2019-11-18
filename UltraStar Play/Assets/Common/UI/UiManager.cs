using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UiManager : MonoBehaviour
{
    public static UiManager Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<UiManager>("UiManager");
        }
    }

    public WarningDialog warningDialogPrefab;

    public WarningDialog CreateWarningDialog(string title, string message)
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        WarningDialog warningDialog = Instantiate(warningDialogPrefab);
        warningDialog.transform.SetParent(canvas.transform);
        warningDialog.transform.SetAsLastSibling();
        warningDialog.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        if (title != null)
        {
            warningDialog.Title = title;
        }
        if (message != null)
        {
            warningDialog.Message = message;
        }
        return warningDialog;
    }
}
