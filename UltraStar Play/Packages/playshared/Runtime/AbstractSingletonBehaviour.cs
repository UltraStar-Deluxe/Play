using System;
using System.Collections.Generic;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public abstract class AbstractSingletonBehaviour : MonoBehaviour
{
    protected abstract object GetInstance();

    protected virtual void AwakeSingleton()
    {
    }

    protected virtual void StartSingleton()
    {
    }

    protected virtual void OnEnableSingleton()
    {
    }

    protected virtual void OnDisableSingleton()
    {
    }

    private void Awake()
    {
        if (GetInstance() != this)
        {
            Destroy(gameObject);
            return;
        }

        AwakeSingleton();
    }

    private void OnEnable()
    {
        if (GetInstance() != this)
        {
            return;
        }

        OnEnableSingleton();
    }

    private void OnDisable()
    {
        if (GetInstance() != this)
        {
            return;
        }

        OnDisableSingleton();
    }

    private void Start()
    {
        if (GetInstance() != this)
        {
            return;
        }

        StartSingleton();
    }
}
