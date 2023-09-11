using System;
using System.Linq;
using IngameDebugConsole;
using UniInject;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class RuntimeUiInspectionManager : AbstractSingletonBehaviour, INeedInjection
{
    public static RuntimeUiInspectionManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<RuntimeUiInspectionManager>();

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private PanelHelper panelHelper;

    private bool isInspectUiEnabled;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        DebugLogConsole.AddCommand("ui.hierarchy", "Copy and log VisualElement hierarchy of current UIDocument",
            () =>
            {
                string uiHierarchyAsString = uiDocument.rootVisualElement.ToUxml();
                Debug.Log($"{uiHierarchyAsString}");
                ClipboardUtils.CopyToClipboard(uiHierarchyAsString);
            });

        DebugLogConsole.AddCommand("ui.inspect", "Toggle inspect mode. If enabled, shows info about VisualElement under mouse pointer on click.",
            () => isInspectUiEnabled = !isInspectUiEnabled);
    }

    private void Update()
    {
        UpdateInspectUi();
    }

    private void UpdateInspectUi()
    {
        if (!isInspectUiEnabled
            || Mouse.current == null
            || !Mouse.current.leftButton.wasReleasedThisFrame)
        {
            return;
        }

        VisualElement elementUnderPointer = VisualElementUtils.GetElementUnderPointer(uiDocument, panelHelper);
        if (elementUnderPointer == null)
        {
            return;
        }

        UiManager.CreateNotification($"type: {elementUnderPointer.GetType().Name}\n" +
                                     $"name: {elementUnderPointer.name}\n" +
                                     $"style sheet classes: {elementUnderPointer.GetClasses().ToList().ToCsv(", ")}");
    }
}

