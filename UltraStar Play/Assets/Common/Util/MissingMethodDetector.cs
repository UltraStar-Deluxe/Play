using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Searches for missing references and missing methods of events in the game scene
/// (e.g. onClick from a Button script).
/// Typically these are missing because classes or methods have been renamed.
/// </summary>
abstract public class MissingMethodDetector<T> : MonoBehaviour where T : UnityEngine.Object
{
    protected abstract void SearchForMissingFunctions(T o);

    protected void Awake()
    {
        if (Application.isEditor)
        {
            SearchForMissingFunctions();
        }
    }

    protected void SearchForMissingFunctions()
    {
        T[] objects = FindObjects();
        foreach (T o in objects)
        {
            SearchForMissingFunctions(o);
        }
    }

    protected T[] FindObjects()
    {
        return GameObject.FindObjectsOfType<T>();
    }

    /// Checks the existance of the target and the method in the target
    /// and logs a message linked to the provided gameObject if not.
    protected void CheckExistance(UnityEngine.Object target, string methodName, GameObject source)
    {
        //Check that the class that holds the function exists
        bool objectExists = CheckObjectExists(target, source);
        if (objectExists)
        {
            CheckMethodExists(target, methodName, source);
        }
    }

    /// Checks if class exists or has been renamed
    protected bool ClassExist(string className)
    {
        Type myType = Type.GetType(className);
        return myType != null;
    }

    /// Checks if functions exist as public function
    protected bool FunctionExistAsPublicInTarget(UnityEngine.Object target, string functionName)
    {
        Type type = target.GetType();
        MethodInfo[] infos = type.GetMethods();
        foreach (MethodInfo info in infos)
        {
            if (info.Name == functionName)
            {
                return true;
            }
        }
        return false;
    }

    /// Checks if functions exist as private function
    protected bool FunctionExistAsPrivateInTarget(UnityEngine.Object target, string functionName)
    {
        Type type = target.GetType();
        MethodInfo targetinfo = type.GetMethod(functionName, BindingFlags.Instance | BindingFlags.NonPublic);
        return targetinfo != null;
    }

    private bool CheckObjectExists(UnityEngine.Object target, GameObject source)
    {
        if (target == null)
        {
            Debug.Log("<color=blue>Object '" + source.name + "' "
                + "is missing a script for a callback function</color>", source);
            return false;
        }
        return true;
    }

    private bool CheckMethodExists(UnityEngine.Object target, string methodName, GameObject source)
    {
        // Check that the method exists.
        string objectFullNameWithNamespace = target.GetType().FullName;
        if (!FunctionExistAsPublicInTarget(target, methodName))
        {
            // Check if function Exist as private.
            if (FunctionExistAsPrivateInTarget(target, methodName))
            {
                Debug.Log("<color=yellow>Private function '" + methodName + "' cannot be used as callback. "
                    + "Please change its visibility in the '" + objectFullNameWithNamespace + "' script to public</color>", source);
            }
            else
            {
                // Function does not even exist at-all.
                Debug.Log("<color=red>Function '" + methodName + "' does not exist in the '" + objectFullNameWithNamespace + "' script</color>", source);
            }
            return false;
        }
        return true;
    }
}
