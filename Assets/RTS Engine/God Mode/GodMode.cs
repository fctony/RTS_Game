using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/* God Mode script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class GodMode : MonoBehaviour
    {
        //is the god mode enabled?
        public static bool Enabled = false;
        public Text ButtonText; //the button's text that triggers the god mode

        void Start()
        {
            Enabled = false; //the god mode is initially disabled.
            ButtonText.color = Color.red; //initially set the button's color to red, meaning that the mode is disabled
        }

        //a method to toggle god mode
        public void ToggleGodMode()
        {
            Enabled = !Enabled;

            //change the color of the button's text
            ButtonText.color = (Enabled == true) ? Color.green : Color.red;
        }
    }
}
