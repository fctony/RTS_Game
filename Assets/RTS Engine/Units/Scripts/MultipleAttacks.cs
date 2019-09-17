using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Multiple Attacks script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class MultipleAttacks : MonoBehaviour
    {

        [HideInInspector]
        public Attack[] AttackTypes; //holds all the attack types that the unit has.
        [HideInInspector]
        public int BasicAttackID; //this is the basic attack

        //Can the different attack tasks be seen in the task panel? 
        public bool CanSwitchAttackTypes = true;

        //Should the unit switch back to basic attack after launching another attack type?
        public bool SwitchBackToBasic = true;

        [HideInInspector]
        private int ActiveAttackID; //saves the current active attack type id in the above list.
        public int AttackTypesTaskCategory = 0;

        Unit UnitMvt;

        void Start()
        {
            //get the unit comp:
            UnitMvt = GetComponent<Unit>();

            //get all the attack types in the unit:
            AttackTypes = GetComponents<Attack>();

            if (AttackTypes.Length > 1)
            { //go through all attack components
                for (int i = 0; i < AttackTypes.Length; i++)
                {
                    AttackTypes[i].AttackID = i;
                    //if we find the basic attack type:
                    if (AttackTypes[i].BasicAttack == true)
                    {
                        BasicAttackID = i;
                        UnitMvt.AttackMgr = AttackTypes[i]; //set as main attack manager.
                        ActiveAttackID = i;
                        AttackTypes[i].IsActive = true;
                    }
                    else
                    {
                        //disable this attack for now:
                        AttackTypes[i].IsActive = false;
                    }
                }
            }
            else
            {
                //if we have 1 or less attack types, we won't be needing this script then:
                //but first set that one attack as the basic attack type:
                if (AttackTypes.Length == 1)
                {
                    AttackTypes[0].AttackID = 0;
                    AttackTypes[0].BasicAttack = true;
                    UnitMvt.AttackMgr = AttackTypes[0];
                }
                Destroy(this); //remove it then.
            }
        }

        public void EnableAttackType(int ID)
        {
            if (GameManager.MultiplayerGame == true)
            { //if this is a MP game and it's the local player:
                if (GameManager.Instance.IsLocalPlayer(gameObject))
                {
                    //send input action to the input manager
                    InputVars NewInputAction = new InputVars();
                    //mode:
                    NewInputAction.SourceMode = (byte)InputSourceMode.CustomCommand;
                    NewInputAction.TargetMode = (byte)InputCustomMode.MultipleAttacks;

                    NewInputAction.Source = gameObject;

                    NewInputAction.Value = ID;

                    InputManager.SendInput(NewInputAction);
                }
            }
            else
            {
                //offline game? update the attack type directly:
                EnableAttackTypeLocal(ID);
            }
        }

        public void EnableAttackTypeLocal(int ID) //called locally to change the attack type
        {
            //only if the new ID is different:
            if (ID == ActiveAttackID)
                return;

            //Cancel the attack
            UnitMvt.CancelAttack();
            AttackTypes[ActiveAttackID].ResetAttack(); //reset the old attack type

            //disable the last attack type:
            AttackTypes[ActiveAttackID].IsActive = false;

            if (AttackTypes[ActiveAttackID].AttackTarget != null)
            { //if the unit has an attack target in the last attack type
                MovementManager.Instance.LaunchAttack(UnitMvt, AttackTypes[ActiveAttackID].AttackTarget, MovementManager.AttackModes.Change); //set the same target for the new attack tpye
            }
            //enable the new one
            AttackTypes[ID].IsActive = true; //enable the new attack:
            UnitMvt.AttackMgr = AttackTypes[ID]; //set as the main attack comp

            //save the new attack ID.
            ActiveAttackID = ID;

            //update the tasks UI if the unit is selected:
            if (GameManager.Instance.SelectionMgr.SelectedUnits.Count == 1)
            {
                if (GameManager.Instance.SelectionMgr.SelectedUnits[0] == UnitMvt)
                {
                    GameManager.Instance.UIMgr.UpdateTaskPanel();
                }
            }

            //custom event
            if (GameManager.Instance.Events)
            {
                GameManager.Instance.Events.OnUnitSwitchAttack(UnitMvt, AttackTypes[ActiveAttackID], AttackTypes[ID]);
            }
        }
    }
}