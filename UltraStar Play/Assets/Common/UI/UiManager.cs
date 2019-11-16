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

    private Canvas canvas;
    public WarningDialog warningDialogPrefab;

    void Awake()
    {
        canvas = FindObjectOfType<Canvas>();
    }

    public WarningDialog CreateWarningDialog(string title, string message)
    {
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
