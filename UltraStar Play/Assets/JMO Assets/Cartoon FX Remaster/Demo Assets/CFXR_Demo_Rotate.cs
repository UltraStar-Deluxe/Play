//--------------------------------------------------------------------------------------------------------------------------------
// Cartoon FX
// (c) 2012-2020 Jean Moreno
//--------------------------------------------------------------------------------------------------------------------------------

using UnityEngine;

namespace CartoonFX
{
	public class CFXR_Demo_Rotate : MonoBehaviour
	{
		public Vector3 axis = new Vector3(0,1,0);
		public Vector3 center;
		public float speed = 1.0f;

		void Update()
		{
			this.transform.RotateAround(center, axis, speed * Time.deltaTime);
		}
	}
}