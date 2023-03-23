using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ParticleEffectConfig
{
    public EParticleEffect particleEffect;
    public Vector2 panelPos;
    public bool loop;
    public bool isBackground;
    
    public VisualElement target;
    public Vector2 referenceTargetSize;
    public Vector3 referenceParticleSystemScale;
    public float scale;

    public Func<Vector2> moveWithTargetPanelPosProducer; 
    public bool hideAndShowWithTarget;
    public bool destroyWithTarget;
}
