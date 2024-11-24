using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// Cartoon FX - (c) 2015 - Jean Moreno
//
// Script handling the Demo scene of the Cartoon FX Packs

public class CFX_Demo_New : MonoBehaviour
{
	public Renderer groundRenderer;
	public Collider groundCollider;
	[Space]
	[Space]
	public Image slowMoBtn;
	public Text slowMoLabel;
	public Image camRotBtn;
	public Text camRotLabel;
	public Image groundBtn;
	public Text groundLabel;
	[Space]
	public Text EffectLabel;
	public Text EffectIndexLabel;

	//-------------------------------------------------------------

	private GameObject[] ParticleExamples;
	private int exampleIndex;
	private bool slowMo;
	private Vector3 defaultCamPosition;
	private Quaternion defaultCamRotation;
	
	private List<GameObject> onScreenParticles = new List<GameObject>();
	
	//-------------------------------------------------------------
	
	void Awake()
	{
		List<GameObject> particleExampleList = new List<GameObject>();
		int nbChild = this.transform.childCount;
		for(int i = 0; i < nbChild; i++)
		{
			GameObject child = this.transform.GetChild(i).gameObject;
			particleExampleList.Add(child);
		}
		particleExampleList.Sort( delegate(GameObject o1, GameObject o2) { return o1.name.CompareTo(o2.name); } );
		ParticleExamples = particleExampleList.ToArray();
		
		defaultCamPosition = Camera.main.transform.position;
		defaultCamRotation = Camera.main.transform.rotation;
		
		StartCoroutine("CheckForDeletedParticles");
		
		UpdateUI();
	}
	
	void Update()
	{
		if(Input.GetKeyDown(KeyCode.LeftArrow))
		{
			prevParticle();
		}
		else if(Input.GetKeyDown(KeyCode.RightArrow))
		{
			nextParticle();
		}
		else if(Input.GetKeyDown(KeyCode.Delete))
		{
			destroyParticles();
		}
		
		if(Input.GetMouseButtonDown(0))
		{
			RaycastHit hit = new RaycastHit();
			if(groundCollider.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 9999f))
			{
				GameObject particle = spawnParticle();
				particle.transform.position = hit.point + particle.transform.position;
			}
		}
		
		float scroll = Input.GetAxis("Mouse ScrollWheel");
		if(scroll != 0f)
		{
			Camera.main.transform.Translate(Vector3.forward * (scroll < 0f ? -1f : 1f), Space.Self);
		}
		
		if(Input.GetMouseButtonDown(2))
		{
			Camera.main.transform.position = defaultCamPosition;
			Camera.main.transform.rotation = defaultCamRotation;
		}
	}

	//-------------------------------------------------------------
	// MESSAGES

	public void OnToggleGround()
	{
		var c = Color.white;
		groundRenderer.enabled = !groundRenderer.enabled;
		c.a = groundRenderer.enabled ? 1f : 0.33f;
		groundBtn.color = c;
		groundLabel.color = c;
	}

	public void OnToggleCamera()
	{
		var c = Color.white;
		CFX_Demo_RotateCamera.rotating = !CFX_Demo_RotateCamera.rotating;
		c.a = CFX_Demo_RotateCamera.rotating ? 1f : 0.33f;
		camRotBtn.color = c;
		camRotLabel.color = c;
	}
	
	public void OnToggleSlowMo()
	{
		var c = Color.white;

		slowMo = !slowMo;
		if(slowMo)
		{
			Time.timeScale = 0.33f;
			c.a = 1f;
		}
		else
		{
			Time.timeScale = 1.0f;
			c.a = 0.33f;
		}

		slowMoBtn.color = c;
		slowMoLabel.color = c;
	}

	public void OnPreviousEffect()
	{
		prevParticle();
	}

	public void OnNextEffect()
	{
		nextParticle();
	}
	
	//-------------------------------------------------------------
	// UI
	
	private void UpdateUI()
	{
		EffectLabel.text = ParticleExamples[exampleIndex].name;
		EffectIndexLabel.text = string.Format("{0}/{1}", (exampleIndex+1).ToString("00"), ParticleExamples.Length.ToString("00"));
	}
	
	//-------------------------------------------------------------
	// SYSTEM
	
	private GameObject spawnParticle()
	{
		GameObject particles = (GameObject)Instantiate(ParticleExamples[exampleIndex]);
		particles.transform.position = new Vector3(0,particles.transform.position.y,0);
		#if UNITY_3_5
			particles.SetActiveRecursively(true);
		#else
			particles.SetActive(true);
//			for(int i = 0; i < particles.transform.childCount; i++)
//				particles.transform.GetChild(i).gameObject.SetActive(true);
		#endif
		
		ParticleSystem ps = particles.GetComponent<ParticleSystem>();

#if UNITY_5_5_OR_NEWER
		if (ps != null)
		{
			var main = ps.main;
			if (main.loop)
			{
				ps.gameObject.AddComponent<CFX_AutoStopLoopedEffect>();
				ps.gameObject.AddComponent<CFX_AutoDestructShuriken>();
			}
		}
#else
		if(ps != null && ps.loop)
		{
			ps.gameObject.AddComponent<CFX_AutoStopLoopedEffect>();
			ps.gameObject.AddComponent<CFX_AutoDestructShuriken>();
		}
#endif

		onScreenParticles.Add(particles);
		
		return particles;
	}
	
	IEnumerator CheckForDeletedParticles()
	{
		while(true)
		{
			yield return new WaitForSeconds(5.0f);
			for(int i = onScreenParticles.Count - 1; i >= 0; i--)
			{
				if(onScreenParticles[i] == null)
				{
					onScreenParticles.RemoveAt(i);
				}
			}
		}
	}
	
	private void prevParticle()
	{
		exampleIndex--;
		if(exampleIndex < 0) exampleIndex = ParticleExamples.Length - 1;
		
		UpdateUI();
	}
	private void nextParticle()
	{
		exampleIndex++;
		if(exampleIndex >= ParticleExamples.Length) exampleIndex = 0;
		
		UpdateUI();
	}
	
	private void destroyParticles()
	{
		for(int i = onScreenParticles.Count - 1; i >= 0; i--)
		{
			if(onScreenParticles[i] != null)
			{
				GameObject.Destroy(onScreenParticles[i]);
			}
			
			onScreenParticles.RemoveAt(i);
		}
	}
}
