//--------------------------------------------------------------------------------------------------------------------------------
// Cartoon FX
// (c) 2012-2020 Jean Moreno
//--------------------------------------------------------------------------------------------------------------------------------


//--------------------------------------------------------------------------------------------------------------------------------

// Use the defines below to globally disable features:

//#define DISABLE_CAMERA_SHAKE
//#define DISABLE_LIGHTS
//#define DISABLE_CLEAR_BEHAVIOR

//--------------------------------------------------------------------------------------------------------------------------------

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CartoonFX
{
	[RequireComponent(typeof(ParticleSystem))]
	[DisallowMultipleComponent]
	public partial class CFXR_Effect : MonoBehaviour
	{
		// Change this value to easily tune the camera shake strength for all effects
		const float GLOBAL_CAMERA_SHAKE_MULTIPLIER = 1.0f;

#if UNITY_EDITOR
		[InitializeOnLoadMethod]
		static void InitGlobalOptions()
		{
			AnimatedLight.editorPreview = EditorPrefs.GetBool("CFXR Light EditorPreview", true);
	#if !DISABLE_CAMERA_SHAKE
			CameraShake.editorPreview = EditorPrefs.GetBool("CFXR CameraShake EditorPreview", true);
	#endif
		}
#endif

		public enum ClearBehavior
		{
			None,
			Disable,
			Destroy
		}

		[System.Serializable]
		public class AnimatedLight
		{
			static public bool editorPreview = true;

			public Light light;

			public bool loop;

			public bool animateIntensity;
			public float intensityStart = 8f;
			public float intensityEnd = 0f;
			public float intensityDuration = 0.5f;
			public AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
			public bool perlinIntensity;
			public float perlinIntensitySpeed = 1f;
			public bool fadeIn;
			public float fadeInDuration = 0.5f;
			public bool fadeOut;
			public float fadeOutDuration = 0.5f;

			public bool animateRange;
			public float rangeStart = 8f;
			public float rangeEnd = 0f;
			public float rangeDuration = 0.5f;
			public AnimationCurve rangeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
			public bool perlinRange;
			public float perlinRangeSpeed = 1f;

			public bool animateColor;
			public Gradient colorGradient;
			public float colorDuration = 0.5f;
			public AnimationCurve colorCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
			public bool perlinColor;
			public float perlinColorSpeed = 1f;

			public void animate(float time)
			{
#if UNITY_EDITOR
				if (!editorPreview && !EditorApplication.isPlaying)
				{
					return;
				}
#endif

				if (light != null)
				{
					if (animateIntensity)
					{
						float delta = loop ? Mathf.Clamp01((time % intensityDuration)/intensityDuration) : Mathf.Clamp01(time/intensityDuration);
						delta = perlinIntensity ? Mathf.PerlinNoise(Time.time * perlinIntensitySpeed, 0f) : intensityCurve.Evaluate(delta);
						light.intensity = Mathf.LerpUnclamped(intensityEnd, intensityStart, delta);

						if (fadeIn && time < fadeInDuration)
						{
							light.intensity *= Mathf.Clamp01(time / fadeInDuration);
						}
					}

					if (animateRange)
					{
						float delta = loop ? Mathf.Clamp01((time % rangeDuration)/rangeDuration) : Mathf.Clamp01(time/rangeDuration);
						delta = perlinRange ? Mathf.PerlinNoise(Time.time * perlinRangeSpeed, 10f) : rangeCurve.Evaluate(delta);
						light.range = Mathf.LerpUnclamped(rangeEnd, rangeStart, delta);
					}

					if (animateColor)
					{
						float delta = loop ? Mathf.Clamp01((time % colorDuration)/colorDuration) : Mathf.Clamp01(time/colorDuration);
						delta = perlinColor ? Mathf.PerlinNoise(Time.time * perlinColorSpeed, 0f) : colorCurve.Evaluate(delta);
						light.color = colorGradient.Evaluate(delta);
					}
				}
			}

			public void animateFadeOut(float time)
			{
				if (fadeOut && light != null)
				{
					light.intensity *= 1.0f - Mathf.Clamp01(time / fadeOutDuration);
				}
			}

			public void reset()
			{
				if (light != null)
				{
					if (animateIntensity)
					{
						light.intensity = (fadeIn || fadeOut) ? 0 : intensityEnd;
					}

					if (animateRange)
					{
						light.range = rangeEnd;
					}

					if (animateColor)
					{
						light.color = colorGradient.Evaluate(1f);
					}
				}
			}

			#region Animated Light Property Drawer
#if UNITY_EDITOR
			[CustomPropertyDrawer(typeof(AnimatedLight))]
			public class AnimatedLightDrawer : PropertyDrawer
			{
				SerializedProperty light;

				SerializedProperty loop;

				SerializedProperty animateIntensity;
				SerializedProperty intensityStart;
				SerializedProperty intensityEnd;
				SerializedProperty intensityDuration;
				SerializedProperty intensityCurve;
				SerializedProperty perlinIntensity;
				SerializedProperty perlinIntensitySpeed;
				SerializedProperty fadeIn;
				SerializedProperty fadeInDuration;
				SerializedProperty fadeOut;
				SerializedProperty fadeOutDuration;

				SerializedProperty animateRange;
				SerializedProperty rangeStart;
				SerializedProperty rangeEnd;
				SerializedProperty rangeDuration;
				SerializedProperty rangeCurve;
				SerializedProperty perlinRange;
				SerializedProperty perlinRangeSpeed;

				SerializedProperty animateColor;
				SerializedProperty colorGradient;
				SerializedProperty colorDuration;
				SerializedProperty colorCurve;
				SerializedProperty perlinColor;
				SerializedProperty perlinColorSpeed;

				void fetchProperties(SerializedProperty property)
				{
					light                 = property.FindPropertyRelative("light");

					loop                  = property.FindPropertyRelative("loop");

					animateIntensity      = property.FindPropertyRelative("animateIntensity");
					intensityStart        = property.FindPropertyRelative("intensityStart");
					intensityEnd          = property.FindPropertyRelative("intensityEnd");
					intensityDuration     = property.FindPropertyRelative("intensityDuration");
					intensityCurve        = property.FindPropertyRelative("intensityCurve");
					perlinIntensity       = property.FindPropertyRelative("perlinIntensity");
					perlinIntensitySpeed  = property.FindPropertyRelative("perlinIntensitySpeed");
					fadeIn                = property.FindPropertyRelative("fadeIn");
					fadeInDuration        = property.FindPropertyRelative("fadeInDuration");
					fadeOut               = property.FindPropertyRelative("fadeOut");
					fadeOutDuration       = property.FindPropertyRelative("fadeOutDuration");

					animateRange          = property.FindPropertyRelative("animateRange");
					rangeStart            = property.FindPropertyRelative("rangeStart");
					rangeEnd              = property.FindPropertyRelative("rangeEnd");
					rangeDuration         = property.FindPropertyRelative("rangeDuration");
					rangeCurve            = property.FindPropertyRelative("rangeCurve");
					perlinRange           = property.FindPropertyRelative("perlinRange");
					perlinRangeSpeed      = property.FindPropertyRelative("perlinRangeSpeed");

					animateColor          = property.FindPropertyRelative("animateColor");
					colorGradient         = property.FindPropertyRelative("colorGradient");
					colorDuration         = property.FindPropertyRelative("colorDuration");
					colorCurve            = property.FindPropertyRelative("colorCurve");
					perlinColor           = property.FindPropertyRelative("perlinColor");
					perlinColorSpeed      = property.FindPropertyRelative("perlinColorSpeed");
				}

				static GUIContent[] ModePopupLabels = new GUIContent[] { new GUIContent("Curve"), new GUIContent("Perlin Noise") };
				static GUIContent IntensityModeLabel = new GUIContent("Intensity Mode");
				static GUIContent RangeModeLabel = new GUIContent("Range Mode");
				static GUIContent ColorModeLabel = new GUIContent("Color Mode");

				const float INDENT_WIDTH = 15f;
				const float PADDING = 4f;

				void startIndent(ref Rect rect)
				{
					EditorGUIUtility.labelWidth -= INDENT_WIDTH;
					rect.xMin += INDENT_WIDTH;
				}

				void endIndent(ref Rect rect)
				{
					EditorGUIUtility.labelWidth += INDENT_WIDTH;
					rect.xMin -= INDENT_WIDTH;
				}

				public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
				{
					fetchProperties(property);

					Rect rect = EditorGUI.IndentedRect(position);

					//Rect lineRect = rect;
					//lineRect.height = 1;
					//lineRect.y -= 2;
					//EditorGUI.DrawRect(lineRect, Color.gray);

					if (Event.current.type == EventType.Repaint)
					{
						EditorStyles.helpBox.Draw(rect, GUIContent.none, 0);
					}

					EditorGUIUtility.labelWidth -= INDENT_WIDTH;

					rect.height = EditorGUIUtility.singleLineHeight;
					rect.xMax -= PADDING;
					rect.y += PADDING;
					float propSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

					EditorGUI.PropertyField(rect, light); rect.y += propSpace;
					EditorGUI.PropertyField(rect, loop); rect.y += propSpace;

					EditorGUI.PropertyField(rect, animateIntensity); rect.y += propSpace;
					if (animateIntensity.boolValue)
					{
						startIndent(ref rect);
						{

							EditorGUI.PropertyField(rect, intensityStart); rect.y += propSpace;
							EditorGUI.PropertyField(rect, intensityEnd); rect.y += propSpace;

							int val = EditorGUI.Popup(rect, IntensityModeLabel, perlinIntensity.boolValue ? 1 : 0, ModePopupLabels); rect.y += propSpace;
							if (val == 1 && !perlinIntensity.boolValue)
							{
								perlinIntensity.boolValue = true;
							}
							else if (val == 0 && perlinIntensity.boolValue)
							{
								perlinIntensity.boolValue = false;
							}

							startIndent(ref rect);
							{
								if (perlinIntensity.boolValue)
								{
									EditorGUI.PropertyField(rect, perlinIntensitySpeed); rect.y += propSpace;
								}
								else
								{
									EditorGUI.PropertyField(rect, intensityDuration); rect.y += propSpace;
									EditorGUI.PropertyField(rect, intensityCurve); rect.y += propSpace;
								}
							}
							endIndent(ref rect);

							EditorGUI.PropertyField(rect, fadeIn); rect.y += propSpace;
							if (fadeIn.boolValue)
							{
								startIndent(ref rect);
								EditorGUI.PropertyField(rect, fadeInDuration); rect.y += propSpace;
								endIndent(ref rect);
							}

							EditorGUI.PropertyField(rect, fadeOut); rect.y += propSpace;
							if (fadeOut.boolValue)
							{
								startIndent(ref rect);
								EditorGUI.PropertyField(rect, fadeOutDuration); rect.y += propSpace;
								endIndent(ref rect);
							}

						}
						endIndent(ref rect);
					}

					EditorGUI.PropertyField(rect, animateRange); rect.y += propSpace;
					if (animateRange.boolValue)
					{
						startIndent(ref rect);
						{
							EditorGUI.PropertyField(rect, rangeStart); rect.y += propSpace;
							EditorGUI.PropertyField(rect, rangeEnd); rect.y += propSpace;

							int val = EditorGUI.Popup(rect, RangeModeLabel, perlinRange.boolValue ? 1 : 0, ModePopupLabels); rect.y += propSpace;
							if (val == 1 && !perlinRange.boolValue)
							{
								perlinRange.boolValue = true;
							}
							else if (val == 0 && perlinRange.boolValue)
							{
								perlinRange.boolValue = false;
							}

							startIndent(ref rect);
							{
								if (perlinRange.boolValue)
								{
									EditorGUI.PropertyField(rect, perlinRangeSpeed); rect.y += propSpace;
								}
								else
								{
									EditorGUI.PropertyField(rect, rangeDuration); rect.y += propSpace;
									EditorGUI.PropertyField(rect, rangeCurve); rect.y += propSpace;
								}
							}
							endIndent(ref rect);
						}
						endIndent(ref rect);
					}

					EditorGUI.PropertyField(rect, animateColor); rect.y += propSpace;
					if (animateColor.boolValue)
					{
						startIndent(ref rect);
						{

							EditorGUI.PropertyField(rect, colorGradient); rect.y += propSpace;

							int val = EditorGUI.Popup(rect, ColorModeLabel, perlinColor.boolValue ? 1 : 0, ModePopupLabels); rect.y += propSpace;
							if (val == 1 && !perlinColor.boolValue)
							{
								perlinColor.boolValue = true;
							}
							else if (val == 0 && perlinColor.boolValue)
							{
								perlinColor.boolValue = false;
							}

							startIndent(ref rect);
							{

								if (perlinColor.boolValue)
								{
									EditorGUI.PropertyField(rect, perlinColorSpeed); rect.y += propSpace;
								}
								else
								{
									EditorGUI.PropertyField(rect, colorDuration); rect.y += propSpace;
									EditorGUI.PropertyField(rect, colorCurve); rect.y += propSpace;
								}
							}
							endIndent(ref rect);
						}
						endIndent(ref rect);
					}

					EditorGUIUtility.labelWidth += INDENT_WIDTH;

					if (GUI.changed)
					{
						property.serializedObject.ApplyModifiedProperties();
					}
				}

				public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
				{
					fetchProperties(property);

					float propSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
					int count = 5;

					if (animateIntensity.boolValue)
					{
						count += 3;
						count += perlinIntensity.boolValue ? 1 : 2;
						count += 1;
						count += fadeIn.boolValue ? 1 : 0;
						count += 1;
						count += fadeOut.boolValue ? 1 : 0;
					}

					if (animateRange.boolValue)
					{
						count += 3;
						count += perlinRange.boolValue ? 1 : 2;
					}

					if (animateColor.boolValue)
					{
						count += 2;
						count += perlinColor.boolValue ? 1 : 2;
					}

					return count * propSpace + PADDING * 2;
				}
			}
#endif
			#endregion

		}

		// ================================================================================================================================

		// Globally disable features
		public static bool GlobalDisableCameraShake;
		public static bool GlobalDisableLights;

		// ================================================================================================================================

		[Tooltip("Defines an action to execute when the Particle System has completely finished playing and emitting particles.")]
		public ClearBehavior clearBehavior = ClearBehavior.Destroy;
		[Space]
		public CameraShake cameraShake;
		[Space]
		public AnimatedLight[] animatedLights;
		[Tooltip("Defines which Particle System to track to trigger light fading out.\nLeave empty if not using fading out.")]
		public ParticleSystem fadeOutReference;

		float time;
		ParticleSystem rootParticleSystem;
		[System.NonSerialized] MaterialPropertyBlock materialPropertyBlock;
		[System.NonSerialized] Renderer particleRenderer;

		// ================================================================================================================================

		public void ResetState()
		{
			time = 0f;
			fadingOutStartTime = 0f;
			isFadingOut = false;

#if !DISABLE_LIGHTS
			if (animatedLights != null)
			{
				foreach (var animLight in animatedLights)
				{
					animLight.reset();
				}
			}
#endif

#if !DISABLE_CAMERA_SHAKE
			if (cameraShake != null && cameraShake.enabled)
			{
				cameraShake.StopShake();
			}
#endif
		}

#if !DISABLE_CAMERA_SHAKE || !DISABLE_CLEAR_BEHAVIOR
		void Awake()
		{
	#if !DISABLE_CAMERA_SHAKE
			if (cameraShake != null && cameraShake.enabled)
			{
				cameraShake.fetchCameras();
			}
	#endif
	#if !DISABLE_CLEAR_BEHAVIOR
			startFrameOffset = GlobalStartFrameOffset++;
#endif
			// Detect if world position needs to be passed to the shader
			particleRenderer = this.GetComponent<ParticleSystemRenderer>();
			if (particleRenderer.sharedMaterial != null && particleRenderer.sharedMaterial.IsKeywordEnabled("_CFXR_LIGHTING_WPOS_OFFSET"))
			{ 
				materialPropertyBlock = new MaterialPropertyBlock();
			}
		}
#endif

			void OnEnable()
		{
			foreach (var animLight in animatedLights)
			{
				if (animLight.light != null)
				{
#if !DISABLE_LIGHTS
					animLight.light.enabled = !GlobalDisableLights;
#else
					animLight.light.enabled = false;
#endif
				}
			}
		}

		void OnDisable()
		{
			ResetState();
		}

#if !DISABLE_LIGHTS || !DISABLE_CAMERA_SHAKE || !DISABLE_CLEAR_BEHAVIOR
		const int CHECK_EVERY_N_FRAME = 20;
		static int GlobalStartFrameOffset = 0;
		int startFrameOffset;
		void Update()
		{
#if !DISABLE_LIGHTS || !DISABLE_CAMERA_SHAKE
			time += Time.deltaTime;

			Animate(time);

			if (fadeOutReference != null && !fadeOutReference.isEmitting && (fadeOutReference.isPlaying || isFadingOut))
			{
				FadeOut(time);
			}
#endif
#if !DISABLE_CLEAR_BEHAVIOR
			if (clearBehavior != ClearBehavior.None)
			{
				if (rootParticleSystem == null)
				{
					rootParticleSystem = this.GetComponent<ParticleSystem>();
				}

				// Check isAlive every N frame, with an offset so that all active effects aren't checked at once
				if ((Time.renderedFrameCount + startFrameOffset) % CHECK_EVERY_N_FRAME == 0)
				{
					if (!rootParticleSystem.IsAlive(true))
					{
						if (clearBehavior == ClearBehavior.Destroy)
						{
							GameObject.Destroy(this.gameObject);
						}
						else
						{
							this.gameObject.SetActive(false);
						}
					}
				}
			}
#endif
			if (materialPropertyBlock != null)
			{
				particleRenderer.GetPropertyBlock(materialPropertyBlock);
				materialPropertyBlock.SetVector("_GameObjectWorldPosition", this.transform.position);
				particleRenderer.SetPropertyBlock(materialPropertyBlock);
			}
		}
#endif

#if !DISABLE_LIGHTS || !DISABLE_CAMERA_SHAKE
		public void Animate(float time)
		{
#if !DISABLE_LIGHTS
			if (animatedLights != null && !GlobalDisableLights)
			{
				foreach (var animLight in animatedLights)
				{
					animLight.animate(time);
				}
			}
#endif

#if !DISABLE_CAMERA_SHAKE
			if (cameraShake != null && cameraShake.enabled && !GlobalDisableCameraShake)
			{
#if UNITY_EDITOR
				if (!cameraShake.isShaking)
				{
					cameraShake.fetchCameras();
				}
#endif
				cameraShake.animate(time);
			}
#endif
		}
#endif

#if !DISABLE_LIGHTS
		bool isFadingOut;
		float fadingOutStartTime;
		public void FadeOut(float time)
		{
			if (animatedLights == null)
			{
				return;
			}

			if (!isFadingOut)
			{
				isFadingOut = true;
				fadingOutStartTime = time;
			}

			foreach (var animLight in animatedLights)
			{
				animLight.animateFadeOut(time - fadingOutStartTime);
			}
		}
#endif

#if UNITY_EDITOR
		// Editor preview
		// Detect when the Particle System is previewing and trigger this animation too

		[System.NonSerialized] ParticleSystem _parentParticle;
		ParticleSystem parentParticle
		{
			get
			{
				if (_parentParticle == null)
				{
					_parentParticle = this.GetComponent<ParticleSystem>();
				}
				return _parentParticle;
			}
		}
		[System.NonSerialized] public bool editorUpdateRegistered;

		[System.NonSerialized] bool particleWasStopped;
		[System.NonSerialized] float particleTime;
		[System.NonSerialized] float particleTimeUnwrapped;

		void OnDestroy()
		{
			UnregisterEditorUpdate();
		}

		public void RegisterEditorUpdate()
		{
			var type = PrefabUtility.GetPrefabAssetType(this.gameObject);
			var status = PrefabUtility.GetPrefabInstanceStatus(this.gameObject);

			// Prefab in Project window
			if ((type == PrefabAssetType.Regular || type == PrefabAssetType.Variant) && status == PrefabInstanceStatus.NotAPrefab)
			{
				return;
			}

			if (!editorUpdateRegistered)
			{
				EditorApplication.update += onEditorUpdate;
				editorUpdateRegistered = true;
			}
		}

		public void UnregisterEditorUpdate()
		{
			if (editorUpdateRegistered)
			{
				editorUpdateRegistered = false;
				EditorApplication.update -= onEditorUpdate;
			}
			ResetState();
		}

		void onEditorUpdate()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
			{
				return;
			}

			if (this == null)
			{
				return;
			}

			var renderer = this.GetComponent<ParticleSystemRenderer>();
			if (renderer.sharedMaterial != null && renderer.sharedMaterial.IsKeywordEnabled("_CFXR_LIGHTING_WPOS_OFFSET"))
			{
				if (materialPropertyBlock == null)
				{
					materialPropertyBlock = new MaterialPropertyBlock();
				}

				renderer.GetPropertyBlock(materialPropertyBlock);
				materialPropertyBlock.SetVector("_GameObjectWorldPosition", this.transform.position);
				renderer.SetPropertyBlock(materialPropertyBlock);
			}

			// Need to track unwrapped time when playing back from Editor
			// because the parentParticle.time will be reset at each loop
			float delta = parentParticle.time - particleTime;

			if (delta < 0 && parentParticle.isPlaying)
			{
				delta = parentParticle.main.duration + delta;
				if (delta > 0.1 || delta < 0)
				{
					// try to detect when "Restart" is pressed
					ResetState();
					particleTimeUnwrapped = 0;
					delta = 0;
				}
			}
			particleTimeUnwrapped += delta;

			if (particleTime != parentParticle.time)
			{
#if !DISABLE_CAMERA_SHAKE
				if (cameraShake != null && cameraShake.enabled && parentParticle.time < particleTime && parentParticle.time < 0.05f)
				{
					cameraShake.StartShake();
				}
#endif
#if !DISABLE_LIGHTS || !DISABLE_CAMERA_SHAKES
				Animate(particleTimeUnwrapped);

				if (!parentParticle.isEmitting)
				{
					FadeOut(particleTimeUnwrapped);
				}
#endif
			}

			if (particleWasStopped != parentParticle.isStopped)
			{
				if (parentParticle.isStopped)
				{
					ResetState();
				}
				particleTimeUnwrapped = 0;
			}

			particleWasStopped = parentParticle.isStopped;
			particleTime = parentParticle.time;
		}
#endif
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(CFXR_Effect))]
	[CanEditMultipleObjects]
	public class CFXR_Effect_Editor : Editor
	{
		bool? lightEditorPreview;
		bool? shakeEditorPreview;

		GUIStyle _PaddedRoundedRect;
		GUIStyle PaddedRoundedRect
		{
			get
			{
				if (_PaddedRoundedRect == null)
				{
					_PaddedRoundedRect = new GUIStyle(EditorStyles.helpBox);
					_PaddedRoundedRect.padding = new RectOffset(4, 4, 4, 4);
				}
				return _PaddedRoundedRect;
			}
		}

		public override void OnInspectorGUI()
		{
			GlobalOptionsGUI();

#if DISABLE_CAMERA_SHAKE
			EditorGUILayout.HelpBox("Camera Shake has been globally disabled in the code.\nThe properties remain to avoid data loss but the shaking won't be applied for any effect.", MessageType.Info);
#endif

			base.OnInspectorGUI();
		}

		void GlobalOptionsGUI()
		{
			EditorGUILayout.BeginVertical(PaddedRoundedRect);
			{
				GUILayout.Label("Editor Preview:", EditorStyles.boldLabel);

				if (lightEditorPreview == null)
				{
					lightEditorPreview = EditorPrefs.GetBool("CFXR Light EditorPreview", true);
				}
				bool lightPreview = EditorGUILayout.Toggle("Light Animations", lightEditorPreview.Value);
				if (lightPreview != lightEditorPreview.Value)
				{
					lightEditorPreview = lightPreview;
					EditorPrefs.SetBool("CFXR Light EditorPreview", lightPreview);
					CFXR_Effect.AnimatedLight.editorPreview = lightPreview;
				}

#if !DISABLE_CAMERA_SHAKE
				if (shakeEditorPreview == null)
				{
					shakeEditorPreview = EditorPrefs.GetBool("CFXR CameraShake EditorPreview", true);
				}
				bool shakePreview = EditorGUILayout.Toggle("Camera Shake", shakeEditorPreview.Value);
				if (shakePreview != shakeEditorPreview.Value)
				{
					shakeEditorPreview = shakePreview;
					EditorPrefs.SetBool("CFXR CameraShake EditorPreview", shakePreview);
					CFXR_Effect.CameraShake.editorPreview = shakePreview;
				}
#endif
			}
			EditorGUILayout.EndVertical();
		}

		void OnEnable()
		{
			if (this.targets == null)
			{
				return;
			}

			foreach (var t in this.targets)
			{
				var cfxr_effect = t as CFXR_Effect;
				if (cfxr_effect != null)
				{
					if (isPrefabSource(cfxr_effect.gameObject))
					{
						return;
					}
					cfxr_effect.RegisterEditorUpdate();
				}
			}
		}

		void OnDisable()
		{
			if (this.targets == null)
			{
				return;
			}

			foreach (var t in this.targets)
			{
				// Can be null if GameObject has been destroyed
				var cfxr_effect = t as CFXR_Effect;
				if (cfxr_effect != null)
				{
					if (isPrefabSource(cfxr_effect.gameObject))
					{
						return;
					}
					cfxr_effect.UnregisterEditorUpdate();
				}
			}
		}

		static bool isPrefabSource(GameObject gameObject)
		{
			var assetType = PrefabUtility.GetPrefabAssetType(gameObject);
			var prefabType = PrefabUtility.GetPrefabInstanceStatus(gameObject);
			return ((assetType == PrefabAssetType.Regular || assetType == PrefabAssetType.Variant) && prefabType == PrefabInstanceStatus.NotAPrefab);
		}
	}
#endif
}
;