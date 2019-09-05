using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class MissingOnClickDetector : MissingMethodDetector<Button>
{
    protected override void SearchForMissingFunctions(Button button)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            // Get the target object and method name.
            UnityEngine.Object referencedObject = button.onClick.GetPersistentTarget(i);
            string methodName = button.onClick.GetPersistentMethodName(i);
            // Check existance of both.
            CheckExistance(referencedObject, methodName, button.gameObject);
        }
    }
}