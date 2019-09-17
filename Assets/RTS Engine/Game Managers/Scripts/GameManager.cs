using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

/* Game Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public enum GameStates {Running, Paused, Over, Frozen}

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance = null;
        public string MainMenuScene = "Menu"; //Main menu scene name, this is the scene that will be loaded when the player decides to leave the game.

        [HideInInspector]
        public static GameStates GameState; //game state

        [System.Serializable]
        //The array that holds all the current teams information.
        public class FactionInfo
        {
            public string Name; //Faction's name.

            public FactionTypeInfo TypeInfo; //Type of this faction (the type determines which extra buildings/units can this faction use).

            public Color FactionColor; //Faction's color.
            public bool playerControlled = false; //Is the team controlled by the player, make sure that only one team is controlled by the player.

            public int maxPopulation; //Maximum number of units that can be present at the same time (which can be increased in the game by constructing certain buildings)

            //update the maximum population
            public void UpdateMaxPopulation(int value, bool add = true)
            {
                if (add)
                    maxPopulation += value;
                else
                    maxPopulation = value;

                //custom event trigger:
                CustomEvents.instance.OnMaxPopulationUpdated(this, value);
            }
            //get the maximum population
            public int GetMaxPopulation()
            {
                return maxPopulation;
            }

            int currentPopulation; //Current number of spawned units.

            //update the current population
            public void UpdateCurrentPopulation(int value)
            {
                currentPopulation += value;
                //custom event trigger:
                CustomEvents.instance.OnCurrentPopulationUpdated(this, value);
            }

            //get the current population
            public int GetCurrentPopulation()
            {
                return currentPopulation;
            }

            //get the amount of free slots:
            public int GetFreePopulation ()
            {
                return maxPopulation - currentPopulation;
            }

            public Building CapitalBuilding; //The capital building that MUST be placed in the map before startng the game.
            public Vector3 CapitalPos; //The capital building's position is stored in this variable because when it's a new multiplayer game, the capital buildings are re-spawned in order to be synced in all players screens.
            [HideInInspector]
            public FactionManager FactionMgr; //The faction manager is a component that stores the faction data. Each faction is required to have one.

            public NPCManager npcMgr; //Drag and drop the NPC manager's prefab here.
            private NPCManager npcMgrIns; //the active instance of the NPC manager prefab.

            public NPCManager GetNPCMgrIns () { return npcMgrIns; }

            //init the npc manager:
            public void InitNPCMgr ()
            {
                //make sure there's a npc manager prefab set:
                if (npcMgr == null)
                {
                    Debug.LogError("[Game Manager]: NPC Manager hasn't been set for NPC faction.");
                    return;
                }

                npcMgrIns = Instantiate(npcMgr.gameObject).GetComponent<NPCManager>();

                //set the faction manager:
                npcMgrIns.FactionMgr = FactionMgr;

                //init the npc manager:
                npcMgrIns.Init();

                if (TypeInfo != null) //if this faction has a valid type.
                {
                    //set the building center regulator (if there's any):
                    if (TypeInfo.centerBuilding != null)
                        npcMgrIns.territoryManager_NPC.centerRegulator = npcMgrIns.GetBuildingRegulatorAsset(TypeInfo.centerBuilding);
                    //set the population building regulator (if there's any):
                    if (TypeInfo.populationBuilding != null)
                        npcMgrIns.populationManager_NPC.populationBuilding = npcMgrIns.GetBuildingRegulatorAsset(TypeInfo.populationBuilding);

                    //is there extra buildings to add?
                    if (TypeInfo.extraBuildings.Count > 0)
                    {
                        //go through them:
                        foreach (Building b in TypeInfo.extraBuildings)
                        {
                            if (b != null)
                                npcMgrIns.buildingCreator_NPC.independentBuildingRegulators.Add(npcMgrIns.GetBuildingRegulatorAsset(b));
                            else
                                Debug.LogError("[Game Manager]: Faction " + TypeInfo.Name + " (Code: " + TypeInfo.Code + ") has missing building regulator(s) in extra buildings.");
                        }
                    }
                }
            }

            public bool IsNPCFaction () //is this faction NPC?
            {
                return playerControlled == false && npcMgr != null;
            }

            public bool Lost = false; //true when the faction is defeated and can no longer have an impact on the game.

            //For multiplayer games purpose:

            //UNET:
            public MFactionLobby_UNET MFactionLobby_UNET; //This is the component that holds the basic information of the network player's faction (faction name, color)
            public MFactionManager_UNET MFactionMgr_UNET; //This component is the one that handles communication between the local player and the server.
            public int ConnID_UNET; //Connection of the client to the server is registered here.
        }
        public List<FactionInfo> Factions = new List<FactionInfo>();
        public bool randomPlayerFaction = true;

        private int activeFactionsAmount = 0; //Amount of spawned factions;

        //Peace time:
        public float PeaceTime = 60.0f; //Time (in seconds) after the game starts, when no faction can attack the other.

        public static int PlayerFactionID; //Faction ID of the team controlled by the player.
        public static FactionManager PlayerFactionMgr; //The faction manager component of the faction controlled by the player.
        public static bool allFactionsReady = false; //Are all factions stats ready? 

        //Borders:
        [HideInInspector]
        public int LastBorderSortingOrder = 0; //In order to draw borders and show which order has been set before the other, their objects have different sorting orders.
        [HideInInspector]
        public List<Border> AllBorders; //All the borders in the game are stored in this game.

        //Other scripts:
        [HideInInspector]
        public ResourceManager ResourceMgr;
        [HideInInspector]
        public UIManager UIMgr;
        [HideInInspector]
        public CameraMovement CamMov;
        [HideInInspector]
        public BuildingPlacement BuildingMgr;
        [HideInInspector]
        public SelectionManager SelectionMgr;
        [HideInInspector]
        public CustomEvents Events;
        [HideInInspector]
        public TaskManager TaskMgr;
        [HideInInspector]
        public BuildingPlacement PlacementMgr;
        [HideInInspector]
        public UnitManager UnitMgr;
        [HideInInspector]
        public TerrainManager TerrainMgr;
        [HideInInspector]
        public MovementManager MvtMgr;

        //Multiplayer related:
        public static bool MultiplayerGame = false; //If it's a multiplayer game, this will be true.
        public static int MasterFactionID; //This is the Faction ID that represents the server/host of the multiplayer game.

        public AudioSource GeneralAudioSource; //The audio source where audio will be played generally unless the audio is local. In that case, it will be played 

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

            Time.timeScale = 1.0f; //unfreeze game if it was frozen from a previous game.

            allFactionsReady = false; //faction stats are not ready, yet

            //Randomize player controlled faction:
            RandomizePlayerFaction();

            //Get components:
            CamMov = FindObjectOfType(typeof(CameraMovement)) as CameraMovement; //Find the camera movement script.
            ResourceMgr = FindObjectOfType(typeof(ResourceManager)) as ResourceManager; //Find the resource manager script.
            if (ResourceMgr != null)
                ResourceMgr.gameMgr = this;
            UIMgr = FindObjectOfType(typeof(UIManager)) as UIManager; //Find the UI manager script.
            BuildingMgr = FindObjectOfType(typeof(BuildingPlacement)) as BuildingPlacement;
            Events = FindObjectOfType(typeof(CustomEvents)) as CustomEvents;
            TaskMgr = FindObjectOfType(typeof(TaskManager)) as TaskManager;
            UnitMgr = FindObjectOfType(typeof(UnitManager)) as UnitManager;
            SelectionMgr = FindObjectOfType(typeof(SelectionManager)) as SelectionManager;
            PlacementMgr = FindObjectOfType(typeof(BuildingPlacement)) as BuildingPlacement;
            TerrainMgr = FindObjectOfType(typeof(TerrainManager)) as TerrainManager;
            MvtMgr = FindObjectOfType(typeof(MovementManager)) as MovementManager;

            MultiplayerGame = false; //We start by assuming it's a single player game.

            InitFactionMgrs(); //create the faction managers components for the faction slots.

            InitMultiplayerGame(); //to initialize a multiplayer game.

            InitSinglePlayerGame(); //to initialize a single player game.

            SetPlayerFactionID(); //pick the player faction ID.

            InitFactionCapitals(); //init the faction capitals.

            ResourceMgr.InitFactionResources(); //init resources for factions.

            InitFactions(); //init the faction types.

            //In order to avoid having buildings that are being placed by AI players and units collide, we will ignore physics between their two layers:
            Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Hidden"), LayerMask.NameToLayer("Unit"));

            //Set the amount of the active factions:
            activeFactionsAmount = Factions.Count;

            GameState = GameStates.Running; //the game state is now set to running

            //reaching this point means that all faction info/stats in the game manager are ready:
            allFactionsReady = true;
        }

        //a method that initializes a multiplayer game
        private bool InitMultiplayerGame ()
        {
            //if there's network manager in the scene
            if (NetworkManager_UNET.instance == null)
                return false; //do not proceed.

            //If there's actually a network map manager, it means that the map was loaded from the multiplayer menu, meaning that this is a MP game.

            MultiplayerGame = true; //we now recongize that this a multiplayer game.

            //set the network type in the input manager
            InputManager.NetworkType = InputManager.NetworkTypes.UNET;

            List<MFactionLobby_UNET> FactionLobbyInfos = new List<MFactionLobby_UNET>();
            FactionLobbyInfos.Clear(); //clear this list
            //we'll add the faction lobby components to it using the lobby slots array from the network manager
            foreach (NetworkLobbyPlayer NetworkPlayer in NetworkManager_UNET.instance.lobbySlots) //go through all the lobby slots
            {
                //if the slot is occupied:
                if (NetworkPlayer != null)
                {
                    //add the faction lobby component attached to it to the list:
                    FactionLobbyInfos.Add(NetworkPlayer.gameObject.GetComponent<MFactionLobby_UNET>());
                }
            }

            //This where we will set the settings for all the players:
            //First check if we have enough faction slots available:
            if (FactionLobbyInfos.Count <= Factions.Count)
            {
                //Loop through all the current factions and set up each faction slot:
                for (int i = 0; i < FactionLobbyInfos.Count; i++)
                {
                    MFactionLobby_UNET ThisFaction = FactionLobbyInfos[i]; //this is the faction info that we will get from the faction lobby info.

                    //Set the info for the factions that we will use:
                    Factions[i].Name = ThisFaction.FactionName; //get the faction name
                    Factions[i].FactionColor = ThisFaction.FactionColor; //the faction color
                    //get the initial max population from the network manager (making it the same for all the players).
                    Factions[i].UpdateMaxPopulation(NetworkManager_UNET.instance.Maps[NetworkManager_UNET.instance.CurrentMapID].InitialPopulation, false);
                    Factions[i].Lost = false;

                    Factions[i].MFactionLobby_UNET = ThisFaction; //linking the faction with its lobby info script.
                    Factions[i].CapitalPos = Factions[i].CapitalBuilding.transform.position; //setting the capital pos to spawn the capital building object at later.

                    Factions[i].TypeInfo = NetworkManager_UNET.instance.Maps[NetworkManager_UNET.instance.CurrentMapID].FactionTypes[ThisFaction.FactionTypeID];

                    Factions[i].npcMgr = null; //mark as non NPC faction by setting the npc manager prefab to null
                    
                    //Setting the local player faction ID:
                    if (ThisFaction.isLocalPlayer)
                    { //isLoclPlayer determines which lobby faction info script is owned by the player..
                      //therefore the faction linked to that script is the player controlled one.
                        Factions[i].playerControlled = true;
                    }
                    else
                    {
                        //all other factions will be defined as NPC but in the reality, they are controlled by other players through the network.
                        Factions[i].playerControlled = false;
                    }

                    //Set the master faction ID:
                    if (ThisFaction.IsServer)
                    {
                        MasterFactionID = i;
                    }
                }

                //loop through all the factions and destroy the default capital buildings because the server will spawn new ones for each faction.
                for (int i = 0; i < Factions.Count; i++)
                {
                    DestroyImmediate(Factions[i].CapitalBuilding.gameObject);
                }

                //if there are more slots than required.
                while (FactionLobbyInfos.Count < Factions.Count)
                {
                    Destroy(Factions[Factions.Count - 1].FactionMgr); //destroy the faction manager component
                    //remove the extra slots:
                    Factions.RemoveAt(Factions.Count - 1);
                }

                return true;
            }
            else
            {
                Debug.LogError("[Game Manager]: Not enough slots available for all the factions coming from the multiplayer menu.");
                return false;
            }
        }

        //a method that initializes a single player game:
        private bool InitSinglePlayerGame ()
        {
            //if there's no single player manager then
            if (SinglePlayerManager.instance == null)
                return false; //do not proceed.

            //If there's a map manager script in the scene, it means that we just came from the single player menu, so we need to set the NPC players settings!
            SinglePlayerManager singlePlayerMgr = SinglePlayerManager.instance;
            //This where we will set the NPC settings using the info from the single player manager:
            //First check if we have enough faction slots available:
            if (singlePlayerMgr.Factions.Count <= Factions.Count)
            {
                //loop through the factions slots of this map:
                for (int i = 0; i < singlePlayerMgr.Factions.Count; i++)
                {
                    //Set the info for the factions that we will use:
                    Factions[i].Name = singlePlayerMgr.Factions[i].FactionName; //name
                    Factions[i].FactionColor = singlePlayerMgr.Factions[i].FactionColor; //color
                    Factions[i].playerControlled = singlePlayerMgr.Factions[i].playerControlled; //is this faction controlled by the player? 
                    Factions[i].UpdateMaxPopulation(singlePlayerMgr.Factions[i].InitialPopulation, false); //initial maximum population (which can be increased in the game).
                    Factions[i].TypeInfo = singlePlayerMgr.Factions[i].TypeInfo; //the faction's code.
                    Factions[i].CapitalPos = Factions[i].CapitalBuilding.transform.position; //setting the capital pos to spawn the capital building object at later.

                    Factions[i].Lost = false; //the game just started.

                    Factions[i].npcMgr = (Factions[i].playerControlled == true) ? null : singlePlayerMgr.Factions[i].npcMgr; //set the npc mgr for this faction.
                }

                //if there are more slots than required.
                while (singlePlayerMgr.Factions.Count < Factions.Count)
                {
                    //remove the extra slots:
                    Destroy(Factions[Factions.Count - 1].FactionMgr); //destroy the faction manager component
                    DestroyImmediate(Factions[Factions.Count - 1].CapitalBuilding.gameObject);
                    Factions.RemoveAt(Factions.Count - 1);
                }

                //Destroy the map manager script because we don't really need it anymore:
                DestroyImmediate(singlePlayerMgr.gameObject);

                return true;
            }
            else
            {
                Debug.LogError("[Game Manager]: Not enough slots available for all the factions coming from the single player menu.");
                return false;
            }
        }

        private void InitFactionMgrs()
        {
            for (int i = 0; i < Factions.Count; i++) //go through the factions list
            {
                //create the faction manager components for each faction:
                Factions[i].FactionMgr = gameObject.AddComponent<FactionManager>();
                Factions[i].FactionMgr.FactionID = i;
            }
        }

        //a method that sets the player faction ID
        private bool SetPlayerFactionID()
        {
            PlayerFactionID = -1;
            for (int i = 0; i < Factions.Count; i++) //go through the factions list
            {
                if (Factions[i].playerControlled == true) //is this the player controlled faction?
                {
                    //if we have a player faction ID already:
                    if (PlayerFactionID != -1)
                    {
                        Debug.LogError("[Game Manager]: There's more than one faction labeled as player controlled.");
                        return false;
                    }
                    //if the player faction hasn't been set yet:
                    if (PlayerFactionID == -1)
                    {
                        PlayerFactionID = i;
                        PlayerFactionMgr = Factions[i].FactionMgr; //& set the player faction manager as well
                    }
                }
            }
            if (PlayerFactionID == -1) //if the player faction ID hasn't been set.
            {
                Debug.LogError("[Game Manager]: There's no faction labeled as player controlled.");
                return false;
            }

            return true;
        }

        //initialize the faction capitals.
        private void InitFactionCapitals ()
        {
            //only in single player:
            if (MultiplayerGame == true)
                return;
            for (int i = 0; i < Factions.Count; i++) //go through the factions list
            {
                //if the faction has a valid faction type:
                if (Factions[i].TypeInfo != null)
                {
                    if (Factions[i].TypeInfo.capitalBuilding != null)
                    { //if the faction to a certain type

                        DestroyImmediate(Factions[i].CapitalBuilding.gameObject); //destroy the default capital and spawn another one:
                        //create new faction center:
                        Factions[i].CapitalBuilding = BuildingManager.CreatePlacedInstance(Factions[i].TypeInfo.capitalBuilding, Factions[i].CapitalPos, null, i, true);
                    }
                }

                //mark as faction capital:
                Factions[i].CapitalBuilding.FactionCapital = true;
            }
        }

        void Start()
        {
            //if it's not a MP game:
            if (MultiplayerGame == false)
            {
                //Set the player's initial cam position (looking at the faction's capital building):
                CamMov.LookAt(Factions[PlayerFactionID].CapitalBuilding.transform.position);
                CamMov.SetMiniMapCursorPos(Factions[PlayerFactionID].CapitalBuilding.transform.position);
            }
        }

        //last initialization method for factions: extra buildings and NPC managers init.
        private void InitFactions()
        {
            //no factions?
            if (Factions.Count == 0)
                return; //do not proceed.

            //go through the factions.
            for (int i = 0; i < Factions.Count; i++)
            {
                //Depending on the faction type, add extra units/buildings (if there's actually any) to be created for each faction:
                if (Factions[i].playerControlled == true) //if this faction is player controlled:
                {
                    if (Factions[i].TypeInfo != null) //if this faction has a valid type.
                    {
                        if (Factions[i].TypeInfo.extraBuildings.Count > 0) //if the faction type has extra buildings:
                            foreach (Building b in Factions[i].TypeInfo.extraBuildings)
                            {
                                BuildingMgr.AllBuildings.Add(b); //add the extra buildings so that this faction can use them.
                            }
                    }
                }
                else if (Factions[i].IsNPCFaction() == true) //if this is not controlled by the local player but rather NPC.
                {
                    //Init the NPC Faction manager:
                    Factions[i].InitNPCMgr();
                }

                if (Factions[i].TypeInfo != null) //if this faction has a valid type.
                {
                    Factions[i].FactionMgr.Limits.Clear();
                    //copy the faction type limits in the faction manager:
                    foreach (FactionTypeInfo.FactionLimitsVars LimitElem in Factions[i].TypeInfo.Limits)
                    {
                        FactionTypeInfo.FactionLimitsVars newLimitElem = new FactionTypeInfo.FactionLimitsVars()
                        {
                            Code = LimitElem.Code,
                            MaxAmount = LimitElem.MaxAmount,
                            CurrentAmount = 0
                        };

                        Factions[i].FactionMgr.Limits.Add(newLimitElem);
                    }
                }
            }
        }

        void Update()
        {
            //Peace timer:
            if (PeaceTime > 0)
            {
                PeaceTime -= Time.deltaTime;

                UIMgr.UpdatePeaceTimeUI(PeaceTime); //update the peace timer UI each time.
            }
            if (PeaceTime < 0)
            {
                //when peace timer is ended:
                PeaceTime = 0.0f;

                UIMgr.UpdatePeaceTimeUI(PeaceTime);
            }
        }

        // Are we in peace time?
        public bool InPeaceTime()
        {
            return PeaceTime > 0.0f;
        }

        //Randomize the order of the factions inside the faction order:
        private void RandomizePlayerFaction()
        {
            //if it is allowed to randomize player faction.
            if (randomPlayerFaction == true)
            {
                //swap faction slots randomly.
                for (int i = 0; i < Factions.Count; i++)
                {
                    //randomly a faction ID to swap with
                    int swapID = Random.Range(0, Factions.Count);

                    //if it's not the same faction:
                    if (i != swapID)
                    {
                        //swap capital building & NPC Manager prefabs:
                        RTSHelper.Swap<Building>(ref Factions[swapID].CapitalBuilding, ref Factions[i].CapitalBuilding);
                        RTSHelper.Swap<NPCManager>(ref Factions[swapID].npcMgr, ref Factions[i].npcMgr);
                    }
                }
            }
        }

        //Game state methods:

        //call when a faction is defeated (its capital building has fallen):
        public void OnFactionDefeated(int FactionID)
        {
            //Destroy all buildings and kill all units:

            if (MultiplayerGame == false || FactionID == PlayerFactionID) //only if this a multiplayer game or this is the local player
            {
                //faction manager of the one that has been defeated.
                FactionManager FactionMgr = Factions[FactionID].FactionMgr;

                //go through all the units that this faction owns
                while (FactionMgr.Units.Count > 0)
                {
                    if (FactionMgr.Units[0] != null)
                    {
                        FactionMgr.Units[0].DestroyUnitLocal();
                    }
                    else
                    {
                        FactionMgr.Units.RemoveAt(0);
                    }
                }

                //go through all the buildings that this faction owns
                while (FactionMgr.Buildings.Count > 0)
                {
                    if (FactionMgr.Buildings[0] != null)
                    {
                        FactionMgr.Buildings[0].DestroyBuildingLocal(false);
                    }
                    else
                    {
                        FactionMgr.Buildings.RemoveAt(0);
                    }
                }
            }

            if (Factions[FactionID].IsNPCFaction() == true) //if his is a NPC faction
            {
                //destroy the active instance of the NPC Manager component:
                Destroy(Factions[FactionID].GetNPCMgrIns());
            }

            Factions[FactionID].Lost = true; //ofc.
            activeFactionsAmount--; //decrease the amount of active factions:
            if (Events) Events.OnFactionEliminated(Factions[FactionID]); //call the custom event.

            if (FactionID == PlayerFactionID)
            {
                //If the player is defeated then:
                LooseGame();
            }
            else
            {
                //If one of the other factions was defeated:
                //Check if only the player was left undefeated!
                if (activeFactionsAmount == 1)
                {
                    WinGame(); //Win the game!
                    if (Events) Events.OnFactionWin(Factions[FactionID]); //call the custom event.
                }
            }
        }

        //Win the game:
        public void WinGame()
        {
            //when all the other factions are defeated, 

            //stop whatever the player is doing:
            UIMgr.SelectionMgr.DeselectBuilding();
            UIMgr.SelectionMgr.DeselectUnits();
            UIMgr.SelectionMgr.DeselectResource();

            UIMgr.WinningMenu.SetActive(true); //Show the winning menu

            Time.timeScale = 0.0f; //freeze the game
            GameState = GameStates.Over; //the game state is now set to over
        }

        //called when the player's faction is defeated:
        public void LooseGame()
        {
            UIMgr.LoosingMenu.SetActive(true); //Show the loosing menu

            Time.timeScale = 0.0f; //freeze the game
            GameState = GameStates.Over; //the game state is now set to over
        }

        //allows the player to leave the current game:
        public void LeaveGame()
        {
            if (MultiplayerGame == false)
            {
                //load the main menu if it's a single player game:
                SceneManager.LoadScene(MainMenuScene);
            }
            else
            {
                if (InputManager.NetworkType == InputManager.NetworkTypes.UNET)
                {
                    NetworkManager_UNET.instance.LastDisconnectionType = NetworkManager_UNET.DisconnectionTypes.Left;
                    //if it's a MP game, then back to the network lobby:
                    NetworkManager_UNET.instance.LeaveLobby();
                }
            }
        }

        //Check if this is the local player:
        public bool IsLocalPlayer(GameObject Obj)
        {
            bool LocalPlayer = false;

            if (Obj.gameObject.GetComponent<Unit>())
            {
                if (Obj.gameObject.GetComponent<Unit>().FactionID == PlayerFactionID)
                { //if the unit and local player have the same faction ID
                    LocalPlayer = true;
                }
                else if (Obj.gameObject.GetComponent<Unit>().FreeUnit == true)
                { //if this is a free unit and the local player is the server
                    LocalPlayer = false; //set this initially to false
                    if (MultiplayerGame == true)
                    { //but if it's a MP game
                        if (PlayerFactionID == MasterFactionID)
                        { //and this is the server then set it to true.
                            LocalPlayer = true;
                        }
                    }
                }
            }
            else if (Obj.gameObject.GetComponent<Building>())
            {
                if (Obj.gameObject.GetComponent<Building>().FactionID == PlayerFactionID)
                { //if the building and local player have the same faction ID
                    LocalPlayer = true;
                }
                else if (Obj.gameObject.GetComponent<Building>().FreeBuilding == true)
                { //if this is a free unit and the local player is the server
                    LocalPlayer = false; //set this initially to false
                    if (MultiplayerGame == true)
                    { //but if it's a MP game
                        if (PlayerFactionID == MasterFactionID)
                        { //and this is the server then set it to true.
                            LocalPlayer = true;
                        }
                    }
                }
            }

            return LocalPlayer;
        } 
    }
}
