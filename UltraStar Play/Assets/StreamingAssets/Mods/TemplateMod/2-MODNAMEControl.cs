using UnityEngine;
using UniInject;
using UnityEngine.UIElements;
using UniRx;
using System;
using System.Collections.Generic;

// Open the mod folder with Visual Studio Code and installed C# Dev Kit for IDE features such as
// code completion, error markers, parameter hints, go to definition, etc.
// ---
// Mods must implement subtypes of special mod interfaces.
// Available interfaces can be found by executing 'mod.interfaces' in the game's console.
public class MODNAMEControl : IOnLoadMod, IOnDisableMod
{
    public void OnLoadMod()
    {
        Debug.Log("MODNAME - OnLoadMod");
    }

    public void OnDisableMod()
    {
        Debug.Log("MODNAME - OnDisableMod");
    }
}
