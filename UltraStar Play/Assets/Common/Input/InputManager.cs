using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        if (Application.isEditor && Application.isPlaying)
        {
            DeleteInputActionAssetFile(GetInputActionAssetFilePath());
        }
        inputActionAsset = null;
    }

    private static InputManager instance;
    public static InputManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = GameObjectUtils.FindComponentWithTag<InputManager>("InputManager");
            }

            return instance;
        }
    }

    private static Dictionary<string, ObservableCancelablePriorityInputAction> pathToInputAction = new Dictionary<string, ObservableCancelablePriorityInputAction>();
    
    /**
     * Default InputActionAsset is copied to streamingAssetsPath such that users can edit it to their preferences.
     * Use loaded InputActions-object instead. The loaded InputActions-object is also available via Injection.
     */
    public InputActionAsset defaultInputActionAsset;

    // Static reference to be persisted across scenes.
    private static InputActionAsset inputActionAsset;
    /**
     * Loaded InputActionAsset. This will be loaded from streamingAssets if possible.
     * If loading it from file failed, then defaultInputActionMap is used instead.
     */
    private InputActionAsset InputActionAsset
    {
        get
        {
            if (inputActionAsset == null)
            {
                string absoluteFilePath = GetInputActionAssetFilePath();
                if (!File.Exists(absoluteFilePath))
                {
                    SaveInputActionAssetToFile(defaultInputActionAsset, absoluteFilePath);
                }

                inputActionAsset = LoadInputActionAssetFromFile(absoluteFilePath);
                inputActionAsset.Enable();
            }

            return inputActionAsset;
        }
    }

    private void Start()
    {
        ContextMenu.OpenContextMenus.Clear();
    }

    private void SaveInputActionAssetToFile(InputActionAsset theInputActionAsset, string absoluteFilePath)
    {
        Debug.Log($"Saving InputActionAsset to '{absoluteFilePath}'");
        
        try
        {
            File.WriteAllText(absoluteFilePath, theInputActionAsset.ToJson(), Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, $"Error saving default InputActionAsset to '{absoluteFilePath}'");
        }
    }

    private InputActionAsset LoadInputActionAssetFromFile(string absoluteFilePath)
    {
        Debug.Log($"Loading InputActionAsset from '{absoluteFilePath}'");
        
        try
        {
            string inputActionMapJson = File.ReadAllText(absoluteFilePath, Encoding.UTF8);
            return InputActionAsset.FromJson(inputActionMapJson);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, $"Error loading InputActionAsset from '{absoluteFilePath}'. Using default InputActionAsset instead.");
            return defaultInputActionAsset;
        }
    }

    private static void DeleteInputActionAssetFile(string absoluteFilePath)
    {
        try
        {
            File.Delete(absoluteFilePath);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, $"Error deleting InputActionAsset '{absoluteFilePath}'");
        }        
    }

    private static string GetInputActionAssetFilePath()
    {
        return ApplicationUtils.GetStreamingAssetsPath("Input/UltraStarPlayInputActions.inputactions");
    }

    public static ObservableCancelablePriorityInputAction GetInputAction(string path)
    {
        if (pathToInputAction.TryGetValue(path, out ObservableCancelablePriorityInputAction observableInputAction))
        {
            return observableInputAction;
        }

        InputAction inputAction = Instance.InputActionAsset.FindAction(path, true);
        observableInputAction = new ObservableCancelablePriorityInputAction(inputAction, Instance.gameObject);
        return observableInputAction;
    }
}
