using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CartoonFX
{
    [RequireComponent(typeof(ParticleSystem))]
    public class CFXR_EmissionBySurface : MonoBehaviour
    {
        public bool active = true;
        public float particlesPerUnit = 10;
        [Tooltip("This is to avoid slowdowns in the Editor if the value gets too high")] public float maxEmissionRate = 5000;
        [HideInInspector] public float density = 0;

        bool attachedToEditor;
        ParticleSystem ps;

#if UNITY_EDITOR
        void OnValidate()
        {
            this.hideFlags = HideFlags.DontSaveInBuild;
            CalculateAndUpdateEmission();
        }

        internal void AttachToEditor()
        {
            if (attachedToEditor) return;

            EditorApplication.update += OnEditorUpdate;
            attachedToEditor = true;
        }

        internal void DetachFromEditor()
        {
            if (!attachedToEditor) return;

            EditorApplication.update -= OnEditorUpdate;
            attachedToEditor = false;
        }

        void OnEditorUpdate()
        {
            CalculateAndUpdateEmission();

            if (!System.Array.Exists(Selection.gameObjects, item => item == this.gameObject))
            {
                DetachFromEditor();
            }
        }

        void CalculateAndUpdateEmission()
        {
            if (!active) return;
            if (this == null) return;
            if (ps == null) ps = this.GetComponent<ParticleSystem>();
            density = CalculateShapeDensity(ps.shape, ps.main.scalingMode == ParticleSystemScalingMode.Shape, this.transform);
            if (density == 0) return;
            float emissionOverTime = density * particlesPerUnit;
            ParticleSystem.EmissionModule emission = ps.emission;
            if (Math.Abs(emission.rateOverTime.constant - emissionOverTime) > 0.1f)
            {
                emission.rateOverTime = Mathf.Min(maxEmissionRate, emissionOverTime);
            }
        }

        float CalculateShapeDensity(ParticleSystem.ShapeModule shapeModule, bool isShapeScaling, Transform transform)
        {
            float arcPercentage = Mathf.Max(0.01f, shapeModule.arc / 360f);
            float thicknessPercentage = Mathf.Max(0.01f, 1.0f - shapeModule.radiusThickness);

            float scaleX = shapeModule.scale.x;
            float scaleY = shapeModule.scale.y;
            float scaleZ = shapeModule.scale.z;
            if (isShapeScaling)
            {
                Vector3 localScale = Quaternion.Euler(shapeModule.rotation) * transform.localScale;
                scaleX = scaleX * localScale.x;
                scaleY = scaleY * localScale.y;
                scaleZ = scaleZ * localScale.z;
            }
            scaleX = Mathf.Abs(scaleX);
            scaleY = Mathf.Abs(scaleY);
            scaleZ = Mathf.Abs(scaleZ);

            switch (shapeModule.shapeType)
            {
                case ParticleSystemShapeType.Hemisphere:
                case ParticleSystemShapeType.Sphere:
                {
                    float rX = shapeModule.radius * scaleX;
                    float rY = shapeModule.radius * scaleY;
                    float rZ = shapeModule.radius * scaleZ;
                    float rmX = rX * thicknessPercentage;
                    float rmY = rY * thicknessPercentage;
                    float rmZ = rZ * thicknessPercentage;
                    float volume = (rX * rY * rZ - rmX * rmY * rmZ) * Mathf.PI;
                    if (shapeModule.shapeType == ParticleSystemShapeType.Hemisphere)
                    {
                        volume /= 2.0f;
                    }
                    return volume * arcPercentage;
                }
                case ParticleSystemShapeType.Cone:
                {
                    float innerDisk = shapeModule.radius * scaleX * thicknessPercentage * shapeModule.radius * scaleY * thicknessPercentage * Mathf.PI;
                    float outerDisk = shapeModule.radius *scaleX * shapeModule.radius * scaleY * Mathf.PI;
                    return outerDisk - innerDisk;
                }
                case ParticleSystemShapeType.ConeVolume:
                {
                    // cylinder volume, changing the angle doesn't actually extend the area from where the particles are emitted
                    float innerCylinder = shapeModule.radius * scaleX * thicknessPercentage * shapeModule.radius * scaleY * thicknessPercentage * Mathf.PI * shapeModule.length * scaleZ;
                    float outerCylinder = shapeModule.radius * scaleX * shapeModule.radius * scaleY * Mathf.PI * shapeModule.length * scaleZ;
                    return outerCylinder - innerCylinder;
                }
                case ParticleSystemShapeType.BoxEdge:
                case ParticleSystemShapeType.BoxShell:
                case ParticleSystemShapeType.Box:
                {
                    return scaleX * scaleY * scaleZ;
                }
                case ParticleSystemShapeType.Circle:
                {
                    float radiusX = shapeModule.radius * scaleX;
                    float radiusY = shapeModule.radius * scaleY;

                    float radiusMinX = radiusX * thicknessPercentage;
                    float radiusMinY = radiusY * thicknessPercentage;
                    float area = (radiusX * radiusY - radiusMinX * radiusMinY) * Mathf.PI;
                    return area * arcPercentage;
                }
                case ParticleSystemShapeType.SingleSidedEdge:
                {
                    return shapeModule.radius * scaleX;
                }
                case ParticleSystemShapeType.Donut:
                {
                    float outerDonutVolume = 2 * Mathf.PI * Mathf.PI * shapeModule.donutRadius * shapeModule.donutRadius * shapeModule.radius * arcPercentage;
                    float innerDonutVolume = 2 * Mathf.PI * Mathf.PI * shapeModule.donutRadius * thicknessPercentage * thicknessPercentage * shapeModule.donutRadius * shapeModule.radius * arcPercentage;
                    return (outerDonutVolume - innerDonutVolume) * scaleX * scaleY * scaleZ;
                }
                case ParticleSystemShapeType.Rectangle:
                {
                    return scaleX * scaleY;
                }
                case ParticleSystemShapeType.Mesh:
                case ParticleSystemShapeType.SkinnedMeshRenderer:
                case ParticleSystemShapeType.MeshRenderer:
                {
                    Debug.LogWarning( string.Format("[{0}] Calculating volume for a mesh is unsupported.", nameof(CFXR_EmissionBySurface)));
                    this.active = false;
                    return 0;
                }
                case ParticleSystemShapeType.Sprite:
                case ParticleSystemShapeType.SpriteRenderer:
                {
                    Debug.LogWarning( string.Format("[{0}] Calculating volume for a sprite is unsupported.", nameof(CFXR_EmissionBySurface)));
                    this.active = false;
                    return 0;
                }
            }

            return 0;
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(CFXR_EmissionBySurface))]
    class CFXR_EmissionBySurface_Editor : Editor
    {
        CFXR_EmissionBySurface Target { get { return target as CFXR_EmissionBySurface; } }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GUILayout.Space(10);
            EditorGUILayout.HelpBox("This Editor script will adapt the particle emission based on its shape density, so that you can resize it to fit a specific situation and the overall number of particles won't change.\n\nYou can scale the object to change the emission area, and you can open the 'Shape' module in the Particle System to visualize the emission area.", MessageType.Info);
            EditorGUILayout.HelpBox("Calculated Density: " + Target.density, MessageType.None);
        }

        void OnEnable()
        {
            Target.AttachToEditor();
        }

        void OnDisable()
        {
            Target.DetachFromEditor();
        }
    }
#endif
}