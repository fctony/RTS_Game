using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.AI;

/* Unit script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

public enum UnitAnimState { Idle, Building, Collecting, Moving, Attacking, Healing, Converting, TakeDamage, Dead, Revive } //the possible animations states

namespace RTSEngine
{
	public class Unit : MonoBehaviour {

		//Unit's info:
		public string Name; //the name of the unit that will be displayd when it is selected.
		public string Code; //unique code for each unit that is used to identify it in the system.
		public string Category; //the category that this unit belongs to.
		public string Description; //the description of the unit that will be displayed when it is selected.
		public Sprite Icon; //the icon that will be displayed when the unit is selected.

		public bool FreeUnit = false; //does this unit belong to no faction?
		public bool CanBeMoved = true; //does this unit move on orders from the player?
        public bool CanBeConverted = true; //can this be converted?

        [HideInInspector]
		public bool IsInvisible = false; //is the unit invisible?

		//health:
		public float MaxHealth = 100.0f; //maximum health points of the unit
        public float HoverHealthBarY = 2.0f; //this is the height of the hover health bar (in case the feature is enabled).
        [HideInInspector]
		public float Health; //current health of the unit
        public bool TakeDamage = true; //does this unit get affected by attacks?
		[HideInInspector]
		public bool Dead = false; //is the unit dead?
        public int KillerFactionID = -1; //the faction ID of the killer
        public bool DestroyObj = true; //destroy the object on death? 
        public float DestroyObjTime = 2.0f;
		public ResourceManager.Resources[] DestroyAward; //these resources will be awarded to the faction that destroyed this unit

        //Unit death effects:
        public AudioClip DestructionAudio; //audio played when the unit is destroyed.
        public EffectObj DestructionEffect; //the object to create as destruction effect.

        public bool StopMvtOnTakeDamage = false; //stop the movement when taking damage? 
        public bool EnableTakeDamageAnim = false; //enable the take damage animation? 
        public float TakeDamageDuration = 0.2f; //taking the damage duration
        float TakeDamageTimer;

        //Damage over time:
        [HideInInspector]
        public Attack.DoTVars DoT;
        float DoTTimer = 0.0f;

        //Wandering:
        public bool CanWander = false; //can the unit actually wander?
        public bool WanderByDefault = false; //when the unit is created/spawned it starts wandering
        [HideInInspector]
        public bool Wandering = false; //is the unit currently wandering? 
        public bool FixedWanderCenter = true; //wander around spawn position (if wander by default is enabled) or wander around the position where the wandering is enbaled
        [HideInInspector]
        public Vector3 WanderCenter; //center of wandering
        public Vector2 WanderRange = new Vector2(10.0f, 15.0f); //range of wandering 
        public Vector2 WanderReloadRange = new Vector2(2.0f, 4.0f); //time before the unit decides to change the wandering destination
        float WanderTimer;

        //Escaping on attack:
        public bool EscapeOnAttack = false; //if true, the unit will escape to a random position when attacked).
        public Vector2 EscapeRange = new Vector2(20.0f, 40.0f); //the range at which the unit will escape is set here.
        public float EscapeSpeed = 10.0f; //you can also change the speed when escaping
        public AnimatorOverrideController EscapeAnimOverride; //escape animation override to show a running animation instead of the normal one when moving

        //Damage effect:
        public EffectObj DamageEffect; //Created when a damage is received in the contact point between the attack object and this one:

        public GameObject UnitPlane; //The plane where the selection texture appears.
		[HideInInspector]
		//this the timer during which the unit selection texture flahes when it is interacted with.
		public float FlashTime = 0.0f;
		public int FactionID = 0; //Unit's faction ID.

		[HideInInspector]
		public Building CreatedBy = null; //The building that produced the unit.

		[HideInInspector]
		public NavMeshAgent NavAgent; //Nav agent component attached to the unit's object.
		public bool DestinationReached = false; //when the target his destination, this is set to true.
		public float Speed = 10.0f; //The unit's movement speed.
		public float RotationDamping = 2.0f; //How fast does the rotation updates?
        public Vector3 RotationLookAt; //At point will the player be looking at? 
		public bool CanRotate = true; //can the unit rotate? 

		[HideInInspector]
		public bool Moving = false; //Is the player currently moving?
        public InputTargetMode MvtTargetMode; //save the last mvt target mode here.
		public float UnitHeight = 1.0f; // This will always be the position on the y axis for this unit.
		public bool FlyingUnit = false; //is the unit flying or walking on the normal terrain? 
		float MvtCheck; //timer to check whether the unit is moving towards target or not.
		Vector3 LastRegisteredPos; //saves the last player's position to compare it later and see if the unit has actually moved.

        //Worker manager related:
        [HideInInspector]
        public int LastWorkerPosID = -1;

        public GameObject UnitModel; //Drag and drop the unit's model here
		public SelectionObj PlayerSelection; //Must be an object that only include this script, a trigger collider and a kinematic rigidbody.
		//the collider represents the boundaries of the object (building, resource or unit) that can be selected by the player.
		public SkinnedMeshRenderer[] FactionColorObjs; //The child objects of the unit prefab that will get the color of the faction (skinned mesh renderers)
		public MeshRenderer[] FactionColorObjs2; //The child objects of the unit prefab that will get the color of the faction (simple mesh renderers)

		//AI Faction manager
		[HideInInspector]
		public FactionManager FactionMgr;

		//Animations:
		[HideInInspector]
		public Animator AnimMgr; //the animator comp attached to the unit's object
        [HideInInspector]
        public UnitAnimState CurrentAnimState;
		public AnimatorOverrideController AnimOverrideController; //the unit's main controller
        [HideInInspector]
        public bool LockAnimState = false; //When true, it won't be possible to change the anim state using the SetAnimState method.

        public Collider TargetPosColl;

        //APC:
        public APC TargetAPC;

		//Portal:
		public Portal TargetPortal;

		public AudioClip SelectionAudio; //Audio played when the unit has been selected.
		public AudioClip MvtOrderAudio; //Audio played when the unit is ordered to move.
        public AudioClip MvtAudio; //Audio clip played when the unit is moving.
		public AudioClip InvalidMvtPathAudio; //When the movement path is invalid, this audio is played.

        //components:
        [HideInInspector]
		public Builder BuilderMgr;
		[HideInInspector]
		public Attack AttackMgr;
		[HideInInspector]
		public MultipleAttacks MultipleAttacksMgr;
		[HideInInspector]
		public Healer HealMgr;
		[HideInInspector]
		public GatherResource ResourceMgr;
		[HideInInspector]
		public Converter ConvertMgr;
		[HideInInspector]
		public APC APCMgr;
		[HideInInspector]
		public Invisibility InvisibilityMgr;
        [HideInInspector]
        public TaskLauncher TaskMgr;
        [HideInInspector]
        public UnitManager UnitMgr;

        //Scripts:
        [HideInInspector]
		SelectionManager SelectionMgr;
		[HideInInspector]
		public UIManager UIMgr;
		[HideInInspector]
		public GameManager GameMgr;
        [HideInInspector]
        AttackWarningManager AttackWarningMgr;
        [HideInInspector]
        MinimapIconManager MinimapIconMgr;
        [HideInInspector]
        MovementManager MvtMgr;
        TerrainManager TerrainMgr;

        //Double Click:
        bool FirstClick = false;
		float DoubleClickTimer = 0;

		void Awake () {

			GameMgr = GameManager.Instance;
			SelectionMgr = GameMgr.SelectionMgr;
			UIMgr = GameMgr.UIMgr;
            AttackWarningMgr = AttackWarningManager.Instance;
            MinimapIconMgr = MinimapIconManager.Instance;
            UnitMgr = GameMgr.UnitMgr;
            MvtMgr = MovementManager.Instance;
            TerrainMgr = TerrainManager.Instance;

            BuilderMgr = GetComponent <Builder> ();
			HealMgr = GetComponent<Healer> ();
			ConvertMgr = GetComponent<Converter> ();
			APCMgr = GetComponent<APC> ();
			ResourceMgr = GetComponent<GatherResource> ();
			AttackMgr = GetComponent<Attack> ();
			MultipleAttacksMgr = GetComponent<MultipleAttacks> ();
			InvisibilityMgr = GetComponent<Invisibility> ();
            TaskMgr = GetComponent<TaskLauncher>();

            //get the comps that the unit script needs:
            NavAgent = GetComponent<UnityEngine.AI.NavMeshAgent> ();
			if (NavAgent != null) {
				//Set the unit's speed:
				NavAgent.speed = Speed;
				//Set the unit's height:
				NavAgent.baseOffset = UnitHeight;
			}

			//Animations:
			if(AnimMgr == null)
			{
				AnimMgr = GetComponent<Animator> (); //Look if there's an animator component attached in the unit main object:
			}
			if (AnimMgr != null) {//If there is
                if (AnimOverrideController != null) //if there's a default anim override controller, assign it
                {
                    AnimMgr.runtimeAnimatorController = AnimOverrideController;
                }
                else //if there's not, use the runtime one
                {
                    AnimOverrideController = UnitMgr.DefaultAnimController;
                    SetAnimatorOverrideCtrl(AnimOverrideController);
                }
				//Set the current animation state to idle because the unit just spawned!
				SetAnimState (UnitAnimState.Idle);
			}

			Moving = false; //mark as not moving when the unit spawns
			Dead = false; //obviously not dead when the unit just spawend.


			//if there's no unit selection texture, we'll let you know
			if (UnitPlane == null) {
				Debug.LogError ("You must attach a plane object at the bottom of the building and set it to 'UnitPlane' in the inspector.");
			} else {
				UnitPlane.SetActive (false); //hide the selection texture object when the unit just spawned.
			}

			Health = MaxHealth; //initial health:


			//In order for collision detection to work, we must assign these settings to the unit's collider and rigidbody
			if (GetComponent<Rigidbody> () == null) {
				gameObject.AddComponent<Rigidbody> ();
			}
			//unit's rigidbody settings:
			GetComponent<Rigidbody> ().isKinematic = true;
			GetComponent<Rigidbody> ().useGravity = false;

			TargetAPC = null;
			TargetPortal = null;
		}

		void Start ()
		{
			if (LayerMask.NameToLayer ("SelectionPlane") > 0) { //if there's a layer for the selection plane
				UnitPlane.layer = LayerMask.NameToLayer ("SelectionPlane"); //assign this layer because we don't want the main camera showing it
			}

            //set the layer to IgnoreRaycast as we don't want any raycast to recongize this:
            gameObject.layer = 2;

            //set the unit height:
            if (NavAgent != null) {

				NavAgent.angularSpeed = 999.0f; //we handle rotation in the code so no need to set it from the nav agent
				NavAgent.acceleration = 200.0f; //to avoid units sliding when reaching the destination, make sure this is set to a high value
				NavAgent.autoBraking = false;
			}

			//Set the selection object if we're using a different collider for player selection:
			if (PlayerSelection != null) {
				//set the player selection object for this building/resource:
				PlayerSelection.MainObj = this.gameObject;
			} else {
				Debug.LogError("Player selection collider is missing!");
			}

			if (FreeUnit == false) {
				SetUnitColors ();

				FactionMgr = GameMgr.Factions [FactionID].FactionMgr; //get the faction manager that this unit belongs to.

				//Add the newly created unit to the team manager list:
				FactionMgr.AddUnitToLists (this);

                //if this is the local player obj
                if ((GameMgr.IsLocalPlayer(gameObject) || GameManager.MultiplayerGame == false) && CreatedBy != null)
                {
                    //if the new unit does not have a task when spawned, send them to the goto position.
                    CreatedBy.SendUnitToRallyPoint(this);
                }

            } else {
				FactionID = -1;
			}

            //if there's a task manager:
            if (TaskMgr)
            {
                TaskMgr.OnTasksInit();

                //update the tasks to the current upgrade level:
                TaskMgr.SyncTaskUpgradeLevel();

                //TO BE CHANGED.

                /*//If the building belongs to a NPC player then we'll check for resources:
                if (FactionID != GameManager.PlayerFactionID && GameManager.MultiplayerGame == false)
                {
                    FactionMgr.ResourceMgr.AllUnitsUpgraded = false;

                    //if the faction is NPC, alert the NPC Army manager that a new task launcher has been added, possibility of creating army units or units that the NPC spawner need:
                    if (FactionID != GameManager.PlayerFactionID)
                    {
                        //inform the NPC army:
                        FactionMgr.ArmyMgr.ReloadArmyUnitsPriority(TaskMgr, true);
                        //inform the NPC unit spawner:
                        FactionMgr.UnitSpawner.ReloadTaskLauncherLists(TaskMgr, true);
                    }
                }*/
            }

            //Wandering:
            //If this is a singleplayer game or a multiplayer game and this is the local player
            if (GameManager.MultiplayerGame == false || GameMgr.IsLocalPlayer(gameObject) == true)
            {
                //is the unit wandering by default? 
                if (WanderByDefault == true && CanWander == true)
                {
                    //The unit is now wandering:
                    Wandering = true;
                    //set the wandering center:
                    if (FixedWanderCenter == true)
                    { //if the unit wanders around the spawn pos
                        WanderCenter = transform.position; //set the wander center.
                    }
                    Wander();
                }
            }

            //call the custom event below:
            if (GameMgr.Events)
				GameMgr.Events.OnUnitCreated (this);

            //release the target position collider:
            if(TargetPosColl)
                TargetPosColl.transform.SetParent(null, true);

        }

		public void SetUnitColors ()
		{
			//Set the faction color objects:
			//If there's actually objects to color in this prefab:

			//for skinned mesh renderers
			if (FactionColorObjs.Length > 0) {
				//Loop through the faction color objects (the array is actually a MeshRenderer array because we want to allow only objects that include mesh renderers in this prefab):
				for (int i = 0; i < FactionColorObjs.Length; i++) {
					//Always checking if the object/material is not invalid:
					if (FactionColorObjs [i] != null) {
						//Color the object to the faction's color:
						FactionColorObjs [i].material.color = GameMgr.Factions [FactionID].FactionColor;
					}
				}
			}

			//for simple mesh renderers
			if (FactionColorObjs2.Length > 0) {
				//Loop through the faction color objects (the array is actually a MeshRenderer array because we want to allow only objects that include mesh renderers in this prefab):
				for (int i = 0; i < FactionColorObjs2.Length; i++) {
					//Always checking if the object/material is not invalid:
					if (FactionColorObjs2 [i] != null) {
						//Color the object to the faction's color:
						FactionColorObjs2 [i].material.color = GameMgr.Factions [FactionID].FactionColor;
					}
				}
			}
		}

        //Reset all DoT settings
        public void ResetDoT()
        {
            DoT.Enabled = false;
            DoT.Damage = 0.0f;
            DoT.Infinite = false;
            DoT.Duration = 0.0f;
            DoT.Cycle = 0.0f;
            DoTTimer = 0.0f;
        }

        //a method to make the unit wander:
        public void Wander ()
        {
            //if the unit's wander position is not fixed as the spawn position
            if (FixedWanderCenter == false)
            {
                WanderCenter = transform.position; //set the wander center each time then
            }
            //move the unit to a random position in the wander range
            Vector3 WanderPos = GetRandomPos(WanderCenter, Random.Range(WanderRange.x, WanderRange.y));

            MvtMgr.Move(this, WanderPos, 0.0f, null, InputTargetMode.None); //move the unit

            //reload the wander timer.
            WanderTimer = Random.Range(WanderReloadRange.x, WanderReloadRange.y);
        }

        //Escape:
        public void Escape (float Range)
        {
            Vector3 EscapePos = GetRandomPos(transform.position, Range); //find a random position to run away.

            //make sure that the chosen escape position is valid:
            if (MvtMgr.IsDestinationClear(EscapePos, this, out EscapePos))
            {
                //if this is an online game pass it 
                if (GameManager.MultiplayerGame == false)
                {
                    EscapeLocal(EscapePos);
                }
                else
                {
                    if (GameMgr.IsLocalPlayer(gameObject) == true)
                    {
                        //send input action to the input manager
                        InputVars NewInputAction = new InputVars();
                        //mode:
                        NewInputAction.SourceMode = (byte)InputSourceMode.CustomCommand;
                        NewInputAction.TargetMode = (byte)InputCustomMode.UnitEscape;

                        //source
                        NewInputAction.Source = gameObject;
                        NewInputAction.InitialPos = transform.position;
                        NewInputAction.TargetPos = EscapePos;

                        //sent input
                        InputManager.SendInput(NewInputAction);
                    }
                }
            }
        }

        //a method to make the unit escape locally
        public void EscapeLocal (Vector3 TargetPos)
        {
            MvtMgr.Move(this, TargetPos, 0.0f, null, InputTargetMode.None); //make the unit escape
            NavAgent.speed = EscapeSpeed; //set the escape speed

            //set the escape animator override:
            if (EscapeAnimOverride != null)
            {
                SetAnimatorOverrideCtrl(EscapeAnimOverride);
            }
        }

        //a method that picks a random position starting from on origina point and inside a certain range
        public Vector3 GetRandomPos(Vector3 Origin, float Range)
        {
            //pick a random direction to go to
            Vector3 RandomDirection = Random.insideUnitSphere * Range;
            RandomDirection += Origin;

            //this will hold the random chosen position, initiated as the unit's current position so that no valid pos is found, unit won't move.
            Vector3 Result = transform.position;

            //get the closet walkable point to the random chosen direction
            NavMeshHit Hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(RandomDirection, out Hit, Range, NavAgent.areaMask))
            {
                Result = Hit.position;
            }

            return Result;
        }

        void Update () 
		{
            //if this is the local player
            if (GameManager.MultiplayerGame == false || GameMgr.IsLocalPlayer(gameObject) == true)
            {
                if (Dead == false)
                {
                    //Damage over time:
                    //if the DoT effect is enabled
                    if (DoT.Enabled == true)
                    {
                        if (DoT.Infinite == false) //if this is no infinite DoT
                        {
                            //DoT duration timer:
                            if (DoT.Duration > 0)
                            {
                                DoT.Duration -= Time.deltaTime;
                            }
                            //duration ended:
                            else
                            {
                                ResetDoT();
                            }
                        }

                        //DoT Cycle Timer:
                        if (DoTTimer > 0)
                        {
                            DoTTimer -= Time.deltaTime;
                        }
                        else //cycle ends
                        {
                            //deal damage:
                            AddHealth(-DoT.Damage, DoT.Source);
                            //start new cycle
                            DoTTimer = DoT.Cycle;
                        }
                    }

                    //Wandering:
                    //only if the player is idle and can wander + is not currently playing the take damage animation
                    if (IsIdle() == true && Wandering == true && (EnableTakeDamageAnim == false || TakeDamageTimer <= 0))
                    {
                        //as long as the wander timer is running, do nothing
                        if (WanderTimer > 0)
                        {
                            WanderTimer -= Time.deltaTime;
                        }
                        if (WanderTimer <= 0) //when the wander timer is over
                        {
                            Wander();
                        }
                    }

                    if (EnableTakeDamageAnim == true)
                    { //if the take damage animation can be played
                        if (TakeDamageTimer > 0)
                        {
                            TakeDamageTimer -= Time.deltaTime;
                        }
                        if (TakeDamageTimer < 0)
                        {
                            TakeDamageTimer = 0.0f;
                            //if the unit escapes when getting attacked
                            if (EscapeOnAttack == true)
                            {
                                Escape(Random.Range(EscapeRange.x, EscapeRange.y)); //escape
                            }
                        }
                    }
                }
            }

            //Double click timer:
            if (DoubleClickTimer > 0) {
				DoubleClickTimer -= Time.deltaTime;
			} else {
				DoubleClickTimer = 0.0f;
				FirstClick = false;
			}
			//Selection flash timer:
			if (FlashTime > 0) {
				FlashTime -= Time.deltaTime;
			}
			if (FlashTime < 0) {
				FlashTime = 0.0f;
				CancelInvoke ("SelectionFlash");
				UnitPlane.gameObject.SetActive (false);
			}

            if (Dead == false) //if the unit isn't dead yet
            {
                if (CanRotate == true && Moving == false)
                {
                    //Unit rotation:
                    Vector3 LookAt = RotationLookAt - transform.position;

                    //if the unit has an attack target
                    if (AttackMgr != null)
                    {
                        if (AttackMgr.AttackTarget != null)
                        {
                            //make the unit look at its target
                            LookAt = AttackMgr.AttackTarget.transform.position - transform.position;
                        }
                    }
                    if (BuilderMgr != null)
                    {
                        //if the unit has a target building to construct
                        if (BuilderMgr.TargetBuilding != null)
                        {
                            //make it look at that building
                            LookAt = BuilderMgr.TargetBuilding.transform.position - transform.position;
                        }
                    }
                    if (HealMgr != null)
                    {
                        //if the unit has a target unit to heal
                        if (HealMgr.TargetUnit != null)
                        {
                            //make it look at that unit
                            LookAt = HealMgr.TargetUnit.transform.position - transform.position;
                        }
                    }
                    if (ConvertMgr != null)
                    {
                        //if the unit has a target unit to convert
                        if (ConvertMgr.TargetUnit != null)
                        {
                            //make it look at that unit
                            LookAt = ConvertMgr.TargetUnit.transform.position - transform.position;
                        }
                    }
                    if (ResourceMgr != null)
                    {
                        //depending on the resource collector goal:
                        if (ResourceMgr.TargetResource != null)
                        {
                            //make it look at the dropping off building or the resource:
                            if (ResourceMgr.DroppingOff == false || (ResourceMgr.DroppingOff == true && ResourceMgr.DropOffBuilding == null))
                            {
                                LookAt = ResourceMgr.TargetResource.transform.position - transform.position;
                            }
                            else
                            {
                                if (ResourceMgr.DropOffBuilding != null)
                                {
                                    LookAt = ResourceMgr.DropOffBuilding.transform.position - transform.position;
                                }
                            }
                        }
                    }

                    LookAt.y = 0;
                    if (LookAt != Vector3.zero)
                    {
                        Quaternion NewRot = Quaternion.LookRotation(LookAt);
                        transform.rotation = Quaternion.Slerp(transform.rotation, NewRot, Time.deltaTime * RotationDamping);
                    }
                }

                if (Moving == true)
                { //Is the unit currently moving?
                    if (NavAgent.path == null)
                    { //if there's no valid path
                        StopMvt(); //stop moving
                    }
                    else
                    {
                        //movement check timer:
                        if (MvtCheck > 0)
                        {
                            MvtCheck -= Time.deltaTime;
                        }
                        if (MvtCheck < 0)
                        {
                            //TBC
                            if (Vector3.Distance(transform.position, LastRegisteredPos) <= 0.1f)
                            { //if the time passed and we still in the same position (unit is stuck) then stop the mvt
                                //stop moving:
                                StopMvt();
                                //cancel all unit jobs:
                                CancelAttack();
                                CancelCollecting();
                                CancelHealing();
                                CancelConverting();
                                CancelBuilding();
                            }
                            else
                            {
                                MvtCheck = 2.0f; //launch the timer again
                                LastRegisteredPos = transform.position; //set this is as the last registered position
                            }
                        }

                        //if the unit hasn't reached the destination yet
                        if (DestinationReached == false)
                        {
                            //if this unit is moving to attack:
                            DestinationReached = Vector3.Distance(GetUnitGroundPos(), NavAgent.destination) <= NavAgent.stoppingDistance;
                        }
                    }

                    //if the unit has reachecd its destination
                    if (DestinationReached == true)
                    {
                        if (TargetAPC != null) //if there's a target APC
                        {
                            //check if there's space for this unit
                            if (TargetAPC.CurrentUnits.Count < TargetAPC.MaxAmount)
                            {
                                TargetAPC.AddUnit(this); //get in the APC
                            }
                        }
                        else if (TargetPortal != null) //if there's a target portal
                        {
                            //go through the portal
                            TargetPortal.Teleport(this);
                        }
                        StopMvt(); //stop the unit mvt
                    }
                }
			}
		}

		//Flashing building selection (when the player sends units to contruct a building, its texture flashes for some time):
		public void SelectionFlash ()
		{
			UnitPlane.gameObject.SetActive (!UnitPlane.activeInHierarchy);
		}

		//method to select the unit:
		public void SelectUnit ()
		{
			//If the selection key is down, then we will add this unit to the current selection, if not we will deselect the selected units then select this one.
			//Make sure we are not clicking on a UI object:
			if (!EventSystem.current.IsPointerOverGameObject ()) {
				if (BuildingPlacement.IsBuilding == false) {

					if (FirstClick == true && FactionID == GameManager.PlayerFactionID) {
						SelectionMgr.SelectUnitsInRange (this);
					} else {
						FlashTime = 0.0f;
						CancelInvoke ("SelectionFlash");

						SelectionMgr.SelectUnit (this, SelectionMgr.MultipleSelectionKeyDown);

						if (SelectionMgr.MultipleSelectionKeyDown == false) {
							FirstClick = true;
							DoubleClickTimer = 0.5f;
						}
					}
				}
			}
		}

		//a method that stops the player movement.
		public void StopMvt ()
		{
            if (Dead == true && DestroyObj == true) //if unit is dead and object is destroyed don't mess with other components
                return;

            Moving = false; //If the movement path is somehow unvalid, stop moving.

			if (gameObject.activeInHierarchy == true) {
                if (NavAgent != null) NavAgent.isStopped = true; //stop the nav agent comp

				//Inform the animator that the unit stopped moving:
				SetAnimState (UnitAnimState.Idle);
			}
			StopAllCoroutines ();

            if (AnimMgr.runtimeAnimatorController != AnimOverrideController)
            {
                //Assign the default animator controller:
                SetAnimatorOverrideCtrl(AnimOverrideController);
            }

            //Stop the movement audio clip.
            AudioManager.StopAudio(gameObject);

            //when unit is not moving, set as most important in obstacle avoidance:
            NavAgent.avoidancePriority = 0;

			TargetPortal = null;
			TargetAPC = null;

            if (TargetPosColl)
                TargetPosColl.transform.position = GetUnitGroundPos();
		}

        Vector3 GetUnitGroundPos ()
        {
            return new Vector3(transform.position.x, TerrainMgr.SampleHeight(transform.position), transform.position.z);
        }

        public void CheckUnitPathLocal (Vector3 TargetPos, GameObject TargetObj, Vector3 LookAt, float StoppingDistance, InputTargetMode TargetMode)
		{
            if(InputTargetMode.Portal == TargetMode)
            {
                TargetPortal = TargetObj.GetComponent<Portal>();
            }

            NavAgent.speed = Speed; //set the movement speed in case it was changed by the escape behavior

            NavAgent.SetDestination(TargetPos); //calculate the path here
            NavAgent.isStopped = true; //allow for the path to be calculated as it takes sometimes a couple of frames

            OnUnitPathComplete (TargetPos, TargetObj, LookAt, StoppingDistance, TargetMode); //see if the path is valid and proceed with the movement
		}

		//This callback will inform us if there's a possible path to the target position:
		void OnUnitPathComplete(Vector3 TargetPos, GameObject TargetObj, Vector3 LookAt, float StoppingDistance, InputTargetMode TargetMode)
        {
			bool ValidMvt = false; //check if the movement is valid:
			if (NavAgent.path != null) { //if it's a nav mesh mvt:
				if (NavAgent.path.status != UnityEngine.AI.NavMeshPathStatus.PathInvalid) {
					ValidMvt = true;
				}
			}
			if (ValidMvt == true) { //if the mvt is valid
                //when the unit is moving, set its obstacle avoidance type to default:
                NavAgent.avoidancePriority = 50;

                //to check if the unit is stuck or not, register the unit's position when the mvt starts and start the check timer
                MvtCheck = 2.0f;
                LastRegisteredPos = transform.position;

                //Set the roation look at position:
                RotationLookAt = LookAt;

                NavAgent.stoppingDistance = StoppingDistance; //default movement stopping distance.

                NavAgent.isStopped = false; //allow the unit to move.
                //If there's no problem with the current path (meaning there's no obstacles in the way):
                //Then we'll start moving the unit there:
                Moving = true;

                DestinationReached = false;

                if (TargetPosColl)
                    TargetPosColl.transform.position = NavAgent.destination;

                //if the unit is already moving then make sure to lock the animations:
                if(CurrentAnimState == UnitAnimState.Moving)
                    LockAnimState = true;

                if (TargetObj != null) {
                    if (TargetObj.GetComponent<Building>())
                    { //if the target object is a building
                        if (TargetObj.GetComponent<Building>().enabled)
                        { //enabled?
                            if (TargetObj.GetComponent<Building>().FactionID == FactionID)
                            { //if it belongs to the same faction and the unit can build
                              //then we're constructing it or dropping resources at it
                                bool IsDroppingOff = false;
                                if (ResourceMgr)
                                {
                                    if (ResourceMgr.DroppingOff == true)
                                    {
                                        IsDroppingOff = true;
                                        TargetAPC = null; TargetPortal = null;
                                        ResourceMgr.GoingToDropOffBuilding = true; //marking the player as going to the drop off building
                                    }
                                }
                                if (IsDroppingOff == false)
                                    CancelCollecting();

                                CancelAttack();
                                CancelHealing();
                                CancelConverting();

                                bool Constructing = false;

                                if (BuilderMgr)
                                {
                                    if (TargetObj.GetComponent<Building>() == BuilderMgr.TargetBuilding)
                                    { //if the target building is the one the unit's going to construct not just go to
                                        Constructing = true;
                                        TargetAPC = null; TargetPortal = null;
                                    }
                                }

                                if (Constructing == false)
                                {
                                    CancelBuilding();
                                }
                            }
                            else
                            {
                                if (TargetObj.GetComponent<Portal>())
                                { //if the target building is a portal:
                                    CancelCollecting();
                                    CancelBuilding();
                                    CancelHealing();
                                    CancelConverting();
                                    TargetAPC = null;

                                    TargetPortal = TargetObj.GetComponent<Portal>();
                                }
                                else if (AttackMgr != null)
                                { //if it does not belong to the same faction and the unit can attack, then we're attacking it
                                    if (AttackMgr.AttackTarget == TargetObj)
                                    {
                                        CancelCollecting();
                                        CancelBuilding();
                                        CancelHealing();
                                        CancelConverting();
                                        TargetAPC = null;
                                        TargetPortal = null;
                                    }
                                }
                            }
                        }                       
                    }
                    else if (ResourceMgr && TargetObj.GetComponent<Resource>())
                    { //if the target object is a resource
                        if (TargetObj.GetComponent<Resource>().enabled)
                        {
                            if (TargetObj.GetComponent<Resource>() == ResourceMgr.TargetResource)
                            { //if the unit's collecting a resource
                                CancelBuilding();
                                CancelHealing();
                                CancelAttack();
                                CancelConverting();
                                TargetAPC = null; TargetPortal = null;
                            }
                        }
                    }
                    else if (TargetObj.GetComponent<Unit>())
                    { //if the target object is a unit
                        if (TargetObj.GetComponent<Unit>().enabled)
                        {
                            if (TargetObj.GetComponent<Unit>().FactionID == FactionID)
                            { //if the unit belongs to the same faction:
                                if (TargetObj.GetComponent<APC>())
                                { //APC
                                  //then move to the APC:
                                    TargetAPC = TargetObj.GetComponent<APC>();
                                }
                                else if (HealMgr != null)
                                { //else if the unit has a healer component
                                    if (HealMgr.TargetUnit.gameObject == TargetObj)
                                    {
                                        CancelCollecting();
                                        CancelBuilding();
                                        CancelAttack();
                                        CancelConverting();
                                        TargetAPC = null; TargetPortal = null;
                                    }
                                }
                            }
                            else
                            {
                                if (AttackMgr)
                                { //if the unit does not belong to the same faction and the unit has an attack manager:
                                    if (AttackMgr.AttackTarget == TargetObj)
                                    {
                                        CancelCollecting();
                                        CancelBuilding();
                                        CancelHealing();
                                        CancelConverting();
                                        TargetAPC = null; TargetPortal = null;
                                    }
                                }
                                else if (ConvertMgr)
                                {
                                    if (ConvertMgr.TargetUnit != null && TargetObj != null)
                                    {
                                        if (ConvertMgr.TargetUnit.gameObject == TargetObj)
                                        {
                                            CancelCollecting();
                                            CancelBuilding();
                                            CancelHealing();
                                            CancelAttack();
                                            TargetAPC = null;
                                            TargetPortal = null;

                                            //custom event:
                                            if (GameMgr.Events)
                                                GameMgr.Events.OnUnitStartConverting(this, ConvertMgr.TargetUnit);
                                        }
                                    }
                                }
                            }
                        }
                    }
				}
				else {

					TargetAPC = null; TargetPortal = null;
					CancelBuilding();
					CancelHealing ();
					CancelAttack ();
					CancelCollecting ();
					CancelConverting ();
				}

                //Unlock animation state:
                LockAnimState = false;

                //Inform the animator that the unit is currently moving:
                SetAnimState(UnitAnimState.Moving);

                //Play the movement audio clip.
                AudioManager.PlayAudio(gameObject, MvtAudio, true);
            } 
			else {
				StopMvt ();

				if (TargetObj != null) {
					if (BuilderMgr && TargetObj.GetComponent<Building> ()) {
						if (TargetObj.GetComponent<Building> () == BuilderMgr.TargetBuilding) { //if the target building is the one the unit's going to construct not just go to
							CancelBuilding ();
						}
					} else if (ResourceMgr && TargetObj.GetComponent<Resource> ()) {
						if (TargetObj.GetComponent<Resource> () == ResourceMgr.TargetResource) { //if the unit's collecting a resource
							CancelCollecting ();
						}
					} else if (AttackMgr) {
						if (AttackMgr.AttackTarget == TargetObj) {
							CancelAttack ();
						}
					}
					else if (HealMgr) {
						if (HealMgr.TargetUnit.gameObject == TargetObj) {
							CancelHealing ();
						}
					}
					else if (ConvertMgr) {
						if (ConvertMgr.TargetUnit.gameObject == TargetObj) {
							CancelConverting ();
						}
					}
				}

				//If it's the local player:
				if (GameManager.PlayerFactionID == FactionID) {
					//Play the invalid movement path sound:
					AudioManager.PlayAudio (GameMgr.GeneralAudioSource.gameObject, InvalidMvtPathAudio, false);
				}
			}
		}

		//a method that stops the unit from building
		public void CancelBuilding ()
		{
			//If the player was supposed to be constructing a building:
			if (BuilderMgr) {
				if (BuilderMgr.TargetBuilding != null) {

					//custom event:
					if(GameMgr.Events) GameMgr.Events.OnUnitStopBuilding(this,BuilderMgr.TargetBuilding);

                    //Stop building:
                    BuilderMgr.TargetBuilding.WorkerMgr.RemoveWorker(this);

					//Hide the builder object:
					if (BuilderMgr.BuilderObj != null) {
						BuilderMgr.BuilderObj.SetActive (false);
					}

					if (SelectionMgr.SelectedBuilding == BuilderMgr.TargetBuilding) {
						SelectionMgr.UIMgr.UpdateBuildingUI (SelectionMgr.SelectedBuilding);
					}

					BuilderMgr.TargetBuilding = null;
					BuilderMgr.IsBuilding = false;

					//Inform the animator that we're returning to the idle state as we stopped this action:
					SetAnimState(UnitAnimState.Idle);

					AudioManager.StopAudio (gameObject);
				}
			}
		}

		//a method that stops the unit from healing
		public void CancelHealing ()
		{
			//If the player was supposed to be healing:
			if (HealMgr) {
				if (HealMgr.TargetUnit != null) {

					//custom event:
					if(GameMgr.Events) GameMgr.Events.OnUnitStopHealing(this,HealMgr.TargetUnit);

					HealMgr.TargetUnit = null;
					HealMgr.IsHealing = false;

					//Inform the animator that we're returning to the idle state as we stopped this action:
					SetAnimState(UnitAnimState.Idle);

					AudioManager.StopAudio (gameObject);
				}
			}
		}

		//a method that stops the unit from converting
		public void CancelConverting ()
		{
			//If the player was supposed to be healing:
			if (ConvertMgr) {
				if (ConvertMgr.TargetUnit != null) {

					//custom event:
					if(GameMgr.Events) GameMgr.Events.OnUnitStopHealing(this,ConvertMgr.TargetUnit);

					ConvertMgr.TargetUnit = null;
					ConvertMgr.IsConverting = false;

					//Inform the animator that we're returning to the idle state as we stopped this action:
					SetAnimState(UnitAnimState.Idle);

					AudioManager.StopAudio (gameObject);
				}
			}
		}

		//stop the unit from collectng a resource
		public void CancelCollecting ()
		{
            //Cancel collectin resources when the unit moves away:
            if (ResourceMgr) {
                if (ResourceMgr.TargetResource != null) {
					if(AnimOverrideController != null && AnimMgr != null) AnimMgr.runtimeAnimatorController = AnimOverrideController; //set the main animation controller.

					//custom event:
					if(GameMgr.Events) GameMgr.Events.OnUnitStopCollecting(this,ResourceMgr.TargetResource);

					//hide the collection object and the drop off object:
					if (ResourceMgr.CurrentCollectionObj != null) {
						ResourceMgr.CurrentCollectionObj.SetActive (false);
					}
					ResourceMgr.CurrentCollectionObj = null;
					if (ResourceMgr.DropOffObj != null) {
						ResourceMgr.DropOffObj.SetActive (false);
					}

                    //Stop collecting:
                    ResourceMgr.TargetResource.WorkerMgr.RemoveWorker(this);

                    if (SelectionMgr.SelectedResource == ResourceMgr.TargetResource) {
						SelectionMgr.UIMgr.UpdateResourceUI (SelectionMgr.SelectedResource);
					}

					ResourceMgr.TargetResource = null;
					ResourceMgr.IsCollecting = false;
					ResourceMgr.DroppingOff = false;

					//set drop off building to null
					ResourceMgr.DropOffBuilding = null;

					//Inform the animator that we're returning to the idle state as we stopped this action:
					SetAnimState(UnitAnimState.Idle);

					AudioManager.StopAudio (gameObject);
				}
			}
		}

		//stop the unit from attacking.
		public void CancelAttack ()
		{
			if (AttackMgr != null) {
				AttackMgr.AttackTarget = null;
				AttackMgr.AttackStep = 0;
				AttackMgr.AttackStepTimer = 0.0f;

                //Inform the animator that we're returning to the idle state as we stopped this action:
                SetAnimState(UnitAnimState.Idle);
			}
		}

        public void AddHealth(float Value, GameObject Source)
        {
            if (GameManager.MultiplayerGame == false)
            {
                AddHealthLocal(Value, Source);
            }
            else
            {
                if (GameMgr.IsLocalPlayer(gameObject))
                {
                    //Sync the building's health with everyone in the network

                    //send input action to the input manager
                    InputVars NewInputAction = new InputVars();
                    //mode:
                    NewInputAction.SourceMode = (byte)InputSourceMode.Unit;
                    NewInputAction.TargetMode = (byte)InputTargetMode.Self;

                    NewInputAction.Source = gameObject;
                    NewInputAction.Target = Source;

                    NewInputAction.InitialPos = transform.position;
                    NewInputAction.Value = (int)Value;

                    InputManager.SendInput(NewInputAction);
                }
            }
        }

        //Health:
        public void AddHealthLocal (float HealthPoints, GameObject Source)
		{
            //if the unit doesn't take damage and the health points to add is negative (damage):
            if (TakeDamage == false && HealthPoints < 0.0f)
                return; //don't proceed.

            Health += HealthPoints;
            if (Health > MaxHealth)
            {
                Health = MaxHealth;
            }

            //Custom events:
            if (GameMgr.Events)
                GameMgr.Events.OnUnitHealthUpdated(this, HealthPoints, Source);

            //if the mouse is over this unit
            if (UIMgr.IsHoverSource(PlayerSelection))
            {
                //update hover health bar:
                UIMgr.UpdateHoverHealthBar(Health, MaxHealth);
            }

            if (Health <= 0.0f) {

				Health = 0.0f;
				if (Dead == false) {

                    if (Source != null)
                    {
                        //get the source faction ID:
                        int SourceFactionID = -1;

                        if (Source.gameObject.GetComponent<Unit>())
                        {
                            SourceFactionID = Source.gameObject.GetComponent<Unit>().FactionID;
                        }
                        else if (Source.gameObject.GetComponent<Building>())
                        {
                            SourceFactionID = Source.gameObject.GetComponent<Building>().FactionID;
                        }

                        //award the destroy award to the source:
                        if (DestroyAward.Length > 0)
                        {
                            //if the source is not the same faction ID:
                            if (SourceFactionID != FactionID)
                            {
                                for (int i = 0; i < DestroyAward.Length; i++)
                                {
                                    //award destroy resources to source:
                                    GameMgr.ResourceMgr.AddResource(SourceFactionID, DestroyAward[i].Name, DestroyAward[i].Amount);
                                }
                            }
                        }

                        KillerFactionID = SourceFactionID;
                    }

                    //destroy the building
                    DestroyUnit ();
					Dead = true;
				}
			}

			else { 
				//Apply the damage animation:
				if (HealthPoints < 0) {
					SetAnimState (UnitAnimState.TakeDamage);
                    if (FactionID == GameManager.PlayerFactionID)
                    { //local player:
                      //show warning in mini map to let player know that he is getting attacked.
                        AttackWarningMgr.AddAttackWarning(this.gameObject);
                    }
                }

                //Update health UI:
                //Checking if the unit that has just received damage is currently selected:
                if (SelectionMgr.SelectedUnits.Contains(this) == this)
                {
                    //Update the health UI:
                    UIMgr.UpdateUnitHealthUI(SelectionMgr.SelectedUnits[0]);
                }

                if (HealthPoints < 0)
                {

                    if (StopMvtOnTakeDamage == true)
                    { //stop mvt on take damage?
                        StopMvt();
                    }
                    if (EnableTakeDamageAnim == true)
                    { //if the take damage animation can be played
                        TakeDamageTimer = TakeDamageDuration; //start the timer
                    }

                    if (Source != null) //if the attack source is known
                    {
                        if (GameManager.MultiplayerGame == false || GameMgr.IsLocalPlayer(gameObject) == true)
                        { //if this is an offline game or online but this is the local player
                            //can the unit actually defend himself? 
                            if (AttackMgr != null)
                            {
                                //if the unit can attack when it is attacked (and make sure it is idle if it's a local player unit).
                                if (AttackMgr.AttackWhenAttacked == true && ((FactionID == GameManager.PlayerFactionID && IsIdle() == true) || FactionID != GameManager.PlayerFactionID))
                                {
                                    //attack back if the unit does not have a target already:
                                    if (AttackMgr.AttackTarget == null)
                                    {
                                        MvtMgr.LaunchAttackLocal(this, Source.gameObject, MovementManager.AttackModes.None);
                                    }
                                }
                            }

                            if (EnableTakeDamageAnim == false && CanBeMoved == true)
                            {  //if there's no take damage anim then check the escape behavior directly
                                if (EscapeOnAttack == true)
                                { //if the unit escapes when attacked.
                                    Escape(Random.Range(EscapeRange.x, EscapeRange.y));
                                }
                            }
                        }

                        /*//offline game only:
                        if (GameManager.MultiplayerGame == false)
                        {
                            //Check if the unit belongs to an AI player:  
                            if (FactionID != GameManager.PlayerFactionID && FreeUnit == false)
                            {
                                //We'll search for the nearest building center from the attacking unit:
                                Building Center = FactionMgr.BuildingMgr.GetNearestBuilding(transform.position, FactionMgr.BuildingMgr.CapitalBuilding.Code);
                                //if there's a center building:
                                if (Center != null)
                                {
                                    //If the attacked unit is outside the faction's border.
                                    if (Vector3.Distance(Center.transform.position, transform.position) > Center.GetComponent<Border>().Size)
                                    {
                                        //Ask for support:
                                        Collider[] UnitsInRange = Physics.OverlapSphere(transform.position, FactionMgr.ArmyMgr.AttackingSupportRange, MvtMgr.GroundUnitLayerMask);
                                        List<Unit> FriendlyUnits = new List<Unit>(); //stores the unit that will support

                                        //Search for the army units in range of the damaged unit:
                                        if (UnitsInRange.Length > 0)
                                        {
                                            foreach (Collider Coll in UnitsInRange)
                                            {
                                                //If the object is a unit that belongs to the same faction
                                                Unit FriendlyUnit = Coll.gameObject.GetComponent<Unit>();
                                                if (FriendlyUnit)
                                                {
                                                    if (FriendlyUnit.AttackMgr && FriendlyUnit.FactionID == FactionID)
                                                    {
                                                        //consider the friendly unit only if it doesn't have a target already
                                                        if (FriendlyUnit.AttackMgr.AttackTarget == null)
                                                        {
                                                            FriendlyUnits.Add(FriendlyUnit);
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        //if there are units ready to defend/support
                                        if(FriendlyUnits.Count > 0)
                                        {
                                            //launch the attack
                                            MvtMgr.LaunchAttack(FriendlyUnits, Source, MovementManager.AttackModes.None);
                                        }
                                    }
                                    else
                                    {
                                        //If the unit is inside the faction's borders:

                                        int SourceFactionID = -1;
                                        if (Source.GetComponent<Attack>() && Source.GetComponent<Unit>())
                                        {
                                            //If the source of the attack is a unit.
                                            SourceFactionID = Source.GetComponent<Unit>().FactionID;
                                        }
                                        if (Center != null && SourceFactionID != -1)
                                        {
                                            FactionMgr.ArmyMgr.UnderAttack = true;
                                            FactionMgr.ArmyMgr.CheckArmyTimer = -1.0f;
                                            FactionMgr.ArmyMgr.SetDefenseCenter(Center, SourceFactionID);
                                        }
                                    }
                                }
                            }
                        }*/
                    }
                }
			}
		}

        public void DestroyUnit()
        {
            if (GameManager.MultiplayerGame == false)
            {
                DestroyUnitLocal();
            }
            else
            {
                if (GameMgr.IsLocalPlayer(gameObject))
                {
                    //destroy the building
                    //send input action to the input manager
                    InputVars NewInputAction = new InputVars();
                    //mode:
                    NewInputAction.SourceMode = (byte)InputSourceMode.Destroy;

                    NewInputAction.Source = gameObject;

                    InputManager.SendInput(NewInputAction);
                }
            }
        }

        public void DestroyUnitLocal ()
		{
			if ((GameManager.PlayerFactionID == FactionID && GameManager.MultiplayerGame == true) || GameManager.MultiplayerGame == false) {
				RemoveFromFaction ();

				//If this unit was selected, hide the selection menu:
				if (SelectionMgr.SelectedUnits.Contains (this)) {
					SelectionMgr.DeselectUnit (this);
					if (SelectionMgr.SelectedUnits.Count == 0) {
						UIMgr.HideTaskButtons ();
						UIMgr.HideSelectionInfoPanel ();
                        UIMgr.HideTooltip();
					}
				}
			}

            //destroy the target collider position:
            TargetPosColl.gameObject.SetActive(false);

            //if the mouse is over this unit
            if (UIMgr.IsHoverSource(PlayerSelection))
            {
                //stop displaying the hover health bar
                UIMgr.TriggerHoverHealthBar(false, PlayerSelection, 0.0f);
            }

            //Inform the animator that the unit died:
            SetAnimState (UnitAnimState.Dead);

            //Stop DoT if there's any:
            ResetDoT();

            //unit death:
            Dead = true;
			Health = 0.0f;

            StopMvt();

            CancelAttack ();
			CancelBuilding ();
			CancelCollecting ();
			CancelHealing ();
			CancelConverting ();

            //remove the minimap icon:
            MinimapIconMgr.RemoveMinimapIcon(PlayerSelection);

			if(GameMgr.Events) GameMgr.Events.OnUnitDead (this);

            //remove components to avoid interacting with the unit:
            if (BuilderMgr)
                BuilderMgr.enabled = false;
            if (AttackMgr)
                AttackMgr.enabled = false;
            if (ResourceMgr)
                ResourceMgr.enabled = false;
            if (APCMgr)
                APCMgr.enabled = false;
            if (HealMgr)
                HealMgr.enabled = false;
            if (ConvertMgr)
                ConvertMgr.enabled = false;

            //Spawn the destruction effect obj if it exists:
            if (DestructionEffect != null)
            {
                //get the destruction effect from the pool
                GameObject NewDestructionEffect = EffectObjPool.Instance.GetEffectObj(EffectObjPool.EffectObjTypes.BuildingDamageEffect, DestructionEffect);
                //set the life time of the destruction effect
                NewDestructionEffect.gameObject.GetComponent<EffectObj>().Timer = NewDestructionEffect.gameObject.GetComponent<EffectObj>().LifeTime;
                //set its position
                NewDestructionEffect.transform.position = transform.position;
                //and activate it:
                NewDestructionEffect.SetActive(true);

                //Building destruction sound effect:
                if (DestructionAudio != null)
                {
                    //Check if the destruction effect object has an audio source:
                    if (NewDestructionEffect.GetComponent<AudioSource>() != null)
                    {
                        //Play the destruction audio:
                        AudioManager.PlayAudio(NewDestructionEffect, DestructionAudio, false);
                    }
                    else
                    {
                        //no audio source and there's an audio clip assigned? report it:
                        Debug.LogError("A destruction audio clip has been assigned but the destruction effect object doesn't have an audio source!");
                    }
                }
            }

            //Destroy the unit's object:
            if (DestroyObj == true) //only if object destruction is allowed
            {
                Destroy(gameObject, DestroyObjTime);
                Destroy(TargetPosColl);
            }
        }

		public void RemoveFromFaction ()
		{
			//Remove this unit from the lists in the team manager:
			if (FactionMgr != null && FreeUnit == false) {
				FactionMgr.RemoveUnitFromLists (this);

                //if the unit has a task launcher:
                if (TaskMgr != null)
                {
                    //Launch the delegate event:
                    if (GameMgr.Events)
                        GameMgr.Events.OnTaskLauncherRemoved(TaskMgr);

                    //If there are pending tasks, stop them and give the faction back the resources of these tasks:
                    if (TaskMgr.TasksQueue.Count > 0)
                    {
                        int j = 0;
                        while (j < TaskMgr.TasksQueue.Count)
                        {
                            TaskMgr.CancelInProgressTask(TaskMgr.TasksQueue[j].ID);
                        }
                    }

                    //Clear all the pending tasks.
                    TaskMgr.TasksQueue.Clear();
                }

				if (gameObject.GetComponent<APC> ()) {
					//if the unit is an APC vehicle:
					int ContainedUnits = gameObject.GetComponent<APC> ().CurrentUnits.Count;
					if (ContainedUnits > 0) { //if ther eare units inside the APC
						for (int i = 0; i < ContainedUnits; i++) { //loop through them
							//release all units:
							if (gameObject.GetComponent<APC> ().ReleaseOnDestroy == true) { //release on destroy:
								gameObject.GetComponent<APC> ().RemoveUnit (gameObject.GetComponent<APC> ().CurrentUnits [0]);
							} else {
								//destroy contained units:
								gameObject.GetComponent<APC> ().CurrentUnits [0].DestroyUnit();
							}
						}
					}
				}

				GameMgr.Factions [FactionID].UpdateCurrentPopulation(-1);
				UIMgr.UpdatePopulationUI ();
			}
		}

        //unit conversion:
        public void ConvertUnit(Unit Converter)
        {
            //if same faction, then do nothing
            if (Converter.FactionID == FactionID)
                return;

            if (GameManager.MultiplayerGame == false)
            {
                //single player game, directly convert unit:
                ConvertUnitLocal(Converter);
            }
            else //online game:
            {
                if (GameMgr.IsLocalPlayer(gameObject))
                { //if this is the local player:
                  //send input action to the input manager
                    InputVars NewInputAction = new InputVars();
                    //mode:
                    NewInputAction.SourceMode = (int)InputSourceMode.CustomCommand;
                    NewInputAction.TargetMode = (int)InputCustomMode.Convert;

                    NewInputAction.Source = gameObject;
                    NewInputAction.Target = Converter.gameObject;

                    InputManager.SendInput(NewInputAction);
                }
            }
        }

        public void ConvertUnitLocal(Unit Converter)
        {
            //remove unit from the previous faction:
            if (FreeUnit == false) {
                RemoveFromFaction();
            }

            if (TaskMgr != null)  //if the unit has a task manager
            {
                //If there are pending tasks, stop them and give the faction back the resources of these tasks:
                if (TaskMgr.TasksQueue.Count > 0)
                {
                    int j = 0;
                    while (j < TaskMgr.TasksQueue.Count)
                    {
                        TaskMgr.CancelInProgressTask(TaskMgr.TasksQueue[j].ID);
                    }
                }
            }

            FreeUnit = false;
			//Add unit to new faction:
			FactionMgr = GameMgr.Factions[Converter.FactionID].FactionMgr;
			FactionID = Converter.FactionID;
			CreatedBy = GameMgr.Factions[Converter.FactionID].CapitalBuilding;
			//Add the newly created unit to the team manager list:
			FactionMgr.AddUnitToLists (this);	    
			//reset the unit's colors:
			SetUnitColors();

            //if the unit has a task launcher:
            if (TaskMgr != null)
            {
                //Clear all the pending tasks.
                TaskMgr.TasksQueue.Clear();
                TaskMgr.SetFactionInfo(); //update faction info
            }

            Converter.ConvertMgr.EnableConvertEffect();

            if (GameMgr.Events) {
				GameMgr.Events.OnUnitConverted (Converter, this);
			}

            //reload the minimap icon color
            MinimapIconMgr.AssignIconColor(PlayerSelection);

            //stop movement of unit:
            StopMvt();
            //make the unit idle:
            CancelAttack();
            CancelBuilding();
            CancelCollecting();
            CancelHealing();
            CancelConverting();
        }

		//See if the unit is in idle state or not:
		public bool IsIdle()
		{
			if (Moving == true) {
				return false;
			}
			if (BuilderMgr) {
				if (BuilderMgr.TargetBuilding == true)
					return false;
			}
			if (ResourceMgr) {
				if (ResourceMgr.TargetResource == true)
					return false;
			}
			if (AttackMgr) {
				if (AttackMgr.AttackTarget != null)
					return false;
			}
			if (HealMgr) {
				if (HealMgr.TargetUnit != null)
					return false;
			}
			if (ConvertMgr) {
				if (ConvertMgr.TargetUnit != null)
					return false;
			}

			return true;
		}

		//Handling animations:
		public void SetAnimState (UnitAnimState NewState)
		{
            //if our animation state is locked then don't proceed.
            if (LockAnimState == true)
                return;

            if (AnimMgr != null && gameObject.activeInHierarchy == true)
            { //if there's an animation manager
                if (AnimMgr.gameObject.activeInHierarchy == true && AnimMgr.runtimeAnimatorController != null)
                { //making sure the object that has the animator manager is active
                    CurrentAnimState = NewState;
                    switch (CurrentAnimState)
                    {
                        case UnitAnimState.Idle:
                            AnimMgr.SetBool("IsIdle", true);

                            //Stop any current action we're making because we just moved to the idle state:
                            if (AttackMgr)
                                AnimMgr.SetBool("IsAttacking", false);
                            AnimMgr.SetBool("IsMoving", false);
                            if (ResourceMgr)
                                AnimMgr.SetBool("IsCollecting", false);
                            if (BuilderMgr)
                                AnimMgr.SetBool("IsBuilding", false);
                            if (ConvertMgr)
                                AnimMgr.SetBool("IsConverting", false);
                            if (HealMgr)
                                AnimMgr.SetBool("IsHealing", false);
                            break;
                        case UnitAnimState.Building:
                            AnimMgr.SetBool("IsIdle", false);
                            AnimMgr.SetBool("IsBuilding", true);
                            break;
                        case UnitAnimState.Healing:
                            AnimMgr.SetBool("IsIdle", false);
                            AnimMgr.SetBool("IsHealing", true);
                            break;
                        case UnitAnimState.Converting:
                            AnimMgr.SetBool("IsIdle", false);
                            AnimMgr.SetBool("IsConverting", true);
                            break;
                        case UnitAnimState.Collecting:
                            AnimMgr.SetBool("IsIdle", false);
                            AnimMgr.SetBool("IsCollecting", true);
                            break;
                        case UnitAnimState.Moving:
                            AnimMgr.SetBool("IsIdle", false);
                            AnimMgr.SetBool("IsCollecting", false);
                            AnimMgr.SetBool("IsBuilding", false);
                            AnimMgr.SetBool("IsMoving", true);
                            break;
                        case UnitAnimState.Attacking:
                            AnimMgr.SetBool("IsIdle", false);
                            AnimMgr.SetBool("IsAttacking", true);
                            break;
                        case UnitAnimState.TakeDamage:
                            if (EnableTakeDamageAnim == true)
                            {
                                AnimMgr.SetBool("TookDamage", true);
                            }
                            break;
                        case UnitAnimState.Dead:
                            AnimMgr.SetBool("IsIdle", true);
                            AnimMgr.SetBool("IsDead", true);
                            break;
                        default:
                            return;
                    }
                }
            }
        }

        //Change the animator override controller:
        public void SetAnimatorOverrideCtrl(AnimatorOverrideController AnimOverrideCtrl)
        {
            AnimMgr.runtimeAnimatorController = AnimOverrideCtrl;

            if (AnimOverrideCtrl != null)
                AnimMgr.Play(AnimMgr.GetCurrentAnimatorStateInfo(0).fullPathHash, -1, 0f);
        }
    }
}
 