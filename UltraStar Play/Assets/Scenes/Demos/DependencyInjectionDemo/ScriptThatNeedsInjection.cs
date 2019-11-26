using UniInject.Attributes;
using UnityEngine;

// Ignore warnings about unassigned fields.
// Their values are injected, but this is not visible to the compiler.
#pragma warning disable CS0649

public class ScriptThatNeedsInjection : MonoBehaviour
{
    // Inject field
    [Inject]
    private SettingsManager settingsManager;

    // Inject property
    [Inject]
    private Settings Settings { get; set; }

    // Inject field using a specific key instead of the type.
    [Inject(key = "author")]
    private string nameOfAuthor { get; set; }

    // Inject field via GetComponentInChildren
    [InjectComponent(GetComponentMethods.GetComponentInChildren)]
    private ChildOfScriptThatNeedsInjection child;

    // Inject property via GetComponentInParent
    [InjectComponent(GetComponentMethods.GetComponentInParent)]
    private ParentOfScriptThatNeedsInjection Parent { get; set; }

    // Inject readonly field via GetComponentInParent
    [InjectComponent(GetComponentMethods.GetComponentInParent)]
    private readonly OtherComponentOfScriptThatNeedsInjection siblingComponent;

    // Inject readonly property via FindObjectOfType
    [InjectComponent(GetComponentMethods.FindObjectOfType)]
    private readonly Canvas canvas;

    void Start()
    {
        Debug.Log("SettingsManager: " + settingsManager);
        Debug.Log("Settings: " + Settings);

        Debug.Log("Parent: " + Parent);
        Debug.Log("Child: " + child);
        Debug.Log("Sibling Component: " + siblingComponent);

        Debug.Log("Canvas: " + canvas);

        Debug.Log("Author: " + nameOfAuthor);
    }
}
