using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/* APC script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
	public class APC : MonoBehaviour {

        [Header("Adding Units:")]
        public Transform InteractionPos; //position where units get in/off the APC

        public bool AllowAllUnits = true; //allow all units to get in the APC?
        public bool AcceptUnitsInList = true; //this determines how the APC will handle the below list if the above bool is set to false, accept units defined there or deny them?
        public List<string> UnitsList = new List<string>(); //a list of the unit codes that are not allowed to get in the APC

		public int MaxAmount = 4; //max amount to transport at the same time.
		[HideInInspector]
		public List<Unit> CurrentUnits = new List<Unit>(); //the units contained in the APC unit.

        public AudioClip AddUnitAudio; //audio clip played when a unit gets in the APC

        [Header("Ejecting Units:")]
        public bool EjectSingleUnit = true; //can the player eject single units?
        public int EjectOneUnitTaskCategory = 0; //the category ID of ejecting one unit task. 

		//removing all units:
		public bool EjectAllUnits = true; //true when the APC is allowed to eject units all at once
		public int EjectAllUnitsTaskCategory = 0; //the category ID of ejecting all units at once
		public Sprite EjectAllIcon; //The icon of the task of ejecting all units at once

        public AudioClip RemoveUnitAudio; //audio clip played when a unit is removed from the APC

        [Header("Calling Units:")]
        //calling units:
        public bool CanCallUnits = true; //can the APC call units to get them inside?
		public int CallUnitsTaskCategory = 0; //the category ID of calling all units task
		public float CallingRange = 20.0f; //the range at which units will be called to get into the APC
		public Sprite CallUnitsIcon; //The task's icon that will eject all the contained units when launched.
		public bool StopUnitsFromAttackingOnCall = false; //stop units from attacking (if they are) when they are called? 
		
		public AudioClip CallUnitsAudio; //audio clip played when the APC is calling units

        [Header("Other Settings:")]
        public bool ReleaseOnDestroy = true; //if true, all units will be released on destroy, if false, all contained units will be destroyed.

		//other scripts:
		GameManager GameMgr;
		FactionManager FactionMgr;

        Unit UnitMgr;
        Building BuildingMgr;

		void Start ()
		{
			GameMgr = GameManager.Instance;
            UnitMgr = gameObject.GetComponent<Unit>();
            BuildingMgr = gameObject.GetComponent<Building>();

            //get the faction manager from the unit APC
            if (UnitMgr) {
				FactionMgr = GameMgr.Factions[UnitMgr.FactionID].FactionMgr;
			}
			//get the faction manager from the building APC.
			else if (BuildingMgr) {
				FactionMgr = GameMgr.Factions[BuildingMgr.FactionID].FactionMgr;
			}

            //if there's no interaction pos:
            if (InteractionPos == null)
                InteractionPos = transform; //set the interaction pos as the building's pos
		}

		//method to add a unit to the APC:
		public void AddUnit (Unit Unit)
		{
			//check if there is space left:
			if (CurrentUnits.Count < MaxAmount) {
				//add the unit:
				Unit.gameObject.SetActive(false);
				CurrentUnits.Add(Unit);
				Unit.transform.SetParent (transform, true);

				//deselect unit:
				if (GameMgr.SelectionMgr.SelectedUnits.Contains (Unit)) {
					GameMgr.SelectionMgr.DeselectUnit (Unit);
				}

				//play the audio clip:
				AudioManager.PlayAudio(this.gameObject, AddUnitAudio, false);

				//if this APC is selected:
				if (GameMgr.SelectionMgr.SelectedUnits.Contains (UnitMgr) || GameMgr.SelectionMgr.SelectedBuilding == BuildingMgr) {
                    GameMgr.UIMgr.UpdateTaskPanel(); //update the task panel
				}

				//custom event:
				if (GameMgr.Events != null) {
					GameMgr.Events.OnAPCAddUnit (this, Unit);
				}
			}
		}

        //a method to drop off units
        public void DropOffUnits (int ID)
        {
            if (GameManager.MultiplayerGame == true)
            { //if this is a MP game and it's the local player:
                if (GameMgr.IsLocalPlayer(gameObject))
                {
                    //send input action to the input manager
                    InputVars NewInputAction = new InputVars();
                    //mode:
                    NewInputAction.SourceMode = (byte)InputSourceMode.CustomCommand;
                    NewInputAction.TargetMode = (byte)InputCustomMode.APCDrop;

                    NewInputAction.Source = gameObject;

                    NewInputAction.Value = ID;

                    InputManager.SendInput(NewInputAction);
                }
            }
            else
            {
                //offline game? update the attack type directly:
                DropOffUnitsLocal(ID);
            }
        }

        public void DropOffUnitsLocal(int ID)
		{
			//when ID is 0, then all units inside the APC will be removed.
			if (ID == 0) { //if the ID is 0
				int Count = CurrentUnits.Count;
				for (int i = 0; i < Count; i++) { //all units inside the APC are removed
					RemoveUnit (CurrentUnits [0]);
				}
			} else { //if the ID is > 0, then remove only one unit
				RemoveUnit (CurrentUnits [ID - 1]); //remove only one unit from the APC
			}
		}

		//method to remove a unit from the APC:
		public void RemoveUnit (Unit Unit)
		{
			//check if the unit is actually in the APC
			if (CurrentUnits.Contains(Unit)) {
				//remove the unit:
				Unit.transform.SetParent (null, true);
				CurrentUnits.Remove(Unit);
				Unit.gameObject.SetActive(true); //set it active again1

				//play the audio clip:
				AudioManager.PlayAudio(this.gameObject, RemoveUnitAudio, false);

                //if this APC is selected:
                if (GameMgr.SelectionMgr.SelectedUnits.Contains(UnitMgr) || GameMgr.SelectionMgr.SelectedBuilding == BuildingMgr)
                {
                    GameMgr.UIMgr.UpdateTaskPanel(); //update the task panel
                }

                //custom event:
                if (GameMgr.Events != null) {
					GameMgr.Events.OnAPCRemoveUnit (this, Unit);
				}
			}
		}

		//called when the APC requests nearby units to enter.
		public void CallForUnits ()
		{
			int i = 0; //counter
			AudioManager.PlayAudio(this.gameObject, CallUnitsAudio, false); //play the call for units audio
			while (i < FactionMgr.Units.Count && CurrentUnits.Count < MaxAmount) { //go through the faction's units while still making sure that there is more space for units to get in
				//the target unit can't be another APC and it must be active and either the APC accepts all units or the unit's code is not in the exceptions list
				if (!FactionMgr.Units [i].gameObject.GetComponent<APC>() && (UnitsList.Contains (FactionMgr.Units [i].Code) == AcceptUnitsInList || AllowAllUnits == true) && FactionMgr.Units [i].gameObject.activeInHierarchy == true) {
					//if the unit is at a distance that is less or equal to the calling distance
					if (Vector3.Distance (this.transform.position, FactionMgr.Units [i].transform.position) <= CallingRange) {
						if (StopUnitsFromAttackingOnCall == true) { //if the APC can stop units from attacking
							if (FactionMgr.Units [i].GetComponent<Attack> ()) { //then check if they have an attack component
								FactionMgr.Units [i].CancelAttack (); //cancel the attack
								FactionMgr.Units [i].GetComponent<Attack> ().AttackTarget = null;
							}
						}
						//set the target APC for the nearby unit
						FactionMgr.Units [i].TargetAPC = this;
                        //make the unit move to the APC.
                        MovementManager.Instance.Move(FactionMgr.Units[i], InteractionPos.position, 0.0f, gameObject, (BuildingMgr) ? InputTargetMode.Building : InputTargetMode.Unit);
					}
				}
				i++;
			}
		}
	}
}