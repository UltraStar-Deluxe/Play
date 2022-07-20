using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniInject.Extensions;
using UniRx;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CreditsCharacterControl : INeedInjection, IDisposable, IInjectionFinishedListener
{
    private static readonly Vector2 gravityInPxPerSecond = new Vector2(0, 300f);

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    public Label Label { get; private set; }
    public VisualElement VisualElement => Label;

    [Inject(UxmlName = R.UxmlNames.allCharactersContainer)]
    private VisualElement allCharactersContainer;

    [Inject]
    private GameObject gameObject;

    private float lifeTime;

    private Vector2 velocityInPxPerSecond;
    private float angleVelocityInDegrees;

    public bool IsMoving { get; private set; }
    public bool IsCollected { get; set; }

    private float startMovementTimeInSeconds;

    public string Character
    {
        get
        {
            return Label.text;
        }
        set
        {
            Label.text = value;
        }
    }

    public void OnInjectionFinished()
    {
    }

    public void InitMovement()
    {
        startMovementTimeInSeconds = Random.Range(1.5f, 3f);
    }

    public void Update()
    {
        if (lifeTime > startMovementTimeInSeconds)
        {
            if (!IsMoving)
            {
                IsMoving = true;
                velocityInPxPerSecond = new Vector2(
                    Random.Range(-150, 150),
                    Random.Range(-360, -200));
                // max angle velocity: 2 turns per second
                angleVelocityInDegrees = Random.Range(0, 720 / Application.targetFrameRate);
            }

            velocityInPxPerSecond += gravityInPxPerSecond * Time.deltaTime;

            Label.style.left = Label.style.left.value.value + velocityInPxPerSecond.x * Time.deltaTime;
            Label.style.top = Label.style.top.value.value + velocityInPxPerSecond.y * Time.deltaTime;
            Label.style.rotate = new StyleRotate(new Rotate(new Angle(
                Label.style.rotate.value.angle.value + angleVelocityInDegrees, AngleUnit.Degree)));
        }

        if (startMovementTimeInSeconds > 0)
        {
            lifeTime += Time.deltaTime;
        }
    }

    public void Dispose()
    {
        Label.RemoveFromHierarchy();
    }
}
