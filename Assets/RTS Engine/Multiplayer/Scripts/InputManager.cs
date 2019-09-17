using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.AI;

/* Input Manager: script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{

    //Input info

    public enum InputSourceMode { FactionSpawn, Create, Unit, Building, Resource, CustomCommand, Destroy, Group }; //this the type of the input action that will be sent
    public enum InputTargetMode { None, Self, Unit, Building, Resource, Faction, Attack, Portal }; //this the type of the input action that will be sent
    public enum InputCustomMode { Event, MultipleAttacks, Convert, Invisibility, APCDrop, Research, UnitEscape } //a custom input target mode when the input source mode is a custom command

    public class IntVector3
    {
        public int X;
        public int Y;
        public int Z;
    }

    //these are the attributes that an input action has
    public class InputVars
    {
        //Local attributes (won't be shared over network):
        public GameObject Source; //object that launched the command
        public GameObject Target; //target object that will get the command

        //All network types:
        public byte SourceMode;
        public byte TargetMode;

        public int SourceID; //object that launched the command
        public string GroupSourceID; //a string that holds a group of unit sources
        
        public int TargetID; //target object that will get the command

        public Vector3 InitialPos; //initial position of the source obj
        public Vector3 TargetPos; //target position

        public int Value; //extra attribut

        //input owner's faction ID:
        public int FactionID;
    }
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance;
        [HideInInspector]
        public List<GameObject> SpawnablePrefabs = new List<GameObject>();
        [HideInInspector]
        public List<GameObject> SpawnedObjects = new List<GameObject>();

        //which network are we using for multiplayer? 
        public enum NetworkTypes { UNET };
        public static NetworkTypes NetworkType;

        //UNET:
        public static MFactionManager_UNET UNET_Mgr;

        //More settings:
        public float SnapDistance = 0.5f;

        //Other components:
        GameManager GameMgr;
        void Start()
        {
            if (GameManager.MultiplayerGame == false)
            { //if this is not a multiplayer game then destroy this component
                Destroy(this);
                return;
            }

            //make sure there's only one of these in the scene:
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(this);
            }

            GameMgr = GameManager.Instance;
        }

        //this is the only way to communicate between objects and multiplayer faction managers.
        public static void SendInput(InputVars Input)
        {
            if (NetworkType == NetworkTypes.UNET) //if we're using UNET
            {
                if (UNET_Mgr != null) //by checking if there's a valid UNET Faction Manager, we check if the multiplayer game is ready and that the faction has successfully initiliazed.
                {
                    //send the input to UNET after converting all attributes:

                    if (Input.Source) //if there's a source object
                    {
                        if (Input.SourceMode == (byte)InputSourceMode.Create)
                        { //if we're creating an object, then look in the spawnable prefabs list
                            Input.SourceID = InputManager.Instance.SpawnablePrefabs.IndexOf(Input.Source);
                        }
                        else
                        { //if not, check the spawned objects list
                            Input.SourceID = InputManager.Instance.SpawnedObjects.IndexOf(Input.Source);


                            //if the objects don't match:
                            if (InputManager.Instance.SpawnedObjects[Input.SourceID] != Input.Source)
                            {
                                //don't send the input message:
                                return;
                            }
                        }
                    }
                    else
                    {
                        Input.SourceID = -1;
                    }

                    //target
                    if (Input.Target)
                    {
                        Input.TargetID = InputManager.Instance.SpawnedObjects.IndexOf(Input.Target);

                        //if the objects don't match:
                        if (InputManager.Instance.SpawnedObjects[Input.TargetID] != Input.Target)
                        {
                            //don't send the input message:
                            return;
                        }
                    }
                    else
                    {
                        Input.TargetID = -1;
                    }

                    UNET_Mgr.CmdSendInput(Input.SourceMode, Input.TargetMode, Input.SourceID, Input.GroupSourceID, Input.TargetID, Input.InitialPos, Input.TargetPos, Input.Value, GameManager.PlayerFactionID);

                }
            }
            else
            {
                Debug.LogError("Invalid network type!");
            }
        }


        public void LaunchCommand(InputVars Command)
        {
            float Value = (float)Command.Value;

            if (Command.SourceMode == (byte)InputSourceMode.FactionSpawn)
            { //spawning the faction capital building
                Building capitalBuilding = null;

                //search for the capital building to spawn for this faction
                if (GameMgr.Factions[Command.FactionID].TypeInfo != null) //valid faction type info is required
                {
                    if (GameMgr.Factions[Command.FactionID].TypeInfo.capitalBuilding != null)
                    { //if the faction belongs to a certain type

                        capitalBuilding = BuildingManager.CreatePlacedInstance(GameMgr.Factions[Command.FactionID].TypeInfo.capitalBuilding, Command.InitialPos, null, Command.FactionID, true);
                        //assign as a capital faction
                        capitalBuilding.FactionCapital = true;

                        //if this is the local player?
                        if (Command.FactionID == GameManager.PlayerFactionID)
                        {
                            //Set the player's initial cam position (looking at the faction's capital building):
                            GameMgr.CamMov.LookAt(capitalBuilding.transform.position);
                            GameMgr.CamMov.SetMiniMapCursorPos(capitalBuilding.transform.position);
                        }

                        InputManager.Instance.SpawnedObjects.Add(capitalBuilding.gameObject); //add the new object to the list
                    }
                }

                //if the capital building hasn't been set.
                if (capitalBuilding == null)
                    Debug.LogError("[Input Manager]: Capital Building hasn't been for faction type of faction ID: " + Command.FactionID);

                if (Command.FactionID == GameManager.MasterFactionID)
                { //if this is the master client
                  //spawn resources
                    if (GameMgr.ResourceMgr.AllResources.Count > 0)
                    { //make sure there are resources to spawn
                        //add the scene resources to list.
                        GameMgr.ResourceMgr.RegisterResourcesMP(); //here resource objects will also be added to the spawn objects list
                    }
                    //register free units and buildings:
                    if (GameMgr.UnitMgr.FreeUnits.Length > 0)
                    {
                        //go through all free units
                        foreach (Unit FreeUnit in GameMgr.UnitMgr.FreeUnits)
                        {
                            //register them
                            InputManager.Instance.SpawnedObjects.Add(FreeUnit.gameObject);
                        }
                    }
                    if (GameMgr.UnitMgr.FreeUnits.Length > 0)
                    {
                        //go through all free buildings
                        foreach (Building FreeBuilding in GameMgr.BuildingMgr.FreeBuildings)
                        {
                            //register them
                            InputManager.Instance.SpawnedObjects.Add(FreeBuilding.gameObject);
                        }
                    }
                }
            }
            else if (Command.SourceMode == (byte)InputSourceMode.Create)
            { //object creation.

                //get the prefab to create from
                GameObject prefab = InputManager.Instance.SpawnablePrefabs[Command.SourceID];
                GameObject newInstance = null;

                //if the prefab is a unit:
                if(prefab.GetComponent<Unit>())
                {
                    Building unitCreator = null;
                    //set the creator:
                    if (Command.TargetID >= 0)
                    {
                        unitCreator = InputManager.Instance.SpawnedObjects[Command.TargetID].gameObject.GetComponent<Building>();
                    }
                    
                    //create new instance of the unit:
                    newInstance = UnitManager.CreateUnit(prefab.GetComponent<Unit>(), Command.InitialPos, Command.FactionID, unitCreator).gameObject;
                }
                //if the prefab is a building:
                else if(prefab.GetComponent<Building>())
                {
                    /*
                 * 0 -> PlacedByDefault = false & Capital = false
                 * 1 -> PlacedByDefault = true & Capital = false
                 * 2 -> PlacedByDefault = false & Capital = true
                 * 3 -> PlacedByDefault = true & Capital = true
                 * */

                    //TO BE MODIFIED
                    bool placedByDefault = (Value == 1 || Value == 3) ? true : false;
                    bool isCapital = (Value == 2 || Value == 3) ? true : false;

                    Border buildingCenter = null;
                    //set the creator:
                    if (Command.TargetID >= 0)
                    {
                        buildingCenter = InputManager.Instance.SpawnedObjects[Command.TargetID].gameObject.GetComponent<Border>();
                    }

                    newInstance = BuildingManager.CreatePlacedInstance(prefab.GetComponent<Building>(), Command.InitialPos, buildingCenter, Command.FactionID, placedByDefault).gameObject;
                    newInstance.GetComponent<Building>().FactionCapital = isCapital;
                }

                if (newInstance != null) //if a new instance of a unit/building is created:
                    InputManager.Instance.SpawnedObjects.Add(newInstance);
            }
            else if (Command.SourceMode == (byte)InputSourceMode.Destroy)
            {
                if (Command.TargetMode == (byte)InputTargetMode.None)
                { //this means we're destroying an object (unit, building or resource)
                    GameObject SourceObj = InputManager.Instance.SpawnedObjects[Command.SourceID];

                    //destroy the object:
                    if (SourceObj.gameObject.GetComponent<Unit>())
                    {
                        SourceObj.gameObject.GetComponent<Unit>().DestroyUnitLocal();
                    }
                    else if (SourceObj.gameObject.GetComponent<Building>())
                    {
                        SourceObj.gameObject.GetComponent<Building>().DestroyBuildingLocal(((int)Value == 1) ? true : false); // 1 means upgrade, 0 means completely destroy
                    }
                    else if (SourceObj.gameObject.GetComponent<Resource>())
                    {
                        SourceObj.gameObject.GetComponent<Resource>().DestroyResourceLocal(InputManager.Instance.SpawnedObjects[Command.TargetID].GetComponent<GatherResource>());
                    }

                    //Find an alternative
                    //InputManager.Instance.SpawnedObjects.RemoveAt(Command.SourceID); //remove the ojbect to destroy from the spawn objects list
                }
                else if (Command.TargetMode == (byte)InputTargetMode.Faction)
                { //and this means that a faction gets defeated
                    GameMgr.OnFactionDefeated(Command.Value);
                    GameMgr.UIMgr.ShowPlayerMessage(GameMgr.Factions[Command.Value].Name + " (Faction ID:" + Command.Value.ToString() + ") has been defeated.", UIManager.MessageTypes.Error);

                    //If this is the server:
                    if(GameManager.PlayerFactionID == GameManager.MasterFactionID)
                    {
                        int i = 0;
                        bool ClientFound = false;

                        //mark this faction as disconnected:
                        //go through all the client infos as long as the faction that disconnected or lost
                        while(i < UNET_Mgr.ClientsInfo.Count && ClientFound == false)
                        {
                            //if this faction is the one that disconncted
                            if(UNET_Mgr.ClientsInfo[i].FactionID == Command.Value)
                            {
                                UNET_Mgr.ClientsInfo[i].Disconneted = true; //mark the player as disconnected
                                //stop the while loop
                                ClientFound = true;

                                //if the game is frozen:
                                if(GameManager.GameState == GameStates.Frozen)
                                {
                                    //perform a sync test:
                                    UNET_Mgr.SyncTest();
                                }
                            }
                            i++;
                        }
                    }
                }
            }
            else if (Command.SourceMode == (byte)InputSourceMode.CustomCommand)
            {
                if (GameMgr.Events)
                {
                    GameObject Source = null;
                    GameObject Target = null;

                    //get the source and target objects if they exist.
                    if (Command.SourceID >= 0)
                    {
                        Source = InputManager.Instance.SpawnedObjects[Command.SourceID];
                    }
                    if (Command.TargetID >= 0)
                    {
                        Target = InputManager.Instance.SpawnedObjects[Command.TargetID];
                    }

                    //Pre defined custom actions for some events that need to be synced between all clients
                    switch (Command.TargetMode)
                    {
                        //switching attack types:
                        case (byte)InputCustomMode.MultipleAttacks:
                            Source.GetComponent<Unit>().MultipleAttacksMgr.EnableAttackTypeLocal(Command.Value);
                            break;
                            //Converting a unit:
                        case (byte)InputCustomMode.Convert:
                            Source.GetComponent<Unit>().ConvertUnitLocal(Target.GetComponent<Unit>());
                            break;
                            //Toggling invisiblity:
                        case (byte)InputCustomMode.Invisibility:
                            Source.GetComponent<Unit>().InvisibilityMgr.ToggleInvisibility();
                            break;
                            //APC dropping units
                        case (byte)InputCustomMode.APCDrop:
                            Source.GetComponent<APC>().DropOffUnitsLocal(Command.Value);
                            break;
                            //Triggering a research task in a building:
                        case (byte)InputCustomMode.Research:
                            Source.GetComponent<TaskLauncher>().LaunchResearchTaskLocal(Command.Value);
                            break;
                            //Unit escaping
                        case (byte)InputCustomMode.UnitEscape:
                            Source.GetComponent<Unit>().EscapeLocal(Command.TargetPos);
                            break;
                        case (byte)InputCustomMode.Event:
                            GameMgr.Events.OnCustomCommand(Source, Target, Command.InitialPos, Command.TargetPos, Command.Value);
                            break;
                        default:
                            Debug.LogError("Invalid custom command target mode!");
                            break;
                    }
                }
            }
            //if a group of units is the source
            else if(Command.SourceMode == (byte)InputSourceMode.Group)
            {
                //get the units list
                List<Unit> UnitList = StringToUnitList(Command.GroupSourceID);
                //if there's units in the list:
                if (UnitList.Count > 0)
                {
                    //if the target mode is none:
                    if (Command.TargetMode == (byte)InputTargetMode.None)
                    {
                        //move units:
                        MovementManager.Instance.MoveLocal(UnitList, Command.TargetPos, Value, null, InputTargetMode.None);
                    }
                    //if the target mode is attack:
                    else if(Command.TargetMode == (byte)InputTargetMode.Attack)
                    {
                        //group attack:
                        MovementManager.Instance.LaunchAttackLocal(UnitList, InputManager.Instance.SpawnedObjects[Command.TargetID].gameObject, (MovementManager.AttackModes)Value);
                    }
                }
            }
            else if (Command.SourceMode == (byte)InputSourceMode.Unit)
            {
                //invalid source id?
                if (Command.SourceID < 0 || Command.SourceID >= InputManager.Instance.SpawnedObjects.Count)
                {
                    return; //do not proceed.
                }

                Unit SourceUnit = InputManager.Instance.SpawnedObjects[Command.SourceID].gameObject.GetComponent<Unit>();

                //see if we need to snap its position or not.
                if (Vector3.Distance(SourceUnit.transform.position, Command.InitialPos) > SnapDistance)
                {
                    SourceUnit.transform.position = Command.InitialPos;
                }

                if (Command.TargetMode == (byte)InputTargetMode.None)
                { //if there's no target
                  //move unit.
                    MovementManager.Instance.MoveLocal(SourceUnit, Command.TargetPos, Value, null, InputTargetMode.None);
                }
                else
                { //if there's a target object:
                    GameObject TargetObj = null;

                    if (Command.TargetID >= 0 && Command.TargetID < InputManager.Instance.SpawnedObjects.Count)
                    {
                        TargetObj = InputManager.Instance.SpawnedObjects[Command.TargetID].gameObject; //get the target obj
                    }

                    if (Command.TargetMode == (byte)InputTargetMode.Self)
                    { //if the target mode is self
                        SourceUnit.AddHealthLocal(Value, TargetObj); //update unit health
                    }
                    else if (TargetObj != null)
                    {
                        if (Command.TargetMode == (byte)InputTargetMode.Unit)
                        { //if the target mode is a unit
                            if (TargetObj.GetComponent<Unit>().FactionID != SourceUnit.FactionID)
                            { //if the target unit is from another faction
                                if (SourceUnit.ConvertMgr)
                                {
                                    //convert the target unit.
                                    SourceUnit.ConvertMgr.SetTargetUnitLocal(TargetObj.GetComponent<Unit>());
                                }
                            }
                            else
                            { //if hte target unit belongs to the source unit's faction
                              //APC
                                if (TargetObj.GetComponent<APC>())
                                {
                                    SourceUnit.TargetAPC = TargetObj.GetComponent<APC>();
                                    MovementManager.Instance.MoveLocal(SourceUnit, Command.TargetPos, Value, TargetObj, InputTargetMode.Unit);
                                }
                                else if (SourceUnit.HealMgr != null)
                                { //healer:
                                    SourceUnit.HealMgr.SetTargetUnitLocal(TargetObj.GetComponent<Unit>());
                                }
                            }
                        }
                        else if (Command.TargetMode == (byte)InputTargetMode.Building)
                        { //if the target mode is a building
                            if (TargetObj.GetComponent<Building>().FactionID == SourceUnit.FactionID)
                            { //and it belongs to the source's faction
                                if (TargetObj.GetComponent<Building>().Health < TargetObj.GetComponent<Building>().MaxHealth)
                                { //if it doesn't have max health
                                  //construct building
                                    Builder BuilderComp = SourceUnit.gameObject.GetComponent<Builder>();

                                    BuilderComp.SetTargetBuildingLocal(TargetObj.GetComponent<Building>());
                                }
                                else if (TargetObj.GetComponent<APC>())
                                { //if target building is APC
                                    SourceUnit.TargetAPC = TargetObj.GetComponent<APC>();
                                    MovementManager.Instance.MoveLocal(SourceUnit, Command.TargetPos, Value, TargetObj, InputTargetMode.Building);
                                }
                            }
                        }
                        else if (Command.TargetMode == (byte)InputTargetMode.Resource)
                        { //if the target mode is a resource
                            GatherResource ResourceComp = SourceUnit.gameObject.GetComponent<GatherResource>();

                            if (TargetObj.GetComponent<Resource>())
                            { //if the target obj is a resource
                              //collect resources:
                                ResourceComp.SetTargetResourceLocal(TargetObj.GetComponent<Resource>());
                            }
                            //but if the target obj is a building
                            else if (TargetObj.GetComponent<Building>())
                            {
                                //send unit to the drop off building.
                                MovementManager.Instance.MoveLocal(SourceUnit, Command.TargetPos, Value, TargetObj, InputTargetMode.Building);
                            }
                        }
                        else if (Command.TargetMode == (byte)InputTargetMode.Portal)
                        { //if the target mode is a portal
                          //move unit to the portal
                            MovementManager.Instance.MoveLocal(SourceUnit, Command.TargetPos, Value, TargetObj, InputTargetMode.Portal);
                        }
                        else if (Command.TargetMode == (byte)InputTargetMode.Attack)
                        {
                            MovementManager.Instance.LaunchAttackLocal(SourceUnit, InputManager.Instance.SpawnedObjects[Command.TargetID].gameObject, (MovementManager.AttackModes)Value);
                        }
                    }
                }
            }
            else if (Command.SourceMode == (byte)InputSourceMode.Building)
            { //if the source mode is a building
                Building SourceBuilding = InputManager.Instance.SpawnedObjects[Command.SourceID].gameObject.GetComponent<Building>();
                if (SourceBuilding)
                {
                    if (Command.TargetMode != (byte)InputTargetMode.None)
                    { //if there's a target object:
                        GameObject TargetObj = null; //get the target obj
                        if (Command.TargetID >= 0)
                        {
                            TargetObj = InputManager.Instance.SpawnedObjects[Command.TargetID].gameObject; //get the target obj
                        }
                        if (Command.TargetMode == (byte)InputTargetMode.Self)
                        { //if the target mode is self = update health
                            SourceBuilding.AddHealthLocal(Value, TargetObj);
                        }
                        else if (Command.TargetMode == (byte)InputTargetMode.Attack && TargetObj != null)
                        { //if the target mode is attack = attack unit
                          //Attack.
                            SourceBuilding.AttackMgr.SetAttackTargetLocal(TargetObj);
                        }
                    }
                }
            }
            else if (Command.SourceMode == (byte)InputSourceMode.Resource)
            { //if the source mode is a resource
                Resource SourceResource = InputManager.Instance.SpawnedObjects[Command.SourceID].gameObject.GetComponent<Resource>();
                if (Command.TargetMode != (byte)InputTargetMode.None)
                { //if there's a target object:
                    if (Command.TargetMode == (byte)InputTargetMode.Self)
                    { //if the target mode is self = update health
                        GameObject TargetObj = InputManager.Instance.SpawnedObjects[Command.TargetID].gameObject; //get the target obj
                        SourceResource.AddResourceAmountLocal(Value, TargetObj.GetComponent<GatherResource>());
                    }
                }
            }
        }

        public static IntVector3 Vector3ToIntVector3(Vector3 Vector)
        {
            IntVector3 Result = new IntVector3();
            Result.X = (int)Vector.x;
            Result.Y = (int)Vector.y;
            Result.Z = (int)Vector.z;

            return Result;
        }

        //convert a unit list into a string:
        public static string UnitListToString (List<Unit> UnitList)
        {
            string ResultString = "";
            if(UnitList.Count > 0)
            {
                //go through the unit's list
                foreach(Unit U in UnitList)
                {
                    //get the ID of the unit in the list
                    int UnitID = InputManager.Instance.SpawnedObjects.IndexOf(U.gameObject);

                    //make sure the objects match
                    if (InputManager.Instance.SpawnedObjects[UnitID] == U.gameObject)
                    {
                        //add to the string:
                        ResultString += UnitID.ToString() + ",";
                    }
                }

                ResultString = ResultString.TrimEnd(',');
            }
            return ResultString;
        }

        //convert a string containing the IDs of units into a unit list:
        public static List<Unit> StringToUnitList (string IDString)
        {
            List<Unit> ResultList = new List<Unit>();
            string[] IDs = IDString.Split(','); //remove the commas from the ID strings
            if(IDs.Length > 0) //go through the IDs (still in string)
            {
                for(int i = 0; i < IDs.Length; i++)
                {
                    //convert each string to an int and then get the correspondant Unit from the spawned objects list
                    ResultList.Add(InputManager.Instance.SpawnedObjects[Int32.Parse(IDs[i])].GetComponent<Unit>());
                }
            }

            return ResultList;
        }
    }
}
