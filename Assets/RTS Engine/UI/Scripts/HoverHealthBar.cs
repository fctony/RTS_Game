using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/* Hover Health Bar script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class HoverHealthBar : MonoBehaviour
    {
        //make sure the hover health bar is always looking at the camera
        Transform CamTransform;

        void Start ()
        {
            CamTransform = GameManager.Instance.CamMov.transform;
        }

        void Update()
        {
            //move the canvas in order to face the camera and look at it
            transform.LookAt(transform.position + CamTransform.rotation * Vector3.forward,
                CamTransform.rotation * Vector3.up);
        }
    }
}
