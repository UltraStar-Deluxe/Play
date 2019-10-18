using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SearchInputField : MonoBehaviour
{

    private InputField inputField;
    private Text placeholderText;

    public enum ESearchMode
    {
        BySongTitle,
        ByArtist
    }

    private ESearchMode searchMode = ESearchMode.BySongTitle;
    public ESearchMode SearchMode
    {
        get
        {
            return searchMode;
        }
        set
        {
            searchMode = value;
            UpdatePlaceholderText();
        }
    }

    public string Text
    {
        get
        {
            return inputField.text;
        }
        set
        {
            inputField.text = value;
        }
    }

    void Awake()
    {
        inputField = GetComponentInChildren<InputField>();
        placeholderText = inputField.placeholder as Text;
        UpdatePlaceholderText();
        Hide();
    }

    void Update()
    {
        // Move caret always to the end
        if (inputField.caretPosition < Text.Length)
        {
            inputField.caretPosition = Text.Length;
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void RequestFocus()
    {
        EventSystem.current.SetSelectedGameObject(gameObject, null);
        inputField.ActivateInputField();
    }

    private void UpdatePlaceholderText()
    {
        switch (searchMode)
        {
            case ESearchMode.BySongTitle:
                placeholderText.text = "Search song title...";
                break;
            case ESearchMode.ByArtist:
                placeholderText.text = "Search artist...";
                break;
            default:
                // Do nothing
                break;
        }
    }

    public void SetSearchText(string text)
    {
        inputField.text = text;
    }
}
