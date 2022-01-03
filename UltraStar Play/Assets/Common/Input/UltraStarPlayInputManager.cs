using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using PrimeInputActions;

public class UltraStarPlayInputManager : InputManager
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        AdditionalInputActionInfos.Clear();
    }

    public static List<InputActionInfo> AdditionalInputActionInfos { get; private set; }= new List<InputActionInfo>();
    
    private void Start()
    {
        try
        {
            // Enable EnhancedTouchSupport to make use of EnhancedTouch.Touch struct etc.
            EnhancedTouchSupport.Enable();
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, "Could not enable enhanced touch support");
        }
        ContextMenu.OpenContextMenus.Clear();
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (InputManager.instance == this)
        {
            AdditionalInputActionInfos.Clear();
        }
    }
}
