using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
	public class RotateHeli : MonoBehaviour {

		public float Speed = 10.0f;

		// Update is called once per frame
		void Update () {
			//rotate the helicopter's motor:
			transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z + 10.0f*Speed*Time.deltaTime);
		}
	}
}