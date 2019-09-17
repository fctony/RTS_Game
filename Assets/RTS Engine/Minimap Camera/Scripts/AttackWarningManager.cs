using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/* Attack Warning Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class AttackWarningManager : MonoBehaviour
    {

        public Canvas MinimapCanvas; //minimap canvas that include the warning icons
        public Camera MinimapCam; //the minimap camera

        public AttackWarning BaseAttackWarning; //base attack warning (that will be copied when more attack warnings are needed)
        List<AttackWarning> AttackWarnings = new List<AttackWarning>(); //a list that holds all created attack warnings
        public float WarningDuration = 3.0f; //how long will every attack warning be active for?

        public AudioClip AttackWarningAudio; //when assigned, this will be played each time an attack warning is shown

        public static AttackWarningManager Instance = null; //we want a single instance of this component in the game scene

        public float MaxDistance = 10.0f; //each two attack warnings must have a distance over this between each other

        void Awake()
        {
            //set the instance:
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        //a method that checks if we can show enemy contact in a position
        public bool CanAddAttackWarning(Vector3 Pos)
        {
            //distance check: is the enemy contact is already highlighted in a defined range then no need to show it another time
            if (AttackWarnings.Count > 0)
            {
                //go through all the spawned attack warnings
                foreach (AttackWarning Warning in AttackWarnings)
                {
                    //if this one is active (it means it's enabled)
                    if (Warning.gameObject.activeInHierarchy == true)
                    {
                        //check the distance between this one and the requested position to show enemy contact at
                        if (Vector3.Distance(Warning.Pos, Pos) < MaxDistance)
                        { //if it's smaller than the defined max distance
                            return false; //don't show another enemy contact on the minimap
                        }
                    }
                }
            }
            return true;
        }

        //basically object pooling for the attack warnings
        public AttackWarning GetAttackWarning()
        {
            //if there is no attack warnings in our list
            if (AttackWarnings.Count == 0)
            {
                //add the base attack warning
                AttackWarnings.Add(BaseAttackWarning);
                //disable it for now (will be later enabled)
                BaseAttackWarning.gameObject.SetActive(false);
                return BaseAttackWarning; //return it
            }
            else
            { //if we already have attack warnings (that have been previously created)
              //we'll attempt to get an inactive one and use it
                foreach (AttackWarning Warning in AttackWarnings)
                { //go through them
                    if (Warning.gameObject.activeInHierarchy == false)
                    { //once we find one return it
                        return Warning;
                    }
                }

                //if we reach this point, then all our attack warnings are currently being used 
                //and therefore we'll need to create a new one
                GameObject NewWarning = Instantiate(BaseAttackWarning.gameObject) as GameObject;
                //settings for the new attack warning
                NewWarning.transform.SetParent(BaseAttackWarning.transform.parent, true);
                NewWarning.transform.rotation = BaseAttackWarning.transform.rotation;
                NewWarning.gameObject.GetComponent<RectTransform>().localPosition = BaseAttackWarning.GetComponent<RectTransform>().localPosition;
                //add it to the list
                AttackWarnings.Add(NewWarning.GetComponent<AttackWarning>());
                //return it
                return NewWarning.GetComponent<AttackWarning>();

            }
        }

        //method called each time a friendly unit/object is getting attacked and therefore requests a radar ping to be shown in the minimap
        public void AddAttackWarning(GameObject Source)
        {
            //first we check whether we can actually add a attack warning (if there isn't one already in that range)
            if (CanAddAttackWarning(Source.transform.position))
            {
                AttackWarning Warning = GetAttackWarning(); //this gets a attack warning that we can use (either an already created one that is unactive and therefore not used or create a new one)
                if (Warning != null)
                { //making sure we have a valid attack warning

                    //settings for the new 
                    Warning.Pos = Source.transform.position; //register the position of the source in the attack warning
                    Warning.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    Warning.Timer = WarningDuration;

                    //play audio:
                    if (AttackWarningAudio)
                        AudioManager.PlayAudio(GameManager.Instance.GeneralAudioSource.gameObject, AttackWarningAudio, false);

                    //set the position of the attack warning correctly on the minimap
                    SetAttackWarningPos(Warning.gameObject, Source.transform.position);

                    Warning.gameObject.SetActive(true); //activate the new attack warning
                }
            }
        }

        //sets the attack warning position
        public void SetAttackWarningPos(GameObject Warning, Vector3 NewPos)
        {
            Vector2 CanvasPos = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(MinimapCanvas.GetComponent<RectTransform>(), MinimapCam.WorldToScreenPoint(NewPos), MinimapCam, out CanvasPos);
            Warning.GetComponent<RectTransform>().localPosition = new Vector3(CanvasPos.x, CanvasPos.y, Warning.GetComponent<RectTransform>().localPosition.z);
        }
    }

}