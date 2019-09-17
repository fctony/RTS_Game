using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Minimap Icon script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class MinimapIcon : MonoBehaviour
    {
        // Update is called once per frame
        void Update()
        {
            //always keep the same rotation:
            transform.rotation = Quaternion.identity;
        }
    }
}