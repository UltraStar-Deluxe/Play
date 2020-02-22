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
        ByTitleOrArtist
    }

    private ESearchMode searchMode = ESearchMode.ByTitleOrArtist;
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
        placeholderText.text = "Search...";
    }

    public void SetSearchText(string text)
    {
        inputField.text = text;
    }
}
