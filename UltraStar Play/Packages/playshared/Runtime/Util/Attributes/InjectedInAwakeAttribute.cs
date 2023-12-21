using System;

/**
 * Marker attribute to indicate that a field or property is set in the Awake callback of Unity.
 */
[AttributeUsage(AttributeTargets.Field
                 | AttributeTargets.Property)]
public class InjectedInAwakeAttribute : Attribute
{
}
