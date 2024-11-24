using UnityEngine;
using System.Collections;

// Cartoon FX  - (c) 2015 Jean Moreno

// Decreases a light's intensity over time.

[RequireComponent(typeof(Light))]
public class CFX_LightIntensityFade : MonoBehaviour
{
	// Duration of the effect.
	public float duration = 1.0f;
	
	// Delay of the effect.
	public float delay = 0.0f;
	
	/// Final intensity of the light.
	public float finalIntensity = 0.0f;
	
	// Base intensity, automatically taken from light parameters.
	private float baseIntensity;
	
	// If <c>true</c>, light will destructs itself on completion of the effect
	public bool autodestruct;
	
	private float p_lifetime = 0.0f;
	private float p_delay;
	
	void Start()
	{
		baseIntensity = GetComponent<Light>().intensity;
	}
	
	void OnEnable()
	{
		p_lifetime = 0.0f;
		p_delay = delay;
		if(delay > 0) GetComponent<Light>().enabled = false;
	}
	
	void Update ()
	{
		if(p_delay > 0)
		{
			p_delay -= Time.deltaTime;
			if(p_delay <= 0)
			{
				GetComponent<Light>().enabled = true;
			}
			return;
		}
		
		if(p_lifetime/duration < 1.0f)
		{
			GetComponent<Light>().intensity = Mathf.Lerp(baseIntensity, finalIntensity, p_lifetime/duration);
			p_lifetime += Time.deltaTime;
		}
		else
		{
			if(autodestruct)
				GameObject.Destroy(this.gameObject);
		}
		
	}
}
