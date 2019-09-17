using UnityEngine;
using System.Collections;

namespace RTSEngine
{
	public class EffectObj : MonoBehaviour {

		public string Code; //Give each type of attack object a unique code used to identify it.

        public bool EnableLifeTime = true; //Control the lifetime of this effect object using the time right below?

		public float LifeTime = 3.0f; //Determine how long will the effect object will be shown for
		[HideInInspector]
		public float Timer;

		void Update ()
		{
            if (gameObject.activeInHierarchy == true)
            {
                if (EnableLifeTime == true)
                {
                    if (Timer > 0.0f)
                    {
                        Timer -= Time.deltaTime;
                    }
                    else
                    {
                        Timer = 0.0f;
                        Disable();
                    }
                }
            }
        }

        public void Disable()
        {
            gameObject.SetActive(false);
            //Make sure it has no parent obj:
            transform.SetParent(null, true);
        }
	}
}