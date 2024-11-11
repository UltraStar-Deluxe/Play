using UnityEngine;
using System.Collections;

// Cartoon FX  - (c) 2015 Jean Moreno

public class CFX_Demo_RotateCamera : MonoBehaviour
{
	static public bool rotating = true;
	
	public float speed = 30.0f;
	public Transform rotationCenter;
	
	void Update ()
	{
		if(rotating)
			transform.RotateAround(rotationCenter.position, Vector3.up, speed*Time.deltaTime);
	}
}
