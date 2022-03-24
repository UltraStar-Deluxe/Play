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

public class PathTextFieldControl : BackslashReplacingTextFieldControl
{
    private const string BackslashReplacement = "/";

    public PathTextFieldControl(TextField textField)
        : base(textField, BackslashReplacement)
    {
    }
}
