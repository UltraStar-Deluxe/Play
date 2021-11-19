using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.SceneManagement;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class UpdateTranslatorsOnSceneInjectionFinished : MonoBehaviour, INeedInjection
{

}
