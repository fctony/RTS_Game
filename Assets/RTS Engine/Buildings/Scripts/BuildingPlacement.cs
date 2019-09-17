using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;

/* Building Placement script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class BuildingPlacement : MonoBehaviour
    {
        public static BuildingPlacement instance = null;

        public List<Building> AllBuildings;

        public float BuildingYOffset = 0.01f; //The value added to the building position on the Y axis so that the building object is not stuck inside the terrain.

        [HideInInspector]
        public Building CurrentBuilding; //Holds the current building to place info.

        public static bool IsBuilding = false;

        //Key to keep spawning buildings:
        public bool HoldAndSpawn = false;
        public KeyCode HoldAndSpawnKey = KeyCode.LeftShift;
        [HideInInspector]
        public int LastBuildingID;

        public Building[] FreeBuildings; //buildings that don't belong to any faction here.
        public Color FreeBuildingSelectionColor = Color.black;

        public AudioClip SendToBuildAudio; //This audio is played when the player sends a group of units to fix/construct a building.
        public AudioClip PlaceBuildingAudio; //Audio played when a building is placed.

        //Scripts:
        ResourceManager ResourceMgr;
        SelectionManager SelectionMgr;
        TerrainManager TerrainMgr;
        GameManager GameMgr;


        void Awake()
        {
            //one instance of this component per map
            if(instance == null)
            {
                instance = this;
            }
            else if(instance != this)
            {
                Destroy(instance);
            }

            IsBuilding = false;

            //find the scripts that we will need:
            GameMgr = GameManager.Instance;
            ResourceMgr = GameMgr.ResourceMgr;
            SelectionMgr = GameMgr.SelectionMgr;
            TerrainMgr = GameMgr.TerrainMgr;
        }


        void Update()
        {
            if (CurrentBuilding != null) //If we are currently attempting to place a building on the map
            {
                IsBuilding = true; //it means we are informing other scripts that we are placing a building.

                MoveBuilding(); //keep moving the building by following the mouse

                if (Input.GetMouseButtonDown(1)) //If the player preses the right mouse button.
                {
                    CancelBuildingPlacement(); //stop placing the building
                }
                else if (CurrentBuilding.CanPlace == true) //If the player can place the building at its current position:
                {
                    if (Input.GetMouseButtonDown(0)) //If the player preses the left mouse button
                    {
                        if (CheckBuildingResources(CurrentBuilding) == true) //Does the player's team have all the required resources to build this building
                        {
                            PlaceBuilding(); //place the building.

                            //Show the tasks again for the builders again:
                            if (SelectionMgr.SelectedUnits.Count > 0 && IsBuilding == false)
                            {
                                SelectionMgr.UIMgr.UpdateTaskPanel();
                            }
                        }
                        else
                        {
                            //Inform the player that he doesn't have enough resources.
                            GameMgr.UIMgr.ShowPlayerMessage("Not enough resources for this building", UIManager.MessageTypes.Error);
                        }
                    }
                }
            }
            else
            {
                if (IsBuilding == true) IsBuilding = false;
            }
        }

        //move the building by following the mouse:
        void MoveBuilding()
        {
            //using a raycheck, we will make the building to place, follow the mouse position and stay on top of the terrain.
            Ray RayCheck = Camera.main.ScreenPointToRay(Input.mousePosition);

            RaycastHit Hit;

            if (Physics.Raycast(RayCheck, out Hit, 80.0f, TerrainMgr.GroundTerrainMask))
            {
                //depending on the height of the terrain, we will place the building on it.
                Vector3 BuildingPos = Hit.point;
                //make sure that the building position on the y axis stays inside the min and max height interval:
                BuildingPos.y += BuildingYOffset;
                if (CurrentBuilding.transform.position != BuildingPos)
                {
                    CurrentBuilding.NewPos = true; //inform the building's comp that we have moved it so that it checks whether the new position is suitable or not.
                }
                CurrentBuilding.transform.position = BuildingPos; //set the new building's pos.
            }
        }

        //abort placing the building:
        void CancelBuildingPlacement()
        {
            //Call the stop building placement custom event:
            if (GameMgr.Events)
                GameMgr.Events.OnBuildingStopPlacement(CurrentBuilding);

            //Abort the building process
            Destroy(CurrentBuilding.gameObject);
            CurrentBuilding = null;

            //Show the tasks again for the builders again:
            if (SelectionMgr.SelectedUnits.Count > 0)
            {
                SelectionMgr.UIMgr.UpdateTaskPanel();
            }
        }

        //the method that allows us to place the building
        void PlaceBuilding()
        {
            TakeBuildingResources(CurrentBuilding); //Remove the resources needed to create the building.

            if (GameManager.MultiplayerGame == false)
            { //if it's a single player game.
                BuildingManager.CreatePlacedInstance(AllBuildings[LastBuildingID], CurrentBuilding.transform.position, CurrentBuilding.CurrentCenter, GameManager.PlayerFactionID); //place the building
            }
            else
            { //in case it's a multiplayer game:

                //ask the server to spawn the building for all clients:
                //send input action to the input manager
                InputVars NewInputAction = new InputVars();
                //mode:
                NewInputAction.SourceMode = (byte)InputSourceMode.Create;

                NewInputAction.Source = AllBuildings[LastBuildingID].gameObject;
                NewInputAction.InitialPos = CurrentBuilding.transform.position;
                NewInputAction.Target = CurrentBuilding.CurrentCenter.gameObject;
                NewInputAction.Value = 0;

                InputManager.SendInput(NewInputAction);
            }

            //remove instance that was being used to place building
            Destroy(CurrentBuilding.gameObject);
            CurrentBuilding = null;

            //Play building placement audio
            AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, PlaceBuildingAudio, false);

            //if holding and spawning is enabled and the player is holding the right key to do that:
            if (HoldAndSpawn == true && Input.GetKey(HoldAndSpawnKey))
            {
                //start placing the same building again
                StartPlacingBuilding(LastBuildingID);
            }
            else
            {
                //Show the tasks again for the builders again:
                if (SelectionMgr.SelectedUnits.Count > 0)
                {
                    SelectionMgr.UIMgr.UpdateTaskPanel();
                }
            }
        }

        //This checks if we have enough resources to build this building or not.
        public bool CheckBuildingResources(Building CheckBuilding)
        {
            //if the god mode is enabled
            if (GodMode.Enabled == true)
            {
                //always return true
                return true;
            }

            if (CheckBuilding.BuildingResources.Length > 0)
            {
                for (int i = 0; i < CheckBuilding.BuildingResources.Length; i++) //Loop through all the requried resources:
                {
                    //Check if the team resources are lower than one of the demanded amounts:
                    if (ResourceMgr.GetResourceAmount(GameManager.PlayerFactionID, CheckBuilding.BuildingResources[i].Name) < CheckBuilding.BuildingResources[i].Amount)
                    {
                        return false; //If yes, return false.
                    }
                }
                return true; //If not, return true.
            }
            else //This means that no resource are required to build this building.
            {
                return true;
            }
        }

        //a method that takes the buildings resources.
        public void TakeBuildingResources(Building CheckBuilding)
        {
            //if the god mode is enabled
            if (GodMode.Enabled == true)
            {
                //take no resources
                return;
            }

            if (CheckBuilding.BuildingResources.Length > 0) //If the building requires resources:
            {
                for (int i = 0; i < CheckBuilding.BuildingResources.Length; i++) //Loop through all the requried resources:
                {
                    //Remove the demanded resources amounts:
                    ResourceMgr.AddResource(GameManager.PlayerFactionID, CheckBuilding.BuildingResources[i].Name, -CheckBuilding.BuildingResources[i].Amount);
                }
            }
        }

        public void StartPlacingBuilding(int BuildingID)
        {
            //make sure the building hasn't reached its limits:
            if (!GameMgr.Factions[GameManager.PlayerFactionID].FactionMgr.HasReachedLimit(AllBuildings[BuildingID].Code))
            {
                //make sure that all required buildings to place this one are here:
                if (GameMgr.Factions[GameManager.PlayerFactionID].FactionMgr.CheckRequiredBuildings(AllBuildings[BuildingID]))
                {
                    //make sure we have enough resources
                    if (CheckBuildingResources(AllBuildings[BuildingID]) == true)
                    {
                        //Spawn the building for the player to place on the map:
                        GameObject BuildingClone = (GameObject)Instantiate(AllBuildings[BuildingID].gameObject, new Vector3(0, 0, 0), AllBuildings[BuildingID].transform.rotation);
                        LastBuildingID = BuildingID;

                        CurrentBuilding = BuildingClone.GetComponent<Building>(); //this is the building to place.

                        CurrentBuilding.InitPlacementInstance(GameManager.PlayerFactionID); //initiliaze the placement instance.

                        //Set the position of the new building:
                        RaycastHit Hit;
                        Ray RayCheck = Camera.main.ScreenPointToRay(Input.mousePosition);

                        if (Physics.Raycast(RayCheck, out Hit))
                        {
                            Vector3 BuildingPos = Hit.point;
                            BuildingPos.y += BuildingYOffset;
                            BuildingClone.transform.position = BuildingPos;
                        }

                        IsBuilding = true; //player is now constructing

                        //Disable the selection box:
                        SelectionMgr.CreatedSelectionBox = false;
                        SelectionMgr.SelectionBoxEnabled = false;

                        //hide task buttons:
                        GameMgr.UIMgr.HideTaskButtons();

                        //hide tooltip info:
                        GameMgr.UIMgr.HideTooltip();

                        //Call the start building placement custom event:
                        if (GameMgr.Events)
                            GameMgr.Events.OnBuildingStartPlacement(CurrentBuilding);
                    }
                    else
                    {
                        //Inform the player that he can't place this building because he's lacking resources.
                        GameMgr.UIMgr.ShowPlayerMessage("Not enough resources to launch task!", UIManager.MessageTypes.Error);
                    }
                }
                else
                {
                    //building requirement fault, send message:
                    GameMgr.UIMgr.ShowPlayerMessage("Building requirements for " + AllBuildings[BuildingID].Name + " are missing.", UIManager.MessageTypes.Error);
                }
            }
            else
            {
                //building limit reached, send message to player:
                GameMgr.UIMgr.ShowPlayerMessage("Building " + AllBuildings[BuildingID].Name + " has reached its placement limit", UIManager.MessageTypes.Error);
            }
        }

        //replace an existing building in the all building list that the faction can spawn with another building (usually after having an age upgrade).
        public void ReplaceBuilding(string Code, Building NewBuilding)
        {
            if (Code == NewBuilding.Code) //if both buildings have the same codes.
                return;

            if (AllBuildings.Count > 0)
            { //go through all the buildings in the list
                int i = 0;
                bool Found = false;
                while (i < AllBuildings.Count && Found == false)
                {
                    if (AllBuildings[i].gameObject.GetComponent<Building>().Code == Code)
                    { //when the building is found
                        AllBuildings.RemoveAt(i);//remove it
                        if (!AllBuildings.Contains(NewBuilding))
                        { //make sure we don't have the same building already in the list
                            AllBuildings.Insert(i, NewBuilding); //place the new building in the same position
                        }
                        Found = true;
                    }
                    i++;
                }
            }
        }
    }
}