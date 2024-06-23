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
    public float scale;
    public float simulationSpeed;
    public float scaleBoxShapeWithTargetFactor;
    public int maxParticles;
    public int rateOverTime;

    public Func<Vector2> moveWithTargetPanelPosProducer; 
    public bool hideAndShowWithTarget;
    public bool destroyWithTarget;
}
