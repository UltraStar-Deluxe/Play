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

    protected virtual void OnDestroySingleton()
    {
    }

    private void Awake()
    {
        if (ReferenceEquals(GetInstance(), this))
        {
            AwakeSingleton();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        if (ReferenceEquals(GetInstance(), this))
        {
            OnEnableSingleton();
        }
    }

    private void OnDisable()
    {
        if (ReferenceEquals(GetInstance(), this))
        {
            OnDisableSingleton();
        }
    }

    private void Start()
    {
        if (ReferenceEquals(GetInstance(), this))
        {
            StartSingleton();
        }
    }

    private void OnDestroy()
    {
        if (ReferenceEquals(GetInstance(), this))
        {
            OnDestroySingleton();
        }
    }
}
