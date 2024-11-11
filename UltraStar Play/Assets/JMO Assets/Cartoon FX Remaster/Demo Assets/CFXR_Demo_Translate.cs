//--------------------------------------------------------------------------------------------------------------------------------
// Cartoon FX
// (c) 2012-2020 Jean Moreno
//--------------------------------------------------------------------------------------------------------------------------------

using UnityEngine;

namespace CartoonFX
{
	public class CFXR_Demo_Translate : MonoBehaviour
	{
		public Vector3 direction = new Vector3(0,1,0);
		public bool randomRotation;


		bool initialized;
		Vector3 initialPosition;

		void Awake()
		{
			if (!initialized)
			{
				initialized = true;
				initialPosition = this.transform.position;
			}
		}

		void OnEnable()
		{
			this.transform.position = initialPosition;
			if (randomRotation)
			{
				this.transform.eulerAngles = Vector3.Lerp(Vector3.zero, Vector3.up * 360, Random.value);
			}
		}

		void Update()
		{
			this.transform.Translate(direction * Time.deltaTime);
		}
	}
}