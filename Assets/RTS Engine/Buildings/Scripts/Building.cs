using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.AI;
//FoW Only
//using FoW;

/* Building script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class Building : MonoBehaviour
    {
        public string Name; //Name of the building that will be displayed in the UI.
        public string Code; //The building's unique code that identifies it.
        public string Category; //The category that this building belongs to.
        public string Description; //A short description of the building that will be displayed when it's selected.
        public Sprite Icon; //The building's icon.
        public int TaskPanelCategory; //when using task panel categories, this is the category ID where the task button of this building will appear when selecting builder units.

        public bool FreeBuilding = false; //if true, then no faction will be controlling this building.
        public bool CanBeAttacked = true; //can the building be actually attacked from units? 

        [HideInInspector]
        public WorkerManager WorkerMgr; //the worker manager that handles this building constructors

        //When the building reaches its maximum health, the building will get its initial state.
        [HideInInspector]
        public bool IsBuilt = false; //Has the building been built after it has been placed on the map?

        public float Radius = 5.0f; //the building's radius will be used to determine when units stop when attacking the building
        public int FactionID = 0; //Building's team ID.
        public bool PlacedByDefault = false; //Is the building placed by default on the map.
        public bool PlaceOutsideBorder = false; //Can this building be placed outside the border?
        [HideInInspector]
        public bool Placed = false; //Has the building been placed on the map?
        [HideInInspector]
        public bool NewPos = false; //Did the player move the building while placing it? We need to know this so that we can minimize the times certain functions, that check if the 
                                    //new position of the building is correct or not, are called.
        [HideInInspector]
        public bool CanPlace = true; //Can the player place the building at its current position?
        [HideInInspector]
        public int CollisionAmount = 0; //The amount of colliders whose this building is in collision with.

        [System.Serializable]
        public class RequiredBuildingVars
        {
            public List<Building> BuildingList = new List<Building>(); //If one the buildings in this list is spawned and built then the requirement is fulfilled.
            public string Name; //will be displayed in the UI task tooltip
        }
        public List<RequiredBuildingVars> RequiredBuildings = new List<RequiredBuildingVars>();

        //Building only near resources:
        public bool PlaceNearResource = false; //if true then this building will only be placable in range of a resource of the type below.
        public string ResourceName = ""; //the resource type that this building will be placed in range
        public float ResourceRange = 5.0f; //the maximum distance between the building and the resource

        //Building population:
        public int AddPopulation = 0; //Increase the maximum population for this faction.

        //Resource drop off:
        public bool ResourceDropOff = false; //make resoure collectors able to drop off their resources at this building.
        public Transform DropOffPos;
        public bool AcceptAllResources = true; //when true then the resource collectors can drop off all resources in this building, if not then the resource must be assigned in the array below
                                               //a list of the resources that this building accepts to be dropped off in case it doesn't accept all resources
        public string[] AcceptedResources;


        //Building health:
        public float MaxHealth = 100.0f; //maximum health.
        public float HoverHealthBarY = 2.0f; //this is the height of the hover health bar (in case the feature is enabled).
        [HideInInspector]
        public float Health; //the building's current health.

        //Building state: You can show and hide parts of the building depending on its health:
        [System.Serializable]
        public class BuildingStateVars
        {
            //Below, you need to enter the interval at which the state will be activated:
            public float MinHealth = 0.0f;
            public float MaxHealth = 50.0f;
            //Make sure that the intervals do not interfere.

            public GameObject[] PartsToShow; //Parts of the building to show (activate).
            public GameObject[] PartsToHide; //Parts of the building to hide (desactivate).
        }
        public BuildingStateVars[] BuildingStates;
        public GameObject BuildingStatesParent; //Extra objects intented to be used only for building states (not shown when the building has max health) should be children of this object.
        [HideInInspector]
        public int BuildingStateID = -1; //Saves the current building state ID.
        public bool DestroyObj = true; //destroy the object on death? 
        public float DestroyObjTime = 0.0f;
        public ResourceManager.Resources[] DestroyAward; //these resources will be awarded to the faction that destroyed this unit


        [HideInInspector]
        public Border CurrentCenter = null; //The current building center that this building belongs to (is inside its borders)

        [HideInInspector]
        public bool FactionCapital = false; //If true, then the building is the capital of this faction (meaning that destroying it means that the faction loses).

        public SelectionObj PlayerSelection; //Must be an object that only include this script, a trigger collider and a kinematic rigidbody.
                                             //the collider represents the boundaries of the object (building or resource) that can be selected by the player.

        public GameObject ConstructionObj; //Must be a child object of the building. If it's assigned, it will be shown when the building is built for the first time.
        [System.Serializable]
        public class ConstructionStateVars
        {
            //Below, you need to enter the interval at which the state will be activated:
            public float MinHealth = 0.0f;
            public float MaxHealth = 50.0f;
            //Make sure that the intervals do not interfere.

            public GameObject ConstructionObj;
        }
        public ConstructionStateVars[] ConstructionStates; //if the construction obj is set to null, then this will be used and will show different construction objects depending on the building's health.
        int ConstructionState = -1;

        //Damage effect:
        public EffectObj DamageEffect; //Created when a damage is received in the contact point between the attack object and this one:

        public GameObject BuildingPlane; //The plane where the selection texture appears.

        public GameObject BuildingModel; //drag and drop the building's model here.

        //If the building allows to create unit, they will spawned in this position.
        public Transform SpawnPosition;
        //The position that the new unit goes to from the spawn position.
        public Transform GotoPosition;
        public Transform Rallypoint;

        //Building destruction effects:
        public AudioClip DestructionAudio; //audio played when the building is destroyed.
        public EffectObj DestructionEffect; //the object to create as destruction effect.
        public bool Destroyed = false;

        [System.Serializable]
        public class FactionColorsVars
        {
            public int MaterialID = 0;
            public Renderer Renderer;
        }
        public FactionColorsVars[] FactionColors; //Mesh renderers 

        public ResourceManager.Resources[] BuildingResources;

        //this the timer during which the building texture flahes when a unit is sent to construct or attack this building
        [HideInInspector]
        public float FlashTime = 0.0f;

        //Building Upgrade:
        public bool DirectUpgrade = true; //allow the player to directly upgrade this building? 
        public Building UpgradeBuilding = null; //the building to upgrade to
        public ResourceManager.Resources[] BuildingUpgradeResources; //resources required to launch the upgrade.
        public Building[] UpgradeRequiredBuildings; //buildings that must be spawned in order to launch the upgrade.
        public float BuildingUpgradeReload = 8.0f; //duration of the upgrade
        public GameObject UpgradeBuildingEffect; //effect spawned when the upgrade is launched.
        [HideInInspector]
        public float BuildingUpgradeTimer;
        [HideInInspector]
        public bool BuildingUpgrading = false;
        public bool UpgradeAllBuildings = false;

        //Resource collection bonus:
        //A building can effect the resources existing inside the same border by increasing the amount of collection per second:
        [System.Serializable]
        public class BonusResourcesVars
        {
            public string Name; //Resource's name
            public float AddCollectAmountPerSecond = 0.22f; //self-explantory
        }
        public BonusResourcesVars[] BonusResources;

        //NPC Army vars:
        //When the NPC manager launches an order to create army units, the buildings that receive this order will announce how many of the units are in progress.
        [HideInInspector]
        public int PendingUnitsToCreate = 0;
        [HideInInspector]
        public int PendingUnitsArmyID = 0;

        //Audio:
        public AudioClip SelectionAudio; //Audio played when the building is selected.
        public AudioClip UpgradeLaunchedAudio; //When the building upgrade starts.
        public AudioClip UpgradeCompletedAudio; //When the building has been upgraded.

        //building components:
        [HideInInspector]
        public Portal PortalMgr;
        [HideInInspector]
        public Attack AttackMgr;
        [HideInInspector]
        public Border BorderMgr;
        [HideInInspector]
        public APC APCMgr;
        [HideInInspector]
        public TaskLauncher TaskMgr;
        [HideInInspector]
        public ResourceGenerator ResourceGen;

        Collider Coll;
        Renderer PlaneRenderer;

        //Scripts:
        [HideInInspector]
        public SelectionManager SelectionMgr;
        [HideInInspector]
        public ResourceManager ResourceMgr;
        [HideInInspector]
        public FactionManager FactionMgr;
        [HideInInspector]
        public GameManager GameMgr;
        [HideInInspector]
        public UIManager UIMgr;
        MovementManager MvtMgr;
        [HideInInspector]
        public CameraMovement CamMov;
        [HideInInspector]
        AttackWarningManager AttackWarningMgr;
        [HideInInspector]
        MinimapIconManager MinimapIconMgr;
        TerrainManager TerrainMgr;

        //components:
        [HideInInspector]
        public NavMeshObstacle NavObs;

        void Awake()
        {
            NavObs = GetComponent<NavMeshObstacle>(); //get the navigation obstacle component.
            PortalMgr = GetComponent<Portal>(); //get the portal component
            AttackMgr = GetComponent<Attack>(); //attack component
            WorkerMgr = GetComponent<WorkerManager>(); //worker manager component
            Coll = GetComponent<Collider>(); //building's main collider.
            BorderMgr = GetComponent<Border>(); //border comp in the building
            APCMgr = GetComponent<APC>(); //APC comp
            PlaneRenderer = BuildingPlane.GetComponent<Renderer>(); //get the building's plane renderer here
            TaskMgr = GetComponent<TaskLauncher>(); //Get the task launcher here.
            ResourceGen = GetComponent<ResourceGenerator>();

            ConstructionState = -1; //initialize the construction state.

            //searching for all the comps that this script needs:
            GameMgr = GameManager.Instance;
            SelectionMgr = GameMgr.SelectionMgr;
            ResourceMgr = GameMgr.ResourceMgr;
            UIMgr = GameMgr.UIMgr;
            CamMov = GameMgr.CamMov;
            AttackWarningMgr = AttackWarningManager.Instance;
            MinimapIconMgr = MinimapIconManager.Instance;
            TerrainMgr = GameMgr.TerrainMgr;
            MvtMgr = GameMgr.MvtMgr;

        }

        void Start()
        {
            if (LayerMask.NameToLayer("SelectionPlane") > 0)
            { //if there's a layer for the selection plane
                BuildingPlane.layer = LayerMask.NameToLayer("SelectionPlane"); //assign this layer because we don't want the main camera showing it
            }

            if (BuildingPlane == null) //if the building plane is not available.
            {
                Debug.LogError("You must attach a plane object at the bottom of the building and set it to 'BuildingPlane' in the inspector.");
            }

            if (PlacedByDefault == false) //if the building is not placed by default.
            {
                PlaneRenderer.material.color = Color.green; //start by setting the selection texture color to green which implies that it's allowed to place building at its position.
            }
            else
            {
                BuildingPlane.SetActive(false); //hide the building plane in case the building is placed by default.
            }

            //Building boundaries:
            if (Coll == null) //if the building collider is not present.
            {
                Debug.LogError("The building parent object must have a collider to represent the building's boundaries.");
            }
            else
            {
                Coll.isTrigger = true; //the building's main collider must always have "isTrigger" is true.
            }

            Rallypoint = GotoPosition;
            //Hide the goto position:
            if (GotoPosition != null)
            {
                GotoPosition.gameObject.SetActive(false);
            }

            //Set the selection object if we're using a different collider for player selection:
            if (PlayerSelection != null)
            {
                //set the player selection object for this building/resource:
                PlayerSelection.MainObj = this.gameObject;
                //Disable the player selection collider object if the building has not been placed yet:
                if (Placed == false)
                {
                    PlayerSelection.gameObject.SetActive(false);
                }
            }
            else
            {
                Debug.LogError("Player selection collider is missing!");
            }

            if(PlacedByDefault) //if the building is supposed to be placed by default -> meaning that it is already in the scene
            {
                //place building and add max health to it.
                PlaceBuilding(FactionID);
                AddHealthLocal(MaxHealth, null);
            }
        }

        //Initial settings for the building:
        void InitBuilding(int factionID)
        {
            this.FactionID = factionID; //set the faction ID.

            //get the faction manager:
            FactionMgr = GameMgr.Factions[FactionID].FactionMgr;

            //Set the faction color objects:
            SetBuildingColors();
        }

        //initialize placement instance:
        public void InitPlacementInstance (int factionID, Border buildingCenter = null)
        {
            InitBuilding(factionID);

            //set the building center:
            CurrentCenter = buildingCenter;

            Placed = false; //mark as not placed.

            //default settings for placing the building.
            CollisionAmount = 0;
            CanPlace = false;

            //Enable the building's plane:
            BuildingPlane.SetActive(true);

            //disable the nav mesh obstacle comp
            if (NavObs)
            {
                NavObs.enabled = false;
            }
        }

        //method called when the player places the building
        public void PlaceBuilding(int factionID)
        {
            InitBuilding(factionID);

            //enable the nav mesh obstacle comp
            if (NavObs)
            {
                NavObs.enabled = true;
            }

            //Disable the selection collider so that it won't get auto selected as soon as it's spawned
            PlayerSelection.gameObject.GetComponent<Collider>().enabled = false;

            //if the building includes a border comp, then enable it as well
            if (BorderMgr)
                BorderMgr.enabled = true;

            //Activate the player selection collider:
            PlayerSelection.gameObject.SetActive(true);

            //Set the building's health to 0 so that builders can start adding health to it:
            Health = 0.0f;

            BuildingPlane.SetActive(false); //hide the building's plane

            //Building is now placed:
            Placed = true;
            //custom event:
            if (GameMgr.Events) GameMgr.Events.OnBuildingPlaced(this);
            ToggleConstructionObj(true); //Show the construction object when the building is placed.

            //if this is a free building:
            if (FreeBuilding == true)
                return; //do not proceed

            FactionMgr.AddBuildingToList(this); //add the building to the faction manager list.

            //if the building is to be placed inside border and this is not a border building
            if (PlaceOutsideBorder == false && BorderMgr == null)
            {
                CurrentCenter.RegisterBuildingInBorder(this); //register building in the territory that it belongs to.
            }

            //if there's a task launcher component attached to the building
            if (TaskMgr)
                TaskMgr.OnTasksInit();

            //if this is the local player
            if (GameManager.PlayerFactionID == this.FactionID && GameManager.MultiplayerGame == false)
            {
                //If god mode is enabled and this is the local player
                if (GodMode.Enabled == true)
                {
                    AddHealth(MaxHealth, null);
                }
                //If God Mode is not enabled, make builders move to the building to construct it if building isn't supposed to be placed by default
                else if (SelectionMgr.SelectedUnits.Count > 0 && PlacedByDefault == false)
                {
                    int i = 0; //counter
                    bool MaxBuildersReached = false; //true when the maximum amount of builders for the hit building has been reached.
                    while (i < SelectionMgr.SelectedUnits.Count && MaxBuildersReached == false)
                    { //loop through the selected as long as the max builders amount has not been reached.
                        if (SelectionMgr.SelectedUnits[i].BuilderMgr)
                        { //check if this unit has a builder comp (can actually build).
                          //make sure that the maximum amount of builders has not been reached:
                            if (WorkerMgr.CurrentWorkers < WorkerMgr.WorkerPositions.Length)
                            {
                                //Make the units fix/build the building:
                                SelectionMgr.SelectedUnits[i].BuilderMgr.SetTargetBuilding(this);
                            }
                            else
                            {
                                MaxBuildersReached = true;
                                //if the max builders amount has been reached.
                                //Show this message: 
                                GameMgr.UIMgr.ShowPlayerMessage("Max building amount for building has been reached!", UIManager.MessageTypes.Error);
                            }
                        }

                        i++;
                    }
                }
            }
        }

        //---------------------------------------------------------------------------------------------------

        void SetResourceBonus(bool Add)
        {
            if (BonusResources.Length == 0 || CurrentCenter == null)
                return;

            //Since the resource bonus is given to resources inside the same border, we will first check if the building is a building center or not.
            //If it's a building center, then the resources around it will receive the bonus. If not, then we'll give the bonus to the building's center resources.
            //So first of all, we will pick the position where we will start looking for resources from depending on the above:
            Vector3 CenterPos = Vector3.zero;
            float Size = CurrentCenter.Size;
            if (BorderMgr)
            {
                CenterPos = transform.position;
            }
            else
            {
                CenterPos = CurrentCenter.transform.position;
            }

            //Search for the resources around the center position:
            Collider[] SearchResources = Physics.OverlapSphere(CenterPos, Size);
            if (SearchResources.Length > 0)
            {
                //Loop through all searched resources:
                for (int i = 0; i < SearchResources.Length; i++)
                {
                    for (int j = 0; j < BonusResources.Length; j++)
                    {
                        Resource TempResource = SearchResources[i].gameObject.GetComponent<Resource>();
                        if (TempResource)
                        {
                            //Add the bonus amount if resource matches:
                            if (TempResource.Name == BonusResources[j].Name)
                            {
                                if (Add == true)
                                {
                                    TempResource.CollectAmountPerSecond += BonusResources[j].AddCollectAmountPerSecond;
                                }
                                else
                                {
                                    TempResource.CollectAmountPerSecond -= BonusResources[j].AddCollectAmountPerSecond;

                                }
                            }
                        }
                    }
                }
            }
        }

        //method called when placing a building to check if it's current position is valid or not:
        public void CheckBuildingPos()
        {
            NewPos = false;
            //FoW Only:
            /*//uncomment this only if you are using the Fog Of War asset and replace MIN_FOG_STRENGTH with the value you need.
            if (FogOfWar.GetFogOfWarTeam(FactionID).IsInFog(transform.position, 0.5f))
            {
                PlaneRenderer.material.color = Color.red; //Show the player that the building can't be placed here.
                CanPlace = false; //The player can't place the building at this position.
                return;
            }*/

            //first check if the building is in range of 
            if (!IsBuildingInRange() || !IsBuildingOnMap() || !IsBuildingNearResource())
            {
                PlaneRenderer.material.color = Color.red; //Show the player that the building can't be placed here.
                CanPlace = false; //The player can't place the building at this position.
            }
            else if (CollisionAmount == 0)
            {
                PlaneRenderer.material.color = Color.green; //Show the player that the building can be placed here.
                CanPlace = true; //The player can place the building at this position.
            }
        }

        //method called when the building is in the process of upgrading
        void UpdateBuildingUpgrade()
        {
            //if the building is selected:
            if (BuildingUpgradeTimer > 0)
            { //if the timer is still going
                BuildingUpgradeTimer -= Time.deltaTime;
                if (SelectionMgr.SelectedBuilding == this)
                { //if this building is selected
                    UIMgr.UpdateBuildingUpgrade(this); //keep updating the UI
                }
            }
            if (BuildingUpgradeTimer <= 0)
            { //if the upgrade timer comes to an end
                if (BuildingUpgradeTimer == -1.0f)
                {
                    LaunchBuildingUpgrade(false);
                }
                else
                {
                    LaunchBuildingUpgrade(true);
                }
            }
        }

        void Update()
        {
            //For the player faction only, because we check if the object is in range or not for other factions inside the NPC building manager: 
            if (FactionID == GameManager.PlayerFactionID)
            {
                if (Placed == false)
                { //If the building isn't placed yet, we'll check if its inside the chosen range from the nearest building center:
                    if (NewPos == true)
                    { //If the building has been moved from its last position.
                        CheckBuildingPos();
                    }
                }
            }

            //Selection flash timer:
            if (FlashTime > 0)
            {
                FlashTime -= Time.deltaTime;
            }
            if (FlashTime < 0)
            {
                //if the flash timer is over:
                FlashTime = 0.0f;
                CancelInvoke("SelectionFlash");
                BuildingPlane.gameObject.SetActive(false); //hide the building plane.
            }

            if (IsBuilt == true)
            {
                if (BuildingUpgrading == true)
                { //if we are upgrading the building
                    UpdateBuildingUpgrade();
                }
            }

        }

        //a method to send a unit to the building's rally point
        public void SendUnitToRallyPoint(Unit RefUnit)
        {
            if (Rallypoint != null)
            {
                Building BuildingRallyPoint = Rallypoint.gameObject.GetComponent<Building>();
                Resource ResourceRallyPoint = Rallypoint.gameObject.GetComponent<Resource>();
                if (BuildingRallyPoint && RefUnit.BuilderMgr)
                {
                    if (BuildingRallyPoint.WorkerMgr.CurrentWorkers < BuildingRallyPoint.WorkerMgr.WorkerPositions.Length)
                    {
                        RefUnit.BuilderMgr.SetTargetBuilding(BuildingRallyPoint);
                    }
                }
                else if (ResourceRallyPoint && RefUnit.ResourceMgr)
                {
                    if (ResourceRallyPoint.WorkerMgr.CurrentWorkers < ResourceRallyPoint.WorkerMgr.WorkerPositions.Length)
                    {
                        RefUnit.ResourceMgr.SetTargetResource(ResourceRallyPoint);
                    }
                }
                else
                {
                    //move the unit to the goto position
                    MvtMgr.Move(RefUnit, Rallypoint.position, 0.0f, null, InputTargetMode.None);
                }
            }
        }



        //Flashing building selection (when the player sends units to contruct a building, its texture flashes for some time):
        public void SelectionFlash()
        {
            BuildingPlane.gameObject.SetActive(!BuildingPlane.activeInHierarchy);
        }

        //Placing the building:

        //Detecting collision with other objects when placing the building.
        void OnTriggerEnter(Collider other)
        {
            if (Placed == false)
            { //if the building is still not placed.
                
                //if this is not the collider on this gameobject and neither the collider on the player selection object.
                if (other != Coll && other.gameObject != PlayerSelection.gameObject)
                {
                    CollisionAmount += 1 ; //Counting how many colliders have entered in collision with the building.

                    //If the building isn't placed yet and it's in collision with another object
                    PlaneRenderer.material.color = Color.red; //Show the player that the building can't be placed here.
                    CanPlace = false; //The player can't place the building at this position.
                }
            }
        }

        //If the building is no longer in collision with an object
        void OnTriggerExit(Collider other)
        {
            if (Placed == false)
            { //if the building has not been placed yet.
                if (other != Coll && other.gameObject != PlayerSelection.gameObject)
                {
                    CollisionAmount -= 1; //Counting how many colliders have entered in collision with the building.
                }
                if (CollisionAmount <= 0)
                { //If the building isn't placed yet and it's in collision with another object
                    CollisionAmount = 0;
                    PlaneRenderer.material.color = Color.green; //Show the player that the building can be placed here.
                    CanPlace = true; //The player can place the building at this position.
                }
            }
        }

        //Building Selection:

        void OnMouseDown()
        {
            if (PlayerSelection == null)
            { //If we're not another collider for player selection, then we'll use the same collider for placement.
                if (!EventSystem.current.IsPointerOverGameObject())
                { //Make sure that the mouse is not over any UI element
                    if (Placed == true && BuildingPlacement.IsBuilding == false)
                    {
                        //Only select the building when it's already placed and when we are not attempting to place any building on the map:
                        SelectionMgr.SelectBuilding(this);
                    }
                }
            }
        }

        //Toggle construction object:
        public void ToggleConstructionObj(bool Toggle)
        {
            UpdateConstructionState();

            //If we have an object to show as the building when it's under construction:
            if (ConstructionObj != null)
            {
                //hide or show the building's model:
                if (BuildingModel)
                    BuildingModel.SetActive(!Toggle);

                ConstructionObj.SetActive(Toggle); //hide or show the construction object.
            }
        }

        //called when the building is under construction and its health changed so the construction state must be updated
        public void UpdateConstructionState()
        {
            //making sure the building is under construction and that there are actually construction states.
            if (IsBuilt == false && ConstructionStates.Length > 0)
            {

                //if there's a default construction object then hide it
                if (ConstructionObj != null)
                {
                    ConstructionObj.SetActive(false);
                }

                bool Found = false;
                int i = 0;
                //go through all the construction states
                while (i < ConstructionStates.Length && Found == false)
                {
                    //when a state that includes the current building's health in its interval is found
                    if (Health >= ConstructionStates[i].MinHealth && Health <= ConstructionStates[i].MaxHealth)
                    {
                        //pick it
                        ConstructionObj = ConstructionStates[i].ConstructionObj;
                        ConstructionState = i;

                        Found = true;
                    }
                    i++;
                }
            }
        }

        //method called to set a faction building's colors:
        void SetBuildingColors()
        {
            //If there's actually objects to color in this prefab and the building belongs to a faction.
            if (FactionColors.Length > 0 && FreeBuilding == false)
            {
                //Loop through the faction color objects (the array is actually a MeshRenderer array because we want to allow only objects that include mesh renderers in this prefab):
                for (int i = 0; i < FactionColors.Length; i++)
                {
                    //Always checking if the object/material is not invalid:
                    if (FactionColors[i].Renderer != null)
                    {
                        //Color the object to the faction's color:
                        FactionColors[i].Renderer.materials[FactionColors[i].MaterialID].color = GameMgr.Factions[FactionID].FactionColor;
                    }
                }
            }
        }

        //method called to complete the construction of the building
        void CompleteConstruction()
        {
            IsBuilt = true; //mark as built.

            //set the layer to IgnoreRaycast as we don't want any raycast to recongize this:
            gameObject.layer = 2;

            //custom event:
            if (GameMgr.Events) GameMgr.Events.OnBuildingBuilt(this);

            //If we have an object to show as the building when it's under construction:
            //hide it and shown the actual building:
            ToggleConstructionObj(false);

            //Check if the building is currently selected:
            if (SelectionMgr.SelectedBuilding == this)
            {
                //Update the selection UI:
                SelectionMgr.SelectBuilding(this);
            }

            if (FreeBuilding == false)
            {
                OnFactionBuildingBuilt();
            }
        }

        //method to configure a faction building:
        void OnFactionBuildingBuilt()
        {
            //if the building includes the border component:
            if (BorderMgr)
            {
                //if the border is not active yet.
                if (BorderMgr.IsActive == false)
                {
                    //add the building to the building centers list (the list that includes all buildings wtih a border comp):
                    FactionMgr.BuildingCenters.Add(this);

                    //activate the border
                    BorderMgr.ActivateBorder();
                    CurrentCenter = BorderMgr; //make the building its own center.
                }
            }

            //if there's a task manager:
            if (TaskMgr)
            {
                //update the tasks to the current upgrade level:
                TaskMgr.SyncTaskUpgradeLevel();

                /*//If the building belongs to a NPC player then we'll check for resources:
                if (FactionID != GameManager.PlayerFactionID && GameManager.MultiplayerGame == false)
                {
                    FactionMgr.ResourceMgr.AllUnitsUpgraded = false;

                    //if the faction is NPC, alert the NPC Army manager that a new building has been added, possibility of creating army units or units that the NPC spawner need:
                    if (FactionID != GameManager.PlayerFactionID)
                    {
                        //inform the NPC army:
                        FactionMgr.ArmyMgr.ReloadArmyUnitsPriority(TaskMgr, true);
                    }
                }*/
            }

            if (ResourceDropOff == true)
            {
                //If this building allows resources to be dropped off at it, then add it to the list:
                FactionMgr.DropOffBuildings.Add(this);
                FactionMgr.CheckCollectorsDropOffBuilding();
            }

            //update the faction population slots.
            GameMgr.Factions[FactionID].UpdateMaxPopulation(AddPopulation);
            UIMgr.UpdatePopulationUI();

            SetResourceBonus(true); //apply the resource bonus

            //If the building has a goto position:
            if (GotoPosition != null)
            {
                //Check if the building is currently selected:
                if (SelectionMgr.SelectedBuilding == this)
                {
                    //Show the goto pos
                    GotoPosition.gameObject.SetActive(true);
                }
                else
                {
                    //hide the goto pos
                    GotoPosition.gameObject.SetActive(false);
                }
            }
        }

        //Building health:

        public void AddHealthLocal(float Value, GameObject Source) //changes the health for the local player.
        {
            if (Placed == false)
            { //if the building is not placed, we have nothing to do.
                return;
            }

            Health += Value; //add the requested value to the building's health
            if (Health >= MaxHealth)
            { //if the building has reached the max health:
                Health = MaxHealth;

                //go through all worker positions and look for all workers
                for (int i = 0; i < WorkerMgr.WorkerPositions.Length; i++)
                {
                    if (WorkerMgr.WorkerPositions[i].CurrentUnit != null) //if this is a avalid worker
                    {
                        WorkerMgr.WorkerPositions[i].CurrentUnit.CancelBuilding();
                    }
                }

                if (IsBuilt == false)
                { //if the building was being constructed for the time:

                    CompleteConstruction();
                }
            }

            //Custom event:
            if (GameMgr.Events)
                GameMgr.Events.OnBuildingHealthUpdated (this, Value, Source);

            //if the mouse is over this building
            if (UIMgr.IsHoverSource(PlayerSelection))
            {
                //update hover health bar:
                UIMgr.UpdateHoverHealthBar(Health, MaxHealth);
            }

            //Updating the building's health UI when it's selected:
            if (SelectionMgr.SelectedBuilding == this)
            {
                //only update the UI health when the actual building health has changed.
                UIMgr.UpdateBuildingHealthUI(this);
            }

            //If the building receives damage:
            if (Value < 0.0f)
            {
                //if it's not a free building and it belongs to the local player
                if (FreeBuilding == false && FactionID == GameManager.PlayerFactionID)
                {
                    //show warning in mini map to let player know that he is getting attacked.
                    AttackWarningMgr.AddAttackWarning(this.gameObject);
                }
            }

            //if the building's health is null and the building has not been destroyed.
            if (Health <= 0)
            {
                Health = 0.0f;

                if (Destroyed == false)
                {

                    //award the destroy award to the source:
                    if (DestroyAward.Length > 0)
                    {
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
                    }

                    //destroy the building
                    DestroyBuilding(false);
                    Destroyed = true;
                }

            }
            else
            {
                //Check the building's state: 
                //only if it has been built:
                if (IsBuilt == true)
                {
                    CheckBuildingState();
                }
                else
                {
                    if (ConstructionStates.Length > 0)
                    { //if the building uses construction states:
                        if (ConstructionState < ConstructionStates.Length)
                        { //check if we have already changed the state:
                            if (ConstructionState >= 0)
                            {
                                if (Health > ConstructionStates[ConstructionState].MaxHealth)
                                {
                                    ToggleConstructionObj(true);
                                }
                            }
                            else
                            {
                                ToggleConstructionObj(true);
                            }
                        }

                    }
                }
            }
        }

        //add health to the building.
        public void AddHealth(float Value, GameObject Source)
        {
            //AddHealthLocal (Value, Source);

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
                    NewInputAction.SourceMode = (byte)InputSourceMode.Building;
                    NewInputAction.TargetMode = (byte)InputTargetMode.Self;

                    NewInputAction.Source = gameObject;
                    NewInputAction.Target = Source;
                    NewInputAction.InitialPos = transform.position;

                    NewInputAction.Value = (int)Value;

                    InputManager.SendInput(NewInputAction);
                }
            }
        }

        //a method that handles the building destruction.
        public void DestroyBuilding(bool Upgrade)
        {
            //if it's a single player game, destroy the building locally, simple.
            if (GameManager.MultiplayerGame == false)
            {
                DestroyBuildingLocal(Upgrade);
            }
            else
            {
                //if it's a MP game:
                if (GameMgr.IsLocalPlayer(gameObject))
                {
                    //ask the server to destroy the building
                    InputVars NewInputAction = new InputVars();
                    //mode:
                    NewInputAction.SourceMode = (byte)InputSourceMode.Destroy;

                    NewInputAction.Source = gameObject;
                    NewInputAction.Value = (Upgrade == true) ? 1 : 0; //when upgrade == true, then set to 1. if not set to 0

                    InputManager.SendInput(NewInputAction);
                }
            }
        }

        public void DestroyBuildingLocal(bool Upgrade)
        {
            int i = 0;
            //Destroy building:

            //if this is the current source of the hover health bar:
            if (UIMgr.IsHoverSource(PlayerSelection))
            {
                UIMgr.TriggerHoverHealthBar(false, PlayerSelection, 0.0f);
            }


            //If this building is selected then deselect it:
            if (SelectionMgr.SelectedBuilding == this)
            {
                SelectionMgr.DeselectBuilding();
            }

            if (FreeBuilding == false)
            {
                if (APCMgr)
                {
                    //if the unit is an APC vehicle:
                    while (APCMgr.CurrentUnits.Count > 0)
                    { //go through all units
                      //release all units:
                        if (APCMgr.ReleaseOnDestroy == true)
                        { //release on destroy:
                            APCMgr.RemoveUnit(APCMgr.CurrentUnits[0]);
                        }
                        else
                        {
                            //destroy contained units:
                            APCMgr.CurrentUnits[0].DestroyUnit();
                            APCMgr.CurrentUnits.RemoveAt(0);
                        }
                    }
                }

                //If the building is considered as a center (defines borders)

                if (BorderMgr)
                {

                    if (GameManager.MultiplayerGame == false || (GameManager.MultiplayerGame == true && FactionID == GameManager.PlayerFactionID))
                    {
                        //Free all the resources inside this border so other:
                        if (BorderMgr.ResourcesInRange.Count > 0)
                        {
                            for (i = 0; i < BorderMgr.ResourcesInRange.Count; i++)
                            {
                                BorderMgr.ResourcesInRange[i].FactionID = -1;
                            }
                        }
                        BorderMgr.ResourcesInRange.Clear();

                        //Go through all the borders' centers to refresh the resources inside this border (as one of the freed resources above could now belong to another center):
                        for (i = 0; i < GameMgr.AllBorders.Count; i++)
                        {
                            //Loop through all the borders while respecting their priority order:
                            GameMgr.AllBorders[i].CheckBorderResources();
                        }

                        //Remove the border from the all borders list if it has been already activated.
                        if (GameMgr.AllBorders.Contains(BorderMgr))
                        {
                            GameMgr.AllBorders.Remove(BorderMgr);
                        }

                        //Remove the building from the building centers list in the faction manager:
                        if (FactionMgr.BuildingCenters.Contains(this))
                        {
                            FactionMgr.BuildingCenters.Remove(this);
                        }

                        //Remove the building from the resource drop off building lists if it's there:
                        if (FactionMgr.DropOffBuildings.Contains(this))
                        {
                            FactionMgr.DropOffBuildings.Remove(this);
                        }
                    }

                    //Destroy the border object.
                    if (BorderMgr.IsActive == true && BorderMgr.SpawnBorderObj == true)
                    {
                        Destroy(BorderMgr.BorderObj);
                    }
                }
                else
                {
                    if (GameManager.MultiplayerGame == false || (GameManager.MultiplayerGame == true && FactionID == GameManager.PlayerFactionID))
                    {
                        //If the building is not a center then we'll check if it occupies a place in the defined buildings for its center:
                        if (CurrentCenter != null)
                        {
                            CurrentCenter.UnegisterBuildingInBorder(this);
                        }
                    }
                }

                //if there's a task maanger:
                if (TaskMgr != null)
                {
                    //Launch the delegate event:
                    if (GameMgr.Events)
                        GameMgr.Events.OnTaskLauncherRemoved(TaskMgr);
                }

                if (GameManager.MultiplayerGame == false || GameMgr.IsLocalPlayer(gameObject))
                {
                    GameMgr.Factions[FactionID].UpdateMaxPopulation(-AddPopulation); //remove population added by this building when destroyed

                    //Remove the building from the spawned buildings list in the faction manager:
                    if (FactionMgr.Buildings.Contains(this))
                    {
                        FactionMgr.RemoveBuilding(this);
                    }

                    //if the building has a task launcher:
                    if (TaskMgr != null)
                    {
                        //If there are pending tasks, stop them and give the faction back the resources of these tasks:
                        if (TaskMgr.TasksQueue.Count > 0)
                        {
                            int j = 0;
                            while (j < TaskMgr.TasksQueue.Count)
                            {
                                TaskMgr.CancelInProgressTask(TaskMgr.TasksQueue[j].ID);
                            }

                            //Clear all the pending tasks.
                            TaskMgr.TasksQueue.Clear();
                        }
                    }

                    //Remove bonuses from nearby resources:
                    SetResourceBonus(false);

                    //Check if it's the capital building:
                    if (FactionCapital == true && Upgrade == false)
                    {

                        if (GameManager.MultiplayerGame == false)
                        {

                            //Mark the faction as defeated:
                            GameMgr.OnFactionDefeated(FactionID);
                        }
                        else
                        {
                            //ask the server to announce that the faction has been defeated.
                            //send input action to the input manager
                            InputVars NewInputAction = new InputVars();
                            //mode:
                            NewInputAction.SourceMode = (byte)InputSourceMode.Destroy;
                            NewInputAction.TargetMode = (byte)InputTargetMode.Faction;

                            NewInputAction.Value = FactionID;

                            InputManager.SendInput(NewInputAction);
                        }
                    }
                }
                //Spawn the destruction effect obj if it exists:
                if (DestructionEffect != null && Upgrade == false)
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

            }

            //remove the minimap icon
            MinimapIconMgr.RemoveMinimapIcon(PlayerSelection);

            if (Upgrade == false)
            {
                //custom event:
                if (GameMgr.Events)
                    GameMgr.Events.OnBuildingDestroyed(this);
            }

            //Destroy the building's object:
            if (DestroyObj == true) //only if object destruction is allowed
            {
                Destroy(gameObject, DestroyObjTime);
            }
        }

        //This checks if the building is inside the faction's borders:
        public bool IsBuildingInRange()
        {
            if (PlaceOutsideBorder == true && FactionID == GameManager.PlayerFactionID)
            {
                return true;
            }

            float Distance = 0.0f;
            bool InRange = false;
            int i = 0;

            //First we check if the building is still inside its last noted building center's borders:
            if (CurrentCenter != null)
            {
                Distance = Vector3.Distance(CurrentCenter.transform.position, this.transform.position);
                if (Distance <= CurrentCenter.Size)
                {
                    InRange = true;
                }
                else
                {
                    InRange = false;
                    CurrentCenter = null;
                }
            }

            if (CurrentCenter == null)
            {
                if (FactionMgr.BuildingCenters.Count > 0)
                {
                    //We have to start by checking the first city center, if the building is not inside its border, then we will move to the next ones:
                    Distance = Vector3.Distance(FactionMgr.BuildingCenters[0].transform.position, this.transform.position);
                    if (Distance <= FactionMgr.BuildingCenters[0].BorderMgr.Size && FactionMgr.BuildingCenters[0].BorderMgr.IsActive == true)
                    {
                        //If we are allowed to place this building inside this border:
                        if (FactionMgr.BuildingCenters[0].BorderMgr.AllowBuildingInBorder(Code) == true)
                        {
                            //If the current building is inside the center
                            InRange = true;
                            CurrentCenter = FactionMgr.BuildingCenters[0].BorderMgr;
                        }
                    }
                    if (FactionMgr.BuildingCenters.Count > 1)
                    {
                        i = 1;
                        while (InRange == false && i < FactionMgr.BuildingCenters.Count)
                        {
                            Distance = Vector3.Distance(FactionMgr.BuildingCenters[i].transform.position, this.transform.position);
                            if (Distance <= FactionMgr.BuildingCenters[i].BorderMgr.Size && FactionMgr.BuildingCenters[i].BorderMgr.IsActive == true)
                            {
                                //If we are allowed to place this building inside this border:
                                if (FactionMgr.BuildingCenters[i].BorderMgr.AllowBuildingInBorder(Code) == true)
                                {
                                    //If the current building is inside the center
                                    InRange = true;
                                    CurrentCenter = FactionMgr.BuildingCenters[i].BorderMgr;
                                }
                            }

                            i++;
                        }
                    }
                }
            }


            if (CurrentCenter != null)
            {
                //Sometimes borders collide with each other but the priority of the border is made by order of creation of the border.
                //That's why we need to check for other factions' borders and make sure the building isn't inside one of them:

                i = 0;
                //So loop through all borders:
                while (i < GameMgr.AllBorders.Count && InRange == true)
                {
                    //Make sure the border is active:
                    if (GameMgr.AllBorders[i].IsActive == true)
                    {
                        //Make sure the border doesn't belong to this faction:
                        if (GameMgr.AllBorders[i].FactionMgr.FactionID != FactionID)
                        {

                            //Calculate the distance between this building and the building center the holds the border:
                            Distance = Vector3.Distance(GameMgr.AllBorders[i].transform.position, this.transform.position);
                            //Check if the building is inside the border:
                            if (Distance <= GameMgr.AllBorders[i].Size)
                            {
                                //See if the border has a priority over the one that the building belongs to:
                                if (GameMgr.AllBorders[i].BorderObj.gameObject.GetComponent<MeshRenderer>().sortingOrder > CurrentCenter.BorderObj.gameObject.GetComponent<MeshRenderer>().sortingOrder)
                                {
                                    InRange = false; //Cancel placing the building here.
                                }
                            }
                        }
                    }
                    i++;
                }

            }

            if (CurrentCenter == null)
                InRange = false;

            return InRange;
        }

        //check if the building is still on the map
        public bool IsBuildingOnMap()
        {
            bool OnMap = true; //Are all four corners and center of the building on the map?

            Ray RayCheck = new Ray();
            RaycastHit[] Hits;

            //Get the main box collider of the building:
            BoxCollider Coll = gameObject.GetComponent<BoxCollider>();

            //Start by checking if the middle point of the building's collider is over the map:

            //Set the ray check source point which is the center of the collider in the world:
            RayCheck.origin = new Vector3(transform.position.x + Coll.center.x, transform.position.y + 0.5f, transform.position.z + Coll.center.z);

            //The direction of the ray is always down because we want check if there's terrain right under the building's object:
            RayCheck.direction = Vector3.down;

            int PointID = 1;
            while (OnMap == true && PointID <= 5)
            {
                Hits = Physics.RaycastAll(RayCheck, 1.5f);
                bool HitTerrain = false;
                if (Hits.Length > 0)
                {
                    for (int i = 0; i < Hits.Length; i++)
                    {
                        if (TerrainMgr.IsTerrainTile(Hits[i].transform.gameObject))
                        {
                            HitTerrain = true;
                        }
                    }
                }

                if (HitTerrain == false)
                {
                    OnMap = false;
                    return OnMap;
                }

                PointID++;

                //If we reached this stage, then while checking the last, we successfully detected that there a terrain under it, so we'll move to the next point:
                switch (PointID)
                {

                    case 2:
                        RayCheck.origin = new Vector3(transform.position.x + Coll.center.x + Coll.size.x / 2, transform.position.y + 0.5f, transform.position.z + Coll.center.z + Coll.size.z / 2);
                        break;
                    case 3:
                        RayCheck.origin = new Vector3(transform.position.x + Coll.center.x + Coll.size.x / 2, transform.position.y + 0.5f, transform.position.z + Coll.center.z - Coll.size.z / 2);
                        break;
                    case 4:
                        RayCheck.origin = new Vector3(transform.position.x + Coll.center.x - Coll.size.x / 2, transform.position.y + 0.5f, transform.position.z + Coll.center.z - Coll.size.z / 2);
                        break;
                    case 5:
                        RayCheck.origin = new Vector3(transform.position.x + Coll.center.x - Coll.size.x / 2, transform.position.y + 0.5f, transform.position.z + Coll.center.z + Coll.size.z / 2);
                        break;
                }
            }
            return OnMap;
        }

        //This method checks if the building is near a specific resource that it's supposed to built in range of
        public bool IsBuildingNearResource()
        {
            if (PlaceNearResource == false || GameMgr.IsLocalPlayer(gameObject) == false)
            { //if we can place the building free from being in range of a resource or this is not the player's faction.
                return true;
            }
            else
            {
                //search for resources in range:
                foreach (Resource Resource in ResourceMgr.AllResources)
                {
                    //make sure this is the resource that the building needs:
                    if (Resource.Name == ResourceName)
                    {
                        if (Vector3.Distance(Resource.transform.position, this.transform.position) < ResourceRange)
                        { //resource is in range
                            return true; //found it
                        }
                    }
                }
            }
            //return false in case we reach this stage (did not find a resource)
            return false;
        }

        //This method allows to show/hide parts of the building depending on the 
        public void CheckBuildingState()
        {
            //Only set the building state when the health is not maximal:
            if (Health < MaxHealth)
            {
                //If there are actually building states:
                if (BuildingStates.Length > 0)
                {
                    //Check if we're not in the same state building...
                    if (BuildingStateID >= 0 && BuildingStateID < BuildingStates.Length)
                    {
                        //...by checking if the building's health is not in the last state interval:
                        if (Health < BuildingStates[BuildingStateID].MinHealth || Health > BuildingStates[BuildingStateID].MaxHealth)
                        {
                            //Look for a new building state:
                            UpdateBuildingState();
                        }
                    }
                    else
                    {
                        //No valid building state ID was found then look for a valid one:
                        UpdateBuildingState();
                    }
                }
            }
            else
            {
                //The building has maximum health so update its state:
                UpdateBuildingState();
            }
        }

        //update the building's state
        public void UpdateBuildingState()
        {
            int i = 0, j = 0;
            if (BuildingStateID >= 0 && BuildingStateID < BuildingStates.Length)
            {
                //First hide the parts that were shown in the last state:
                if (BuildingStates[BuildingStateID].PartsToShow.Length > 0)
                {
                    for (i = 0; i < BuildingStates[BuildingStateID].PartsToShow.Length; i++)
                    {
                        BuildingStates[BuildingStateID].PartsToShow[i].SetActive(false);
                    }
                }
                //and show the parts that were hidden in the last state:
                if (BuildingStates[BuildingStateID].PartsToHide.Length > 0)
                {
                    for (i = 0; i < BuildingStates[BuildingStateID].PartsToHide.Length; i++)
                    {
                        BuildingStates[BuildingStateID].PartsToHide[i].SetActive(true);
                    }
                }
            }

            //Then move to a new state only if the maximum health has not been reached:
            if (Health < MaxHealth)
            {
                while (i < BuildingStates.Length)
                {
                    //Check if the current building health is in the interval of this building state:
                    if (Health > BuildingStates[i].MinHealth && Health < BuildingStates[i].MaxHealth)
                    {
                        //Update the building state to this one:
                        //Hide some parts:
                        if (BuildingStates[i].PartsToHide.Length > 0)
                        {
                            for (j = 0; j < BuildingStates[i].PartsToHide.Length; j++)
                            {
                                BuildingStates[i].PartsToHide[j].SetActive(false);
                            }
                        }

                        //and show some others:
                        if (BuildingStates[i].PartsToShow.Length > 0)
                        {
                            for (j = 0; j < BuildingStates[i].PartsToShow.Length; j++)
                            {
                                BuildingStates[i].PartsToShow[j].SetActive(true);
                            }
                        }

                        BuildingStateID = i;

                        return;


                    }
                    i++;
                }
            }
            else
            {
                //Reset the building state ID when the maximum health has been reached:
                BuildingStateID = -1;
            }
        }

        //Launch building upgrade:
        public void CheckBuildingUpgrade()
        {
            if (UpgradeBuilding == null)
            {
                return;
            }

            if (FactionMgr == null)
            {
                FactionMgr = GameMgr.Factions[FactionID].FactionMgr;
            }

            if (ResourceMgr.CheckResources(BuildingUpgradeResources, FactionID))
            { //if the faction has the required resources to upgrade the building.
              //check if the required buildings are spawned:
                if (FactionMgr.AreBuildingsSpawned(UpgradeRequiredBuildings))
                {

                    //Check if there are no pending tasks:
                    bool HasPendingTasks = false;
                    if (TaskMgr != null)
                        HasPendingTasks = TaskMgr.TasksQueue.Count != 0;

                    if (HasPendingTasks == false)
                    { //if there are no pending quests.
                        BuildingUpgrading = true; //launch the upgrade timer:
                        BuildingUpgradeTimer = BuildingUpgradeReload;

                        ResourceMgr.TakeResources(BuildingUpgradeResources, FactionID); //take resources.
                                                                                        //custom event:
                        if (GameMgr.Events)
                            GameMgr.Events.OnBuildingStartUpgrade(this, true);

                        //show the UI (if it's the local player):
                        if (FactionID == GameManager.PlayerFactionID)
                        {
                            if (SelectionMgr.SelectedBuilding == this)
                            {
                                UIMgr.UpdateBuildingUI(this);
                                UIMgr.HideTooltip();
                            }
                        }

                        //if this is the local player:
                        if(FactionID == GameManager.PlayerFactionID)
                        {
                            //play the upgrade launch audio clip
                            AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, UpgradeLaunchedAudio, false);
                        }

                    }
                    else
                    {
                        if (FactionID == GameManager.PlayerFactionID)
                        {
                            UIMgr.ShowPlayerMessage("The building must have no pending tasks to upgrade!", UIManager.MessageTypes.Error);
                        }
                    }
                }
                else
                {
                    if (FactionID == GameManager.PlayerFactionID)
                    {
                        UIMgr.ShowPlayerMessage("Not all required buildings for upgrade are built!", UIManager.MessageTypes.Error);
                    }
                }
            }
            else
            {
                if (FactionID == GameManager.PlayerFactionID)
                {
                    UIMgr.ShowPlayerMessage("You don't have enough resources to upgrade the building!", UIManager.MessageTypes.Error);
                }
            }

        }

        //launching a building upgrade is done here
        public void LaunchBuildingUpgrade(bool Direct)
        {
            if (UpgradeBuildingEffect != null)
            { //the upgrade effect:
                Instantiate(UpgradeBuildingEffect, this.transform.position, UpgradeBuildingEffect.transform.rotation);
            }
            BuildingUpgrading = false; //we are not upgrading the building anymore.
            //save the upgrade prefab:
            Building UpradePrefab = UpgradeBuilding;

            //if this is the local player:
            if (FactionID == GameManager.PlayerFactionID)
            {
                //play the upgrade completed audio clip;
                AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, UpgradeCompletedAudio, false);
            }

            //if this triggers a full buildings upgrade and this has been called directly (directly means that this has been called by the player himself by clicking on the UI button);
            if (UpgradeAllBuildings == true && Direct == true)
            {
                //convert all buildings inside the buildings list in the placement manager if it's the local player:
                if (FactionID == GameManager.PlayerFactionID)
                {
                    for (int j = 0; j < GameMgr.BuildingMgr.AllBuildings.Count; j++)
                    {
                        if (GameMgr.BuildingMgr.AllBuildings[j].UpgradeBuilding != null)
                        {
                            //set the new building in the placement manager:
                            GameMgr.BuildingMgr.ReplaceBuilding(GameMgr.BuildingMgr.AllBuildings[j].Code, GameMgr.BuildingMgr.AllBuildings[j].UpgradeBuilding);
                        }
                    }
                }
                else
                {
                    /*for (int j = 0; j < FactionMgr.BuildingMgr.AllBuildings.Count; j++)
                    {
                        if (FactionMgr.BuildingMgr.AllBuildings[j].UpgradeBuilding != null)
                        {
                            //set the new building in the placement manager:
                            FactionMgr.BuildingMgr.ReplaceBuilding(FactionMgr.BuildingMgr.AllBuildings[j].Code, FactionMgr.BuildingMgr.AllBuildings[j].UpgradeBuilding);
                        }
                    }*/
                }

                //save a list of the faction's buildings (because it will change in the process below):
                List<Building> OldBuildings = new List<Building>();
                OldBuildings.AddRange(FactionMgr.Buildings);
                //go through all the faction's buildings
                int i = 0;
                while (i < OldBuildings.Count)
                {
                    if (OldBuildings[i] != null && OldBuildings[i] != this)
                    { //make sure there's a building and that it's not this one
                        if (OldBuildings[i].UpgradeBuilding != null)
                        {
                            if (OldBuildings[i].IsBuilt == true)
                            { //if the buildng is built
                                OldBuildings[i].LaunchBuildingUpgrade(false);
                                OldBuildings.RemoveAt(i);
                            }
                            else
                            {
                                OldBuildings[i].BuildingUpgrading = true; //launch the upgrade timer:
                                OldBuildings[i].BuildingUpgradeTimer = -1.0f; //-1 means that the next upgrade is ordered by another building
                                i++;
                            }
                        }
                        else
                        {
                            i++;
                        }
                    }
                    else
                    {
                        i++;
                    }
                }

            }

            //Upgrade building:
            if (GameManager.MultiplayerGame == false)
            {  //if it's a single player game
                UpgradeBuilding = Instantiate(UpgradeBuilding.gameObject, this.transform.position, UpgradeBuilding.transform.rotation).GetComponent<Building>(); //spawn the new building directly
                UpgradeBuilding.PlacedByDefault = true; //so that it won't have to be built.

                if (SelectionMgr.SelectedBuilding == this)
                { //if the previous building was selected
                    SelectionMgr.SelectBuilding(UpgradeBuilding); //select the new one.
                }
                //make sure to pass the configurations of the last building:
                UpgradeBuilding.FactionCapital = FactionCapital; //if it is a faction capital
                UpgradeBuilding.FactionID = FactionID; //pass the faction ID
                FactionMgr.AddBuildingToList(UpgradeBuilding); //add the building to the faction manager's list
            }
            else
            {
                //if it's a MP game, then ask the server to spawn it.
                //send input action to the input manager
                InputVars NewInputAction = new InputVars();
                //mode:
                NewInputAction.SourceMode = (byte)InputSourceMode.Create;

                NewInputAction.Source = UpgradeBuilding.gameObject;
                NewInputAction.Value = 3;

                NewInputAction.InitialPos = this.transform.position;

                InputManager.SendInput(NewInputAction);
            }

            //if the building to upgrade is a current center (it has a border comp):
            if (BorderMgr)
            {
                UpgradeBuilding.gameObject.GetComponent<Border>().ResourcesInRange = CurrentCenter.ResourcesInRange; //pass the resources inside the border to the new upgraded building
                if (CurrentCenter.BuildingsInRange.Count > 0)
                { //pass the buildings list inside the border to the upgraded one.
                    for (int i = 0; i < CurrentCenter.BuildingsInRange.Count; i++)
                    {
                        CurrentCenter.BuildingsInRange[i].CurrentCenter = UpgradeBuilding.GetComponent<Border>(); //change the centers of the buildings inside the border's range
                        UpgradeBuilding.GetComponent<Border>().RegisterBuildingInBorder(CurrentCenter.BuildingsInRange[i]); //register buildings inside the new upgraded border
                    }
                }
            }

            //if this faction does not upgrade all building and the upgrade is direct
            if (Direct == true && UpgradeAllBuildings == false)
            {
                //if this is the local player's faction
                if (FactionID == GameManager.PlayerFactionID)
                {
                    //set the new building in the placement manager:
                    GameMgr.BuildingMgr.ReplaceBuilding(Code, UpradePrefab);
                }
                else
                {
                    //FactionMgr.BuildingMgr.ReplaceBuilding(Code, UpradePrefab);
                }
            }

            //custom event:
            if (GameMgr.Events) GameMgr.Events.OnBuildingCompleteUpgrade(this, Direct);

            DestroyBuilding(true);

        }

        //Check if a resource can be dropped off here:
        public bool CanDrop(string Name)
        {
            if (ResourceDropOff == false) //it's not a drop off building, return false
                return false;
            if (AcceptAllResources == true) //it's a drop off building and it accepts all resources, return true
                return true;
            if (AcceptedResources.Length > 0)
            { //it does accept some resources, look for the target resource
                for (int i = 0; i < AcceptedResources.Length; i++)
                {
                    if (AcceptedResources[i] == Name) //resource found then return true
                        return true;
                }
            }
            return false; //if target resource is not on the list then return false

        }

        //Draw the building's radius in blue
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}