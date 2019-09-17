using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/* Attack Warning script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class AttackWarning : MonoBehaviour
    {

        [HideInInspector]
        public Vector3 Pos; //the source position of the enemy contact is registerd here

        [HideInInspector]
        public float Timer; //how long will this be enabled for?

        Image Img; //image component of this UI object

        void Start()
        {
            InvokeRepeating("Blink", 0.0f, 0.3f); //effect of the warning image

            Img = gameObject.GetComponent<Image>();
        }

        void Update()
        {
            //timer here:
            if (Timer > 0)
            {
                Timer -= Time.deltaTime;
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        //making the object blink
        void Blink()
        {
            if (gameObject.activeInHierarchy == true)
            {
                float Transparency = (Img.color.a == 0.0f) ? 0.5f : 0.0f;
                Img.color = new Color(Img.color.r, Img.color.g, Img.color.b, Transparency);
            }
        }
    }

}
