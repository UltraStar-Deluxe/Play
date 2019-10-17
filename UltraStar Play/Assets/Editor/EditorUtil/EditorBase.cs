using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.IO;

/// Base class for custom inspectors.
public class EditorBase : Editor
{

    /// Draws the default inspector without certain properties.
    /// Can be used to draw certain properties in a custom way and still keep the rest of the default inspector.
    public static bool DrawDefaultInspectorWithoutProperties(SerializedObject obj, params string[] ignoreProperties)
    {
        EditorGUI.BeginChangeCheck();
        obj.Update();
        SerializedProperty iterator = obj.GetIterator();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            if (ignoreProperties.Contains(iterator.propertyPath))
            {
                continue;
            }

            if (iterator.propertyPath == "m_Script")
            {
                GUI.enabled = false;
            }

            EditorGUILayout.PropertyField(iterator, true, new GUILayoutOption[0]);
            enterChildren = false;

            if (iterator.propertyPath == "m_Script")
            {
                GUI.enabled = true;
            }
        }
        obj.ApplyModifiedProperties();
        return EditorGUI.EndChangeCheck();
    }

    /// Draws a single property of a SerializedObject using the default implementation from Unity.
    public static bool DrawProperty(SerializedObject obj, string propertyPath)
    {
        EditorGUI.BeginChangeCheck();
        obj.Update();

        SerializedProperty property = obj.FindProperty(propertyPath);
        EditorGUILayout.PropertyField(property, true, new GUILayoutOption[0]);

        obj.ApplyModifiedProperties();
        return EditorGUI.EndChangeCheck();
    }

    /// Clear the GUI.changed flag and update serializedObject.
    protected void PrepareChanges()
    {
        GUI.changed = false;
        serializedObject.Update();
    }

    /// Set targets dirty if GUI changed
    /// and apply modification to serialized objects.
    protected void ApplyChanges()
    {
        if (GUI.changed)
        {
            foreach (Object o in targets)
            {
                EditorUtility.SetDirty(o);
            }
        }
        serializedObject.ApplyModifiedProperties();
    }
}
