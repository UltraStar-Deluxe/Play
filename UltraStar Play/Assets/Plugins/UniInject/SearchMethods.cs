namespace UniInject
{
    // Determines how the value to be injected is found.
    // This can be a search in an injector's bindings,
    // or via a Unity method that searches in the scene hierarchy (e.g. GetComponentInChildren).
    public enum SearchMethods
    {
        // Search in an injector's bindings.
        SearchInBindings,
        // Unity method to search in the scene hierarchy.
        GetComponent,
        GetComponentInChildren,
        GetComponentInChildrenIncludeInactive,
        GetComponentInParent,
        FindObjectOfType
    }
}
