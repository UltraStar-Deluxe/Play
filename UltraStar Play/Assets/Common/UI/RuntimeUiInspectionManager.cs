using System;
using System.Linq;
using System.Reflection;
using System.Text;
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

    private bool inspectionEnabled;

    private VisualElement inspectionFrame;
    private Label inspectionLabel;

    private Vector2 lastMousePosition;

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

        DebugLogConsole.AddCommand("ui.inspect", "Toggle UI inspection. " +
                                                 "Shows info about element under mouse pointer. " +
                                                 "Copies details of the element on click.",
            () =>
            {
                inspectionEnabled = !inspectionEnabled;
                Debug.Log($"UI inspection: {inspectionEnabled}");
            });
    }

    private void Update()
    {
        UpdateInspectUi();
    }

    private void UpdateInspectUi()
    {
        if (!inspectionEnabled
            || Mouse.current == null)
        {
            DestroyInspectionElements();
            return;
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            inspectionEnabled = false;

            // Copy details of element under pointer to clipboard
            VisualElement elementUnderPointer = VisualElementUtils.GetElementUnderPointer(uiDocument, panelHelper);
            if (elementUnderPointer != null)
            {
                string text = $"{elementUnderPointer.ToUxml()}\n" +
                              $"{GetStyleAttributes(elementUnderPointer)}";
                ClipboardUtils.CopyToClipboard(text);
            }
        }
        else if (lastMousePosition != Mouse.current.position.value)
        {
            lastMousePosition = Mouse.current.position.value;

            VisualElement elementUnderPointer = VisualElementUtils.GetElementUnderPointer(uiDocument, panelHelper);
            UpdateInspectionFrame(elementUnderPointer);
            UpdateInspectionLabel(elementUnderPointer);
        }
    }

    private string GetStyleAttributes(VisualElement target)
    {
        Type styleType = typeof(IStyle);

        StringBuilder sb = new();
        FieldInfo[] fieldInfos = styleType.GetFields(BindingFlags.Instance | BindingFlags.Public);
        foreach (FieldInfo fieldInfo in fieldInfos)
        {
            try
            {
                object fieldValue = fieldInfo.GetValue(target.style);
                sb.Append($"{fieldInfo.Name} = {fieldValue}\n");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        PropertyInfo[] propertyInfos = styleType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (PropertyInfo propertyInfo in propertyInfos)
        {
            try
            {
                object propertyValue = propertyInfo.GetValue(target.style);
                sb.Append($"{propertyInfo.Name} = {propertyValue}\n");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        return sb.ToString();
    }

    private void UpdateInspectionLabel(VisualElement target)
    {
        if (inspectionLabel == null)
        {
            inspectionLabel = new();
            inspectionLabel.name = "runtimeUiInspectionLabel";
            inspectionLabel.pickingMode = PickingMode.Ignore;
            uiDocument.rootVisualElement.Add(inspectionLabel);
        }
        inspectionLabel.BringToFront();

        string targetType = target.GetType().Name;
        string targetName = target.name;
        string targetClasses = target.GetClasses().ToList().JoinWith(" ");
        inspectionLabel.text = $"<{targetType} name=\"{targetName}\" class=\"{targetClasses}\"/>";
    }

    private void UpdateInspectionFrame(VisualElement target)
    {
        if (inspectionFrame == null)
        {
            inspectionFrame = new();
            inspectionFrame.name = "runtimeUiInspectionFrame";
            inspectionFrame.pickingMode = PickingMode.Ignore;
            uiDocument.rootVisualElement.Add(inspectionFrame);
        }
        inspectionFrame.BringToFront();

        Rect targetWorldBound = target.worldBound;
        inspectionFrame.style.position = new StyleEnum<Position>(Position.Absolute);
        inspectionFrame.style.left = targetWorldBound.xMin;
        inspectionFrame.style.top = targetWorldBound.yMin;
        inspectionFrame.style.width = targetWorldBound.width;
        inspectionFrame.style.height = targetWorldBound.height;
    }

    private void DestroyInspectionElements()
    {
        if (inspectionFrame != null)
        {
            inspectionFrame.RemoveFromHierarchy();
            inspectionFrame = null;
        }

        if (inspectionLabel != null)
        {
            inspectionLabel.RemoveFromHierarchy();
            inspectionLabel = null;
        }
    }
}

