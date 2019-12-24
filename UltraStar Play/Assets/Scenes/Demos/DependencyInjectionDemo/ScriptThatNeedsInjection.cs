using UniInject;
using UnityEngine;
using UnityEngine.UI;
using static UniInject.UniInjectUtils;

// Ignore warnings about unassigned fields.
// Their values are injected, but this is not visible to the compiler.
#pragma warning disable CS0649

public class ScriptThatNeedsInjection : MonoBehaviour, INeedInjection
{
    // The marker attribute can be used to check that the field has been set.
    // Use the corresponding menu item (under UniInject) to perform a check on the current scene.
    [InjectedInInspector]
    public Transform referencedTransform;

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

    // Inject property
    [Inject]
    private SettingsManager SettingsManager { get; set; }

    // Inject field. Binding the settings is done lazy, so they will not be loaded if not injected here.
    [Inject]
    private Settings settings;

    // Inject optional
    [Inject(optional = true)]
    private SceneNavigator sceneNavigator;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren, optional = true)]
    private Text uiText;

    // Inject property using a specific key instead of the type.
    [Inject(key = "author")]
    private string NameOfAuthor { get; set; }

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

    // Inject the injector that was used to inject all the fields.
    // The injector can be used at runtime to inject newly created scripts.
    [Inject]
    private Injector injector;

    void Start()
    {
        Debug.Log("Parent: " + Parent);
        Debug.Log("Child: " + child);
        Debug.Log("Sibling Component: " + siblingComponent);

        Debug.Log("Canvas: " + canvas);

        Debug.Log("SettingsManager: " + SettingsManager);
        Debug.Log("Settings: " + settings);

        Debug.Log("Author: " + NameOfAuthor);

        Debug.Log("Field from method injection:" + methodInjectionField);

        Debug.Log("Instance of an interface (field 1):" + demoInterfaceInstance1.GetGreeting());
        Debug.Log("Instance of an interface (field 2):" + demoInterfaceInstance2.GetGreeting());

        Debug.Log("Instance of an interface with constructor parameters:" + demoInterfaceInstanceWithConstructorParameters.GetByeBye());

        Debug.Log("Optional sceneNavigator: " + sceneNavigator);
        Debug.Log("Optional uiText: " + uiText);

        Debug.Log("The bound int: " + SceneInjector.GetValueForInjectionKey<int>());
        Debug.Log("The bound instance of an interface: " + SceneInjector.GetValueForInjectionKey<IDependencyInjectionDemoInterface>());

        Debug.Log("Injector:" + injector);
    }
}
