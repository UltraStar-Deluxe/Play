//--------------------------------------------------------------------------------------------------------------------------------
// Cartoon FX
// (c) 2012-2020 Jean Moreno
//--------------------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CartoonFX
{
	public class CFXR_Demo : MonoBehaviour
	{
		//----------------------------------------------------------------------------------------------------------------------------
		// UI

		public void NextEffect()
		{
			index++;
			WrapIndex();
			PlayAtIndex();
		}

		public void PreviousEffect()
		{
			index--;
			WrapIndex();
			PlayAtIndex();
		}

		public void ToggleSlowMo()
		{
			slowMotion = !slowMotion;

			Time.timeScale = slowMotion ? 0.33f : 1.0f;

			var color = Color.white;
			color.a = slowMotion ? 1f : 0.33f;
			btnSlowMotion.color = color;
			lblSlowMotion.color = color;
		}

		public void ToggleCamera()
		{
			rotateCamera = !rotateCamera;

			var color = Color.white;
			color.a = rotateCamera ? 1f : 0.33f;
			btnCameraRotation.color = color;
			lblCameraRotation.color = color;
		}

		public void ToggleGround()
		{
			showGround = !showGround;

			ground.SetActive(showGround);

			var color = Color.white;
			color.a = showGround ? 1f : 0.33f;
			btnShowGround.color = color;
			lblShowGround.color = color;
		}

		public void ToggleCameraShake()
		{
			CFXR_Effect.GlobalDisableCameraShake = !CFXR_Effect.GlobalDisableCameraShake;

			var color = Color.white;
			color.a = CFXR_Effect.GlobalDisableCameraShake ? 0.33f : 1.0f;
			btnCamShake.color = color;
			lblCamShake.color = color;
		}

		public void ToggleEffectsLights()
		{
			CFXR_Effect.GlobalDisableLights = !CFXR_Effect.GlobalDisableLights;

			var color = Color.white;
			color.a = CFXR_Effect.GlobalDisableLights ? 0.33f : 1.0f;
			btnLights.color = color;
			lblLights.color = color;
		}

		public void ToggleBloom()
		{
			bloom.enabled = !bloom.enabled;

			var color = Color.white;
			color.a = !bloom.enabled ? 0.33f : 1.0f;
			btnBloom.color = color;
			lblBloom.color = color;
		}

		public void ResetCam()
		{
			Camera.main.transform.position = camInitialPosition;
			Camera.main.transform.rotation = camInitialRotation;
		}

		//----------------------------------------------------------------------------------------------------------------------------

		public Image btnSlowMotion;
		public Text lblSlowMotion;
		public Image btnCameraRotation;
		public Text lblCameraRotation;
		public Image btnShowGround;
		public Text lblShowGround;
		public Image btnCamShake;
		public Text lblCamShake;
		public Image btnLights;
		public Text lblLights;
		public Image btnBloom;
		public Text lblBloom;
		[Space]
		public Text labelEffect;
		public Text labelIndex;
		[Space]
		public GameObject ground;
		public Collider groundCollider;
		public Transform demoCamera;
		public MonoBehaviour bloom;
		public float rotationSpeed = 10f;
		public float zoomFactor = 1f;

		bool slowMotion = false;
		bool rotateCamera = false;
		bool showGround = true;

		//----------------------------------------------------------------------------------------------------------------------------

		[System.NonSerialized] public GameObject currentEffect;
		GameObject[] effectsList;
		int index = 0;

		Vector3 camInitialPosition;
		Quaternion camInitialRotation;

		void Awake()
		{
			camInitialPosition = Camera.main.transform.position;
			camInitialRotation = Camera.main.transform.rotation;

			var list = new List<GameObject>();
			for (int i = 0; i < this.transform.childCount; i++)
			{
				var effect = this.transform.GetChild(i).gameObject;
				list.Add(effect);

				var cfxrEffect= effect.GetComponent<CFXR_Effect>();
				if (cfxrEffect != null) cfxrEffect.clearBehavior = CFXR_Effect.ClearBehavior.Disable;
			}
			effectsList = list.ToArray();

			PlayAtIndex();
			UpdateLabels();
		}

		void Update()
		{
			if (rotateCamera)
			{
				demoCamera.RotateAround(Vector3.zero, Vector3.up, rotationSpeed * Time.deltaTime);
			}

			if (Input.GetKeyDown(KeyCode.Space))
			{
				if (currentEffect != null)
				{
					var ps = currentEffect.GetComponent<ParticleSystem>();
					if (ps.isEmitting)
					{
						ps.Stop(true);
					}
					else
					{
						if (!currentEffect.gameObject.activeSelf)
						{
							currentEffect.SetActive(true);
						}
						else
						{
							ps.Play(true);
							var cfxrEffects = currentEffect.GetComponentsInChildren<CFXR_Effect>();
							foreach (var cfxr in cfxrEffects)
							{
								cfxr.ResetState();
							}
						}
					}
				}
			}

			if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
			{
				if (currentEffect != null)
				{
					currentEffect.SetActive(false);
					currentEffect.SetActive(true);
				}
			}

			if (Input.GetKeyDown(KeyCode.LeftArrow))
			{
				PreviousEffect();
			}

			if (Input.GetKeyDown(KeyCode.RightArrow))
			{
				NextEffect();
			}

			if (Input.GetMouseButtonDown(0))
			{
				var ray = demoCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
				if (Physics.Raycast(ray))
				{
					if (currentEffect != null)
					{
						currentEffect.SetActive(false);
						currentEffect.SetActive(true);
					}
				}
			}

			if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
			{
				ResetCam();
			}

			float scroll = Input.GetAxis("Mouse ScrollWheel");
			if (scroll != 0f)
			{
				Camera.main.transform.Translate(Vector3.forward * (scroll < 0f ? -1f : 1f) * zoomFactor, Space.Self);
			}
		}

		public void PlayAtIndex()
		{
			if (currentEffect != null)
			{
				currentEffect.SetActive(false);
			}

			currentEffect = effectsList[index];
			currentEffect.SetActive(true);

			UpdateLabels();
		}

		void WrapIndex()
		{
			if (index < 0) index = effectsList.Length - 1;
			if (index >= effectsList.Length) index = 0;
		}

		void UpdateLabels()
		{
			labelEffect.text = currentEffect.name;
			labelIndex.text = string.Format("{0}/{1}", (index+1), effectsList.Length);
		}
	}
}