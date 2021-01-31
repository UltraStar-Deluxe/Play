using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UniInject;

public class InputManager : MonoBehaviour, IBinder
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        if (Application.isEditor && Application.isPlaying)
        {
            DeleteInputActionAssetFile(GetInputActionAssetFilePath());
        }
        inputActionAsset = null;
        inputActions = null;
    }
    
    public static InputManager Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<InputManager>("InputManager");
        }
    }
    
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

    // Static reference to be persisted across scenes.
    private static InputActions inputActions;
    /**
     * Holds the loaded InputActions for easy access. This object is also available via Injection.
     */
    public InputActions InputActions
    {
        get
        {
            if (inputActions == null)
            {
                inputActions = new InputActions(InputActionAsset);
            }

            return inputActions;
        }
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
    
    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new BindingBuilder();
        bb.BindExistingInstanceLazy(() => InputActions);
        return bb.GetBindings();
    }
}
