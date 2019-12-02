using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniInject
{
    public class UniInjectMenuItems
    {

        [MenuItem("UniInject/Check current scene &v")]
        public static void CheckScene()
        {
            bool hasErrors = false;
            GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (GameObject rootObject in rootObjects)
            {
                MonoBehaviour[] scripts = rootObject.GetComponentsInChildren<MonoBehaviour>();
                foreach (MonoBehaviour script in scripts)
                {
                    hasErrors |= CheckScript(script);
                }
            }

            if (!hasErrors)
            {
                Debug.Log("UniInject - No issues found in current scene. Yay!");
            }
        }

        // Returns true if a problem has been found.
        private static bool CheckScript(MonoBehaviour script)
        {
            bool hasErrors = false;

            Type type = script.GetType();
            MemberInfo[] memberInfos = type.GetMembers(BindingFlags.Public
                                                        | BindingFlags.NonPublic
                                                        | BindingFlags.Instance);
            foreach (MemberInfo memberInfo in memberInfos)
            {
                hasErrors |= CheckInjectedInInspectorAttribute(script, type, memberInfo);
            }

            return hasErrors;
        }

        // Returns true if a problem has been found.
        private static bool CheckInjectedInInspectorAttribute(MonoBehaviour script, Type type, MemberInfo memberInfo)
        {
            InjectedInInspectorAttribute attribute = memberInfo.GetCustomAttribute<InjectedInInspectorAttribute>();
            if (attribute != null)
            {
                // Check that the value has been set.
                object value = null;
                if (memberInfo is FieldInfo)
                {
                    value = (memberInfo as FieldInfo).GetValue(script);
                }
                else if (memberInfo is PropertyInfo)
                {
                    value = (memberInfo as PropertyInfo).GetValue(script);
                }
                else
                {
                    // This should never happen
                    // because the attribute can only be used on fields and properties.
                    return false;
                }

                if (value == null || value.ToString() == "null")
                {
                    Debug.LogError($"Missing value in {script.name}: {type.Name}.{memberInfo.Name} is null", script);
                    return true;
                }
            }
            return false;
        }
    }
}