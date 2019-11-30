using UniInject;
using UnityEngine;
using static UniInject.UniInject;

// Ignore warnings about unassigned fields.
// Their values are injected, but this is not visible to the compiler.
#pragma warning disable CS0649

public class ScriptThatNeedsInjection : MonoBehaviour
{
    // Inject field via GetComponentInChildren
    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private ChildOfScriptThatNeedsInjection child;

    // Inject property via GetComponentInParent
    [Inject(searchMethod = SearchMethods.GetComponentInParent)]
    private ParentOfScriptThatNeedsInjection Parent { get; set; }

    // Inject readonly field via GetComponentInParent
    [Inject(searchMethod = SearchMethods.GetComponentInParent)]
    private readonly OtherComponentOfScriptThatNeedsInjection siblingComponent;

    // Inject readonly property via FindObjectOfType
    [Inject(searchMethod = SearchMethods.FindObjectOfType)]
    private readonly Canvas canvas;

    // Inject field
    [Inject]
    private SettingsManager settingsManager;

    // Inject property
    [Inject]
    private Settings Settings { get; set; }

    // Inject field using a specific key instead of the type.
    [Inject(key = "author")]
    private string nameOfAuthor { get; set; }

    // The instance of this field is created during injection.
    // Depending how the interface is bound (singleton or not),
    // the instances of demoInterfaceInstance1 and demoInterfaceInstance2 will be the same or different objects.
    [Inject]
    private readonly IDependencyInjectionDemoInterface demoInterfaceInstance1;

    [Inject]
    private IDependencyInjectionDemoInterface demoInterfaceInstance2;

    [Inject]
    private IDependencyInjectionDemoInterfaceWithConstructorParameters demoInterfaceInstanceWithConstructorParameters;

    // This field is set in a method via method injection
    private string methodInjectionField;

    [Inject]
    private void SetMethodInjectionField([InjectionKey("personWithAge")] string personWithAge, int age)
    {
        this.methodInjectionField = $"{personWithAge} is {age} years old";
    }

    void Start()
    {
        Debug.Log("SettingsManager: " + settingsManager);
        Debug.Log("Settings: " + Settings);

        Debug.Log("Parent: " + Parent);
        Debug.Log("Child: " + child);
        Debug.Log("Sibling Component: " + siblingComponent);

        Debug.Log("Canvas: " + canvas);

        Debug.Log("Author: " + nameOfAuthor);

        Debug.Log("Field from method injection:" + methodInjectionField);

        Debug.Log("Instance of an interface (field 1):" + demoInterfaceInstance1.GetGreeting());
        Debug.Log("Instance of an interface (field 2):" + demoInterfaceInstance2.GetGreeting());

        Debug.Log("Instance of an interface with constructor parameters:" + demoInterfaceInstanceWithConstructorParameters.GetByeBye());

        Debug.Log("An int: " + GlobalInjector.GetInstance<int>());
        Debug.Log("An instance of an interface: " + GlobalInjector.GetInstance<IDependencyInjectionDemoInterface>());
    }
}
