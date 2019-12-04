using System;

namespace UniInject
{
    // Marker attribute to indicate that a field or property is set in the Unity Inspector.
    // Using this attribute, the field or property can be checked to be not null.
    [AttributeUsage(AttributeTargets.Field
                  | AttributeTargets.Property)]
    public class InjectedInInspectorAttribute : Attribute
    {

    }
}