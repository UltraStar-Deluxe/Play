using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class HashSetChipsComboControl<T>
{
    public ChipsCombo ChipsCombo { get; private set; }
    public ReactiveProperty<HashSet<T>> Selection { get; private set; }
    public Func<T, string> GetLabelTextFunction { get; set; } = itemValue => itemValue != null ? itemValue.ToString() : "";

    private readonly IEnumerable<T> allValues;

    private VisualElement chipsComboDialog;

    public HashSetChipsComboControl(ChipsCombo chipsCombo, IEnumerable<T> allValues, HashSet<T> initialSelection = null)
    {
        chipsCombo.InitControl(this);
        this.ChipsCombo = chipsCombo;
        this.Selection = new(initialSelection ?? new HashSet<T>());
        this.allValues = allValues;

        ChipsCombo.ComboButton.RegisterCallbackButtonTriggered(_ => OpenValueChooserDialog());

        UpdateChipsComboEntries();
        Selection.Subscribe(_ => UpdateChipsComboEntries());
    }

    private void UpdateChipsComboEntries()
    {
        ChipsCombo.ChipsList.Clear();

        void CreateChipsComboEntry(T item)
        {
            VisualElement chipsComboEntryVisualElement = VisualElementUtils.LoadVisualElementFromResources("UIDocuments/ChipsComboEntry");
            ChipsCombo.ChipsList.Add(chipsComboEntryVisualElement);

            Label label = chipsComboEntryVisualElement.Q<Label>("chipsComboEntryLabel");
            label.text = GetLabelTextFunction(item);

            Button button = chipsComboEntryVisualElement.Q<Button>("chipsComboEntryButton");
            button.RegisterCallbackButtonTriggered(_ => RemoveFromSelection(item));
        }

        Selection.Value.ForEach(item => CreateChipsComboEntry(item));
    }

    private void OpenValueChooserDialog()
    {
        CloseValueChooserDialog();

        chipsComboDialog = VisualElementUtils.LoadVisualElementFromResources("UIDocuments/ChipsComboDialog");
        UIDocument uiDocument = GameObjectUtils.FindComponentWithTag<UIDocument>("UIDocument");
        uiDocument.rootVisualElement.Add(chipsComboDialog);

        ScrollView scrollView = chipsComboDialog.Q<ScrollView>("dialogContentScrollView");
        scrollView.Clear();

        VisualElement dialogButtonContainer = chipsComboDialog.Q<VisualElement>("dialogButtonContainer");
        Button closeButton = new();
        closeButton.text = "Close";
        closeButton.RegisterCallbackButtonTriggered(_ => CloseValueChooserDialog());
        dialogButtonContainer.Add(closeButton);

        void CreateValueCheckbox(T item)
        {
            string valueToggleLabelText = GetLabelTextFunction(item);
            Toggle valueToggle = new(valueToggleLabelText);
            valueToggle.AddToClassList("chipsDialogToggle");
            valueToggle.value = Selection.Value.Contains(item);
            valueToggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    AddToSelection(item);

                }
                else
                {
                    RemoveFromSelection(item);
                }
            });
            scrollView.Add(valueToggle);
        }

        allValues.ForEach(item => CreateValueCheckbox(item));
    }

    public void AddToSelection(T item)
    {
        HashSet<T> newSelectionValue = new(Selection.Value
            .Union(new List<T> { item })
            .ToHashSet());
        if (newSelectionValue != Selection.Value)
        {
            Selection.Value = newSelectionValue;
        }
    }

    public void RemoveFromSelection(T item)
    {
        HashSet<T> newSelectionValue = new(Selection.Value
            .Except(new List<T> { item })
            .ToHashSet());
        if (newSelectionValue != Selection.Value)
        {
            Selection.Value = newSelectionValue;
        }
    }

    public void Bind(Func<HashSet<T>> getter, Action<HashSet<T>> setter)
    {
        Selection.Value = getter.Invoke();
        Selection.Subscribe(newValue => setter.Invoke(newValue));
    }

    private void CloseValueChooserDialog()
    {
        if (chipsComboDialog == null)
        {
            return;
        }

        chipsComboDialog.RemoveFromHierarchy();
        chipsComboDialog = null;
    }

    public void SetSelection(HashSet<T> modifiers)
    {
        if (modifiers == null
            || modifiers.SetEquals(Selection.Value))
        {
            return;
        }

        Selection.Value = modifiers;
    }
}
