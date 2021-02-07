using System;
using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SearchInputField : MonoBehaviour, INeedInjection
{
    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private InputField inputField;
    
    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    public RectTransformSlideIntoViewport RectTransformSlideIntoViewport { get; private set; }

    [Inject]
    public EventSystem eventSystem;
    
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

    void Start()
    {
        placeholderText = inputField.placeholder as Text;
        UpdatePlaceholderText();
    }

    void Update()
    {
        // Move caret always to the end
        if (inputField.caretPosition < Text.Length)
        {
            inputField.caretPosition = Text.Length;
        }

        if (eventSystem.currentSelectedGameObject == inputField.gameObject)
        {
            RectTransformSlideIntoViewport.SlideIn();
        }
        else if(!RectTransformUtils.IsMouseOverRectTransform(RectTransformSlideIntoViewport.triggerArea))
        {
            RectTransformSlideIntoViewport.SlideOut();
        }
    }

    public void Show()
    {
        RectTransformSlideIntoViewport.SlideIn();
    }

    public void Hide()
    {
        RectTransformSlideIntoViewport.SlideOut();
        eventSystem.SetSelectedGameObject(null);
    }

    public void RequestFocus()
    {
        inputField.Select();
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

    public InputField GetInputField()
    {
        return inputField;
    }
}
