using UnityEngine;
using System.Collections;

/* Builder script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
	public class Builder : MonoBehaviour {
		[HideInInspector]
		public bool IsBuilding = false; //is the player constructing a building?
		[HideInInspector]
		public Building TargetBuilding; //does the player have a target building to construct?

        //Can the builder also construct free buildings that do not belong to the faction?
        public bool BuildFreeBuildings = false;

		public float HealthPerSecond = 5f; //Amount of health to give the building per second
		//Timer:
		float Timer; //timers that, when it ends, adds health points to the target building.

		//main unit script:
		[HideInInspector]
		public Unit UnitMvt;

		//Audio clips:
		public AudioClip[] BuildingAudio; //audio played when the unit is building.
		public AudioClip BuildingOrderAudio; //audio clip played when the unit is ordered to construct a builid.

		public GameObject BuilderObj; //The object that will be activated when the player starts building.

        [Header("Auto Build:")]

        //auto-build:
        public bool AutoBuild = true; //searches for buildings to construct them
        public float SearchReload = 5.0f; //timer before the builder looks for buildings to construct
        float SearchTimer;
        public float SearchRange = 20.0f; //the range at where the builder will search for buildings

        void Awake () {
			//get he unit mvt script:
			UnitMvt = gameObject.GetComponent<Unit> ();
		}

		void Update () {

			//If the player has a target building, then send him there:
			if (TargetBuilding != null) {
				if (TargetBuilding.Health >= TargetBuilding.MaxHealth) { //If the target building reached the maximum health:
					//stop building
					UnitMvt.StopMvt ();
					UnitMvt.CancelBuilding ();
				} else {	
					//If the unit is in range of the buidling to construct:
					if (UnitMvt.DestinationReached == true) {
						if (IsBuilding == false) {
							//Stop moving:
							UnitMvt.StopMvt ();

							//Start building:
							IsBuilding = true;

							//Inform the animator that we started building:
							UnitMvt.SetAnimState (UnitAnimState.Building);

							//Play the construction audio clips:
							if (BuildingAudio.Length > 0) {
								int AudioID = Random.Range (0, BuildingAudio.Length - 1);
								AudioManager.PlayAudio (gameObject, BuildingAudio [AudioID], true);
							}

							//activate the builder object: 
							if (BuilderObj != null) {
								BuilderObj.SetActive (true);
							}

							//custom event:
							if (UnitMvt.GameMgr.Events)
								UnitMvt.GameMgr.Events.OnUnitStartBuilding (UnitMvt, TargetBuilding);

							Timer = 1.0f;
						}
					}

					//Adding health to the building:
					if (IsBuilding == true) {

						//building timer:
						if (UnitMvt.Moving == false) {
							if (Timer > 0) {
								Timer -= Time.deltaTime;
							}
							if (Timer <= 0) {
								Timer = 1.0f;
								TargetBuilding.AddHealth (HealthPerSecond, null); //adding health points each second
							}
						}
                    }
				}
			} else {
				if (IsBuilding == true) {
					UnitMvt.StopMvt ();
					UnitMvt.CancelBuilding ();
				}

				if (AutoBuild == true && UnitMvt.IsIdle() == true) { //if the unit can build automatically and is not doing any other task
					if (GameManager.PlayerFactionID == UnitMvt.FactionID) { //if this is the local player in a MP game or if this is simply an offline game
						if (SearchTimer > 0) {
							SearchTimer -= Time.deltaTime;
						} else {
							//search for units 
							int i = 0;
							while (TargetBuilding == null && i < UnitMvt.FactionMgr.Buildings.Count) {//loop through the faction's buildings
                                //make sure there's a valid building here:
                                if (UnitMvt.FactionMgr.Buildings[i])
                                {
                                    if (Vector3.Distance(UnitMvt.FactionMgr.Buildings[i].transform.position, transform.position) < SearchRange)
                                    {
                                        //if the building does not have full health
                                        if (UnitMvt.FactionMgr.Buildings[i].Health < UnitMvt.FactionMgr.Buildings[i].MaxHealth)
                                        {
                                            SetTargetBuilding(UnitMvt.FactionMgr.Buildings[i]); //target found.
                                        }
                                    }
                                }
								i++;
							}
							SearchTimer = SearchReload; //reload the search timer.
						}
					}
				}
			}

		}

        //Set the building's that the unit will construct:
        public void SetTargetBuilding(Building Target)
        {
            //if it's as single player game.
            if (GameManager.MultiplayerGame == false)
            {
                //directly send the unit to build
                SetTargetBuildingLocal(Target);
            }
            else
            {
                //in a case of a MP game
                //and it's the unit belongs to the local player:
                if (GameManager.Instance.IsLocalPlayer(gameObject))
                {
                    //ask the server to tell all clients at once that this unit is going to construct a building.

                    //send input action to the input manager
                    InputVars NewInputAction = new InputVars();
                    //mode:
                    NewInputAction.SourceMode = (byte)InputSourceMode.Unit;
                    NewInputAction.TargetMode = (byte)InputTargetMode.Building;

                    NewInputAction.Source = gameObject;

                    NewInputAction.Target = Target.gameObject;

                    NewInputAction.InitialPos = transform.position;
                    NewInputAction.TargetPos = Target.transform.position;

                    InputManager.SendInput(NewInputAction);
                }
            }
        }

        //Set the building's that the unit will construct:
        public void SetTargetBuildingLocal (Building Target)
		{
			if (Target == null || TargetBuilding == Target)
				return;

			//Check first if the building needs construction by cheking if its current health is below the max health:
			if (Target.Health < Target.MaxHealth && Target.WorkerMgr.CurrentWorkers < Target.WorkerMgr.WorkerPositions.Length)
            {
                UnitMvt.CancelBuilding (); //stop constructing the current building

				IsBuilding = false;

				TargetBuilding = Target;

                //Move the unit to the building:
                MovementManager.Instance.MoveLocal(UnitMvt, TargetBuilding.WorkerMgr.AddWorker(UnitMvt), TargetBuilding.Radius, TargetBuilding.gameObject, InputTargetMode.Building);
            }
		}
	}
}