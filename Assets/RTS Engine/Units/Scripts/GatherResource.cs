using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

/* Gather Resource script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
	public class GatherResource : MonoBehaviour {


		public int MaxHoldQuantity = 7; //the maximum quantity of each resource that the unit can hold before having to drop it off at the closet building that allows him to do so.
		[System.Serializable]
		public class DropOffVars //this class holds the current amount of each resource the unit is holding:
		{
			public int CurrentQuantity = 0;
			public string Name = "";

		}
		[HideInInspector]
		public List<DropOffVars> DropOff = new List<DropOffVars>();

		[HideInInspector]
		public Building DropOffBuilding; //where does the unit drops resources at.
		[HideInInspector]
		public bool DroppingOff = false; //is the unit dropping resources?
        [HideInInspector]
        public bool GoingToDropOffBuilding = false;
		[HideInInspector]
		public bool GoingBack = false; //is the unit going back to collect after it had dropped the resources

		[HideInInspector]
		public bool IsCollecting = false; //is the unity collecting resources
		[HideInInspector]
		public Resource TargetResource; //the resource that the unit is collecting from

		//Timer:
		float Timer;

		//other scripts:
		[HideInInspector]
		public Unit UnitMvt;

		ResourceManager ResourceMgr;
		SelectionManager SelectionMgr;

		//
		public GameObject DropOffObj; //activated when the player is dropping off resources.
		[System.Serializable]
		public class CollectionInfoVars
		{
			public string ResourceName;
			public GameObject Obj;
			public AnimatorOverrideController AnimatorOverride; //if collecting this type of resource enables a special type of animation.
		}
		public CollectionInfoVars[] CollectionInfo; //activated when the unit is collecting a resource
		[HideInInspector]
		public GameObject CurrentCollectionObj;

        //Audio clip:
        public AudioClip SendToCollectAudio; //Audio played when the player sends this unit to collect from this resource.

        [Header("Auto Collect:")]

        //auto-build:
        public bool AutoCollect = true; //searches for resources to collect them
        public float SearchReload = 5.0f; //timer before the collector looks for resources to construct
        float SearchTimer;
        public float SearchRange = 20.0f; //the range at where the collector will search for resources

        void Start () {
			//get sripts:
			UnitMvt = gameObject.GetComponent<Unit> ();
			ResourceMgr = GameManager.Instance.ResourceMgr;
			SelectionMgr = GameManager.Instance.SelectionMgr;

			//Set the drop off initial settings:
			DropOff.Clear ();
			for (int i = 0; i < ResourceMgr.ResourcesInfo.Length; i++) {
				DropOffVars DropOffResource = new DropOffVars ();
				DropOffResource.Name = ResourceMgr.ResourcesInfo [i].TypeInfo.Name;
				DropOffResource.CurrentQuantity = 0;
				DropOff.Add (DropOffResource);
			}
			DroppingOff = false;

            //If the drop off model has been assigned
            if(DropOffObj != null)
            {
                //turn it off in the beginning
                DropOffObj.SetActive(false);
            }
		}

		void Update () {
			//If the player has a target resource, then send him there:
			if (TargetResource != null) {
				if (TargetResource.Amount >= 1) { 
					if (ResourceMgr.AutoCollect == true || (ResourceMgr.AutoCollect == false && DroppingOff == false)) {
						//If the unit is in range of the resource to collect from:
						if (UnitMvt.DestinationReached == true) {
							if (IsCollecting == false) {
								//Stop moving:
								UnitMvt.StopMvt ();

								//Start collecting:
								IsCollecting = true;
								GoingBack = false;

								//Start playing the collection audio:
								if (ResourceMgr.ResourcesInfo [TargetResource.ResourceID].TypeInfo.CollectionAudio.Count > 0) {
									int AudioID = Random.Range (0, ResourceMgr.ResourcesInfo [TargetResource.ResourceID].TypeInfo.CollectionAudio.Count - 1);
									AudioManager.PlayAudio (gameObject, ResourceMgr.ResourcesInfo [TargetResource.ResourceID].TypeInfo.CollectionAudio [AudioID], true);

								}

								//activate the collection object:
								if (CurrentCollectionObj != null) {
									CurrentCollectionObj.SetActive (true);
								}

								//start the collection timer:
								Timer = TargetResource.CollectOneUnitTime;
								UnitMvt.SetAnimState (UnitAnimState.Collecting);

								DroppingOff = false;
							}
						}

						/*//If the unit has been moved unexpectedly while collecting:
						else if ((IsCollecting == true || GoingBack == true) && UnitMvt.Moving == false) {
							//Bring the unit back:
							SetTargetResource (TargetResource);
						}*/
					} else {
						if (DropOffBuilding != null && GoingToDropOffBuilding == true) { //if there's a dropoff building and the player is actually going there.
							//If the unit mission is to drop off the collected resources:
							if (UnitMvt.DestinationReached == true) {
								DropOffResources ();
								IsCollecting = false;

								//Hide the drop off object
								if (DropOffObj != null) {
									DropOffObj.SetActive (false);
								}
							}
						}
					}

					//Collecting resources to the building:
					if (IsCollecting == true) {
						if (UnitMvt.Moving == false) {
							if (DroppingOff == false) {

								//resource collection timer:
								if (Timer > 0) {
									Timer -= Time.deltaTime;
								}
								if (Timer <= 0) {
									Timer = TargetResource.CollectOneUnitTime;

									if (TargetResource.gameObject.GetComponent<Treasure> ()) { //if this a treasure and not a resource.
										//destroy the treasure directly:
										TargetResource.Amount = 0;
										TargetResource.DestroyResource (this);
									} else { //normal resource:
										//if there's still amount in the target resource
										if (TargetResource.Amount - 1 >= 0) {
											TargetResource.AddResourceAmount (-1.0f, this);
										}
									}

									//If the target resource is the one that's selected:
									if (SelectionMgr.SelectedResource == TargetResource && TargetResource != null) {
										//Update the resource UI:
										SelectionMgr.UIMgr.UpdateResourceUI (TargetResource);
									}

								}
							}
						}
					}
				}
			} else {
				if (IsCollecting == true) {
					UnitMvt.StopMvt ();
					UnitMvt.CancelCollecting ();
				}

				if (AutoCollect == true && UnitMvt.IsIdle() == true) { //if the unit can collect automatically and is not doing any other task
					if (GameManager.PlayerFactionID == UnitMvt.FactionID) { //if this is the local player in a MP game or if this is simply an offline game
						if (SearchTimer > 0) {
							SearchTimer -= Time.deltaTime;
						} else {
							//search for units 
							int i = 0;
							while (TargetResource == null && i < GameManager.Instance.ResourceMgr.AllResources.Count) {//loop through all resources
                                if (GameManager.Instance.ResourceMgr.AllResources[i]) //if the resource is valid
                                {
                                    if (GameManager.Instance.ResourceMgr.AllResources[i].gameObject.activeInHierarchy == true && GameManager.Instance.ResourceMgr.AllResources[i].FactionID == UnitMvt.FactionID)
                                    { //resource mut have same name and must be active
                                        if (Vector3.Distance(GameManager.Instance.ResourceMgr.AllResources[i].transform.position, transform.position) < SearchRange)
                                        {
                                            //if the resource has amount
                                            if (GameManager.Instance.ResourceMgr.AllResources[i].Amount > 0)
                                            {
                                                SetTargetResource(GameManager.Instance.ResourceMgr.AllResources[i]); //target found.
                                            }
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

        //Set the resource's that the unit will collect from:
        public void SetTargetResource(Resource Target)
        {
            //if it's as single player game.
            if (GameManager.MultiplayerGame == false)
            {
                //directly send the unit to collect
                SetTargetResourceLocal(Target);
            }
            else
            {
                //in a case of a MP game
                //and the unit belongs to the local player:
                if (GameManager.Instance.IsLocalPlayer(gameObject))
                {
                    //ask the server to tell all clients at once that this unit is going to resource from this resource:

                    //send input action to the input manager
                    InputVars NewInputAction = new InputVars();
                    //mode:
                    NewInputAction.SourceMode = (byte)InputSourceMode.Unit;
                    NewInputAction.TargetMode = (byte)InputTargetMode.Resource;

                    NewInputAction.Source = gameObject;

                    NewInputAction.Target = Target.gameObject;

                    NewInputAction.InitialPos = transform.position;
                    NewInputAction.TargetPos = Target.transform.position;

                    InputManager.SendInput(NewInputAction);
                }
            }
        }

        public void SetTargetResourceLocal (Resource Target)
		{
			if (Target == null)
				return;

			//Check first if the resource needs collectors:
			if (Target.Amount > 0) {
                if (TargetResource != Target)
                {
                    if (Target.WorkerMgr.CurrentWorkers < Target.WorkerMgr.WorkerPositions.Length)
                    {
                        UnitMvt.CancelCollecting(); //stop collecting from the last resource

                        TargetResource = null;
                        IsCollecting = false;

                        TargetResource = Target;

                        //Search for the nearest drop off building if we are not automatically collecting:
                        if (ResourceMgr.AutoCollect == false)
                        {
                            FindClosetDropOffBuilding();
                        }

                        CurrentCollectionObj = null;
                        //look for the collection object if it exists:
                        if (CollectionInfo.Length > 0)
                        {
                            for (int i = 0; i < CollectionInfo.Length; i++)
                            {
                                if (CollectionInfo[i].ResourceName == TargetResource.Name)
                                {
                                    CurrentCollectionObj = CollectionInfo[i].Obj; //set the collection object:

                                    //set the collection animation:
                                    if (CollectionInfo[i].AnimatorOverride != null)
                                    {
                                        UnitMvt.AnimMgr.runtimeAnimatorController = CollectionInfo[i].AnimatorOverride;
                                    }
                                }
                            }
                        }

                        //custom event:
                        if (UnitMvt.GameMgr.Events && DroppingOff == false)
                            UnitMvt.GameMgr.Events.OnUnitStartCollecting(UnitMvt, TargetResource);

                        //Move the unit to the resource by registering the unit in the worker manager
                        MovementManager.Instance.MoveLocal(UnitMvt, TargetResource.WorkerMgr.AddWorker(UnitMvt), TargetResource.Radius, TargetResource.gameObject, InputTargetMode.Resource);
                    }
                }
                else
                {
                    MovementManager.Instance.MoveLocal(UnitMvt, TargetResource.WorkerMgr.GetWorkerPos(UnitMvt.LastWorkerPosID), TargetResource.Radius, TargetResource.gameObject, InputTargetMode.Resource);
                }
            }
		}

		//Find the closet drop off building:
		public void FindClosetDropOffBuilding ()
		{
			if (UnitMvt.FactionMgr != null) {
				if (TargetResource != null) {
					if (UnitMvt.FactionMgr.DropOffBuildings.Count > 0) {
						Building CurrentBuilding = null;
						float Distance = 0.0f;
						//Loop through all the existing drop off buildings:
						foreach (Building PossibleBuilding in UnitMvt.FactionMgr.DropOffBuildings) {
							if (PossibleBuilding != null) {
								if (PossibleBuilding.IsBuilt == true && PossibleBuilding.Placed == true) {
									if (PossibleBuilding.CanDrop (TargetResource.Name)) { //if the player can drop the target resource at this drop off building
										//if the target resource has a drop range then the drop off building must be inside that range, if not then proceed.
										if(TargetResource.HaveDropOffRange == false || (Vector3.Distance (TargetResource.transform.position, PossibleBuilding.transform.position) < TargetResource.DropOffRange && TargetResource.HaveDropOffRange == true))
										//Pick the closet building to the resource.
										if (CurrentBuilding == null) {
											CurrentBuilding = PossibleBuilding;
											Distance = Vector3.Distance (TargetResource.transform.position, CurrentBuilding.transform.position);
										} else if (Vector3.Distance (TargetResource.transform.position, PossibleBuilding.transform.position) < Distance) {
											CurrentBuilding = PossibleBuilding;
											Distance = Vector3.Distance (TargetResource.transform.position, CurrentBuilding.transform.position);
										}
									}
								}
							}
						}
						DropOffBuilding = CurrentBuilding;

					}
				}

				if (DropOffBuilding == null) {
					if (UnitMvt.FactionID == GameManager.PlayerFactionID) {
						//message that there's no drop off building.
						UnitMvt.UIMgr.ShowPlayerMessage ("There's no drop off building in this map!", UIManager.MessageTypes.Error);
					}
				}
			}

		}

        //Sending unit to drop off resources:
        public void SendUnitToDropOffResources()
        {
            //if there's no drop off building
            if (DropOffBuilding == null)
            {
                FindClosetDropOffBuilding(); //try to find one:
            }

            if (DropOffBuilding != null)
            { //if there's a drop off building
              //stop the gathering audio:
                AudioManager.StopAudio(gameObject);
                
                //if we're using legacy mvt then set the drop off building as the drop pos, if not use the pre defined drop off pos
                Vector3 DropPos = (DropOffBuilding.DropOffPos == null) ? DropOffBuilding.transform.position : DropOffBuilding.DropOffPos.transform.position;
                float Radius = (DropOffBuilding.DropOffPos == null) ? DropOffBuilding.Radius : 0.0f;
                if (GameManager.MultiplayerGame == false)
                { //in case it's a singleplayer game:
                    MovementManager.Instance.MoveLocal(UnitMvt, DropPos, Radius, DropOffBuilding.gameObject, InputTargetMode.Building);
                }
                else
                {
                    if (GameManager.Instance.IsLocalPlayer(gameObject))
                    { //if local player
                      //send input action to the input manager
                        InputVars NewInputAction = new InputVars();
                        //mode:
                        NewInputAction.SourceMode = (byte)InputSourceMode.Unit;
                        NewInputAction.TargetMode = (byte)InputTargetMode.Resource;

                        NewInputAction.Source = gameObject;

                        NewInputAction.Target = DropOffBuilding.gameObject;
                        
                        NewInputAction.InitialPos = transform.position;
                        NewInputAction.TargetPos = DropPos;

                        InputManager.SendInput(NewInputAction);
                    }
                }
            }
        }

        //drop off resources
        public void DropOffResources ()
		{
			//if it's a single player game or a multiplayer game and this is the local player:
			if (GameManager.MultiplayerGame == false || (GameManager.MultiplayerGame == true && UnitMvt.FactionID == GameManager.PlayerFactionID)) {
				for (int i = 0; i < DropOff.Count; i++) {
					ResourceMgr.AddResource (UnitMvt.FactionID, DropOff [i].Name, DropOff [i].CurrentQuantity); //add the resource collectors:
					DropOff [i].CurrentQuantity = 0;
				}
			}

			//make the unit go back to the resource he's collecting from:
			DroppingOff = false;
			GoingToDropOffBuilding = false;
			GoingBack = true;

            SetTargetResource(TargetResource);
		}
	}
}