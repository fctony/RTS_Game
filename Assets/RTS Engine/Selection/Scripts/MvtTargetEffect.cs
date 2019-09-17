using UnityEngine;
using System.Collections;

/* Mvt Target Effect script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
	public class MvtTargetEffect : MonoBehaviour {

		public float LifeTime = 2.0f; //Time in seconds at which the object will be shown.
		float LifeTimer;

		//Flashing:
		public bool Flash = true; //If set to true, this object will be flashing during the time it is actiavted
		public float FlashEvery = 0.2f; //self explanatory? 

		void Start ()
		{
			if (Flash == true) {
				InvokeRepeating ("FlashRepeat", 0.0f, FlashEvery);
			}
		}

		public void Activate ()
		{
			LifeTimer = LifeTime; //Set the life timer.

			//Activate the object:
			gameObject.SetActive(true); 
		}

		void Update () {
			if (LifeTimer > 0.0f) {
				LifeTimer -= Time.deltaTime;
			}

			if (LifeTimer < 0.0f) {
				LifeTimer = 0.0f;
				gameObject.SetActive (false);
			}
		}

		void FlashRepeat ()
		{
			if (LifeTimer > 0.0f) {
				gameObject.SetActive (!gameObject.activeInHierarchy);
			}
		}


	}
}