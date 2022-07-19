using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CreditsEntry : JsonSerializable
{
    public string Name { get; set; }
    public string Nickname { get; set; }
    public string Comment { get; set; }
    public string Banner { get; set; }
}
