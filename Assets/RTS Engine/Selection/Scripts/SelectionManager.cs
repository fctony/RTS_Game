﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.AI;

/* Selection Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class SelectionManager : MonoBehaviour
    {

        [Header("General Selection Settings:")]
        public LayerMask RaycastLayerMask; //Make sure the raycast doesn't include the "Building" and "Resource" layers.

        //Selection textures for buildings, units and resources:
        public Texture2D UnitSelectionTexture;
        public Texture2D BuildingSelectionTexture;
        public Texture2D ResourceSelectionTexture;

        [HideInInspector]
        public Building SelectedBuilding;
        [HideInInspector]
        public List<Unit> SelectedUnits;
        [HideInInspector]
        public Resource SelectedResource;

        public KeyCode MultipleSelectionKey = KeyCode.LeftShift; //Key used to select multiple units.
        [HideInInspector]
        public bool MultipleSelectionKeyDown = false;

        //double selection range:
        public float DoubleClickSelectSize = 10.0f;

        [Header("Selection Box Settings:")]
        //Selection Box:
        public Image SelectionBox;
        public RectTransform Canvas;
        //Hold the first and last mouse position when creating the selection box:
        Vector3 FirstMousePos;
        Vector3 LastMousePos;
        [HideInInspector]
        public bool CreatedSelectionBox = false;
        [HideInInspector]
        public bool SelectionBoxEnabled = false;
        public float MinBoxSize = 1.0f; //Holds the minimal selection box to draw it.

        [Header("Selection Flash Settings:")]
        public float FlashTime = 1.0f; //This is the total time at which the building/resource/unit selection texture will be chosen after the player sent units to construct/collect it.
        public Color FriendlyFlashColor = Color.green; //The color of a friendly building/resource/unit selection texture when it starts flashing.
        public Color EnemyFlashColor = Color.red; //the color of an enemy building/unit selection texture when it's flashing due to an attack order
        public float FlashRepeatTime = 0.2f; //The selection texture flash repeat time.

        [Header("Selection Group Settings:")]
        //Selection Groups:
        public bool EnableSelectionGroups = true;
        public List<Unit>[] SelectionGroups;
        public AudioClip GroupAssignedAudio;
        public AudioClip GroupEmptyAudio;

        //Raycast: We'll need those two many times.
        RaycastHit Hit;
        Ray RayCheck;

        GameManager GameMgr;
        [HideInInspector]
        public UIManager UIMgr;
        ResourceManager ResourceMgr;
        BuildingPlacement BuildingMgr;
        TerrainManager TerrainMgr;
        MovementManager MvtMgr;
        TaskManager TaskMgr;

        void Awake()
        {
            GameMgr = GameManager.Instance;
            UIMgr = GameMgr.UIMgr;
            ResourceMgr = GameMgr.ResourceMgr;
            BuildingMgr = GameMgr.BuildingMgr;
            TerrainMgr = GameMgr.TerrainMgr;
            TaskMgr = GameMgr.TaskMgr;
            MvtMgr = MovementManager.Instance;

            MultipleSelectionKeyDown = false;

            if (EnableSelectionGroups == true)
            { //if there's selection groups:
              //initialize them:
                SelectionGroups = new List<Unit>[10];
                //go through all of them and make sure they're empty
                for (int i = 0; i < SelectionGroups.Length; i++)
                {
                    SelectionGroups[i] = new List<Unit>();
                    SelectionGroups[i].Clear();
                }
            }
        }

        void Update()
        {
            //If the game is not running
            if (GameManager.GameState != GameStates.Running)
            {
                return; //don't proceed
            }

            //Checking if the selection key is held down or not!
            MultipleSelectionKeyDown = Input.GetKey(MultipleSelectionKey);

            if (BuildingPlacement.IsBuilding == false)
            { //If we are not placing a building
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    //We check if the player hasn't clicked a building or a unit by drawing a ray from the mouse position:
                    if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(0))
                    {
                        RayCheck = Camera.main.ScreenPointToRay(Input.mousePosition);
                        if (Physics.Raycast(RayCheck, out Hit, Mathf.Infinity, RaycastLayerMask.value))
                        {
                            //If the ray doesn't hit a building, a unit object and any UI object:
                            SelectionObj HitObj = Hit.transform.gameObject.GetComponent<SelectionObj>();
                            Unit HitUnit = null;
                            Building HitBuilding = null;
                            Resource HitResource = null;
                            if (HitObj != null)
                            {
                                HitUnit = HitObj.MainObj.GetComponent<Unit>();
                                HitBuilding = HitObj.MainObj.GetComponent<Building>();
                                HitResource = HitObj.MainObj.GetComponent<Resource>();
                            }

                            if (Input.GetMouseButtonDown(1))
                            {
                                if (TaskMgr.AwaitingTaskType != TaskManager.TaskTypes.Null)
                                { //if we click with the right mouse button while having an awaiting component task..
                                    TaskMgr.ResetAwaitingTaskType(); //reset it.
                                }
                                else
                                {
                                    if (HitObj != null)
                                    {
                                        if (HitBuilding != null)
                                        {
                                            ActionOnBuilding(HitBuilding, TaskManager.TaskTypes.Null);
                                        }
                                        else if (HitResource != null)
                                        {
                                            ActionOnResource(HitResource, TaskManager.TaskTypes.Null);
                                        }
                                        else if (HitUnit != null)
                                        {
                                            ActionOnUnit(HitUnit, TaskManager.TaskTypes.Null);
                                        }
                                    }
                                    else
                                    {
                                        //Moving selected units:
                                        //The position which the unit will move to will be determined by a ray coming out from the mouse to the terrain object. 
                                        if (SelectedUnits.Count > 0)
                                        {
                                            //if the terrain was hit (if the object that is hit includes has a terrain layer) and the selected units belong to the player
                                            if (TerrainMgr.IsTerrainTile(Hit.transform.gameObject) && SelectedUnits[0].FactionID == GameManager.PlayerFactionID)
                                            {
                                                MvtMgr.Move(SelectedUnits, Hit.point, 0.0f, null, InputTargetMode.None);
                                            }
                                        }
                                    }

                                    //Goto position:
                                    //If we're currently selecting a building:
                                    if (SelectedBuilding != null)
                                    {
                                        //If the building has been already placed.
                                        if (SelectedBuilding.IsBuilt == true)
                                        {
                                            //If it has a go to position:
                                            if (SelectedBuilding.Rallypoint != null && SelectedBuilding.FactionID == GameManager.PlayerFactionID)
                                            {
                                                if (HitObj == null && TerrainMgr.IsTerrainTile(Hit.transform.gameObject))
                                                { //if the player clicked on an empty point in the map's terrain
                                                  //Move the goto position:
                                                    SelectedBuilding.GotoPosition.position = Hit.point;
                                                    SelectedBuilding.Rallypoint = SelectedBuilding.GotoPosition;
                                                }
                                                else
                                                { //the player has clicked on either a building or a resource:
                                                    if (HitBuilding)
                                                    { //if it's a building
                                                      //check if the building belongs to this faciton
                                                        if (HitBuilding.FactionID == GameManager.PlayerFactionID)
                                                        {
                                                            SelectedBuilding.Rallypoint = HitBuilding.transform;
                                                            SelectedBuilding.GotoPosition.position = HitBuilding.transform.position;

                                                            FlashSelection(HitBuilding.gameObject, true);
                                                        }
                                                    }
                                                    else if (HitResource)
                                                    {
                                                        if (HitResource.FactionID == GameManager.PlayerFactionID)
                                                        {
                                                            SelectedBuilding.Rallypoint = HitResource.transform;
                                                            SelectedBuilding.GotoPosition.position = HitResource.transform.position;

                                                            FlashSelection(HitResource.gameObject, true);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else if (Input.GetMouseButtonDown(0))
                            {
                                //if awaiting task type is set to null (not chosen)
                                if (TaskMgr.AwaitingTaskType == TaskManager.TaskTypes.Null)
                                {
                                    //only deselect units if the multiple selection key is not down
                                    if (MultipleSelectionKeyDown == false || HitUnit == null)
                                    {
                                        DeselectUnits();
                                    }
                                    DeselectBuilding(); //deselect buildings
                                }
                                DeselectResource(); //deselect the resource

                                if (HitObj != null)
                                {
                                    //If we selected a building or a resource, update the selection info:
                                    if (HitBuilding)
                                    {
                                        if (TaskMgr.AwaitingTaskType != TaskManager.TaskTypes.Null)
                                        { //if the player assigned selected unit(s) to do a component task:
                                            ActionOnBuilding(HitBuilding, TaskMgr.AwaitingTaskType);
                                        }
                                        else
                                        {
                                            HitObj.SelectObj();
                                        }
                                    }
                                    else if (HitResource)
                                    {
                                        if (TaskMgr.AwaitingTaskType != TaskManager.TaskTypes.Null)
                                        { //if the player assigned selected unit(s) to do a component task:
                                            ActionOnResource(HitResource, TaskMgr.AwaitingTaskType);
                                        }
                                        else
                                        {
                                            HitObj.SelectObj();
                                        }
                                    }
                                    else if (HitUnit)
                                    {
                                        if (TaskMgr.AwaitingTaskType != TaskManager.TaskTypes.Null)
                                        { //if the player assigned selected unit(s) to do a component task:
                                            ActionOnUnit(HitUnit, TaskMgr.AwaitingTaskType);
                                        }
                                        else
                                        {
                                            HitObj.SelectObj();
                                        }
                                    }
                                }
                                else if (TaskMgr.AwaitingTaskType == TaskManager.TaskTypes.Mvt)
                                { //if the pending comp task is a mvt one
                                    //if the terrain was hit (if the object that is hit includes has a terrain layer) and the selected units belong to the player
                                    if (TerrainMgr.IsTerrainTile(Hit.transform.gameObject) && SelectedUnits[0].FactionID == GameManager.PlayerFactionID)
                                    {
                                        MvtMgr.Move(SelectedUnits, Hit.point, 0.0f, null, InputTargetMode.None);
                                    }
                                }

                                //resets the awaiting task type
                                if (TaskMgr.AwaitingTaskType != TaskManager.TaskTypes.Null)
                                {
                                    TaskMgr.ResetAwaitingTaskType();
                                }
                            }
                        }
                    }
                }


                /***********************************************************************************************************************************************/
                //Selection Box:

                if (SelectionBox != null)
                { //making sure that there's a valid selection box assigned
                    if (Input.GetMouseButton(0))
                    { //If the player is holding the left mouse button.
                      //If we haven't created the selection box yet
                        if (CreatedSelectionBox == false)
                        {
                            //Create the selection and save the initial mouse position on the screen:
                            FirstMousePos = Input.mousePosition;
                            CreatedSelectionBox = true;
                        }
                        //Check if the box size is above the minimal size.
                        if (Vector3.Distance(FirstMousePos, Input.mousePosition) > MinBoxSize)
                        {
                            SelectionBoxEnabled = true;
                            //Activate the selection box object if it's not activated.
                            if (SelectionBox.gameObject.activeSelf == false)
                            {
                                SelectionBox.gameObject.SetActive(true);
                            }

                            LastMousePos = Input.mousePosition; //Always save the last mouse position.

                            //Calculate the box's size in the canvas:
                            Vector3 CurrentMousePosUI = Input.mousePosition - Canvas.localPosition;
                            Vector3 FirstMousePosUI = FirstMousePos - Canvas.localPosition;

                            //Set the selection box size in the canvas.
                            SelectionBox.GetComponent<RectTransform>().sizeDelta = new Vector2(Mathf.Abs(CurrentMousePosUI.x - FirstMousePosUI.x), Mathf.Abs(CurrentMousePosUI.y - FirstMousePosUI.y));

                            Vector3 Center = (FirstMousePos + Input.mousePosition) / 2 - Canvas.localPosition; //Calculate the center of the selection box.
                            SelectionBox.GetComponent<RectTransform>().localPosition = Center; //Set the selection position.
                        }
                    }

                    //If the player releases the mouse button:
                    if (Input.GetMouseButtonUp(0) && CreatedSelectionBox == true)
                    {
                        CreatedSelectionBox = false;
                        SelectionBoxEnabled = false;

                        //We'll check if he had selected units:
                        if (Vector3.Distance(FirstMousePos, Input.mousePosition) > MinBoxSize)
                        {
                            bool SelectionBoxReady = false; //True when we are able to detect objects inside the selection box.

                            Vector3 Center = Vector3.zero;
                            Vector3 Corner = Vector3.zero;

                            //We'll use a raycast which will detect the terrain objects and then allow us to look for the objects inside the selection box.
                            RaycastHit[] Hits;
                            RayCheck = Camera.main.ScreenPointToRay((FirstMousePos + LastMousePos) / 2);
                            Hits = Physics.RaycastAll(RayCheck, Mathf.Infinity);

                            //First we'll send a raycast from the center of the selection box.
                            if (Hits.Length > 0)
                            {
                                for (int i = 0; i < Hits.Length; i++)
                                {
                                    if (Hits[i].transform.gameObject == TerrainMgr.FlatTerrain)
                                    {
                                        Center = Hits[i].point;
                                        Center.y = 0.0f;

                                        SelectionBoxReady = true;

                                        //The raycast hit the terrain object so we save the hit point position and move to the next step.
                                    }
                                }
                            }


                            //If the last step was successfully completed.
                            if (SelectionBoxReady == true)
                            {

                                DeselectBuilding();

                                SelectionBoxReady = false;

                                //We'll send another raycast from one of the corners of the selectin box.
                                RayCheck = Camera.main.ScreenPointToRay(FirstMousePos);
                                Hits = Physics.RaycastAll(RayCheck, Mathf.Infinity);

                                if (Hits.Length > 0)
                                {
                                    for (int i = 0; i < Hits.Length; i++)
                                    {
                                        if (Hits[i].transform.gameObject == TerrainMgr.FlatTerrain)
                                        {
                                            Corner = Hits[i].point;
                                            Corner.y = 0.0f;

                                            SelectionBoxReady = true;

                                            //Step successful when the raycast hit the terrain object, so we save the hit point position.
                                        }
                                    }
                                }

                                //If the player is holding the multiple selection key, we'll keep the previosuly selected units, if not we won't
                                if (MultipleSelectionKeyDown == false)
                                {
                                    DeselectUnits();
                                }

                                //If both of the above steps have been successful: 
                                if (SelectionBoxReady == true)
                                {
                                    //All set.

                                    //Search for all objects in range of the selection box.
                                    Collider[] ObjsInSelection = Physics.OverlapSphere(Center, Vector3.Distance(Center, Corner));
                                    if (ObjsInSelection.Length > 0)
                                    {
                                        //Create a new list to select units in range: 
                                        List<Unit> UnitsInRange = new List<Unit>();
                                        for (int i = 0; i < ObjsInSelection.Length; i++)
                                        {
                                            //Search for the units objects only and add them to the previous list:
                                            Unit SelectedUnit = ObjsInSelection[i].gameObject.GetComponent<Unit>();
                                            if (SelectedUnit)
                                            {
                                                //Only select units belonging to the player's team.
                                                if (SelectedUnit.FactionID == GameManager.PlayerFactionID)
                                                {
                                                    UnitsInRange.Add(SelectedUnit);
                                                }
                                            }
                                        }



                                        //Check if there are units in the selection box:
                                        if (UnitsInRange.Count > 0)
                                        {
                                            Vector3 BoxCenter = (LastMousePos + FirstMousePos) / 2 - Canvas.localPosition;

                                            if (FirstMousePos.x >= LastMousePos.x)
                                            {
                                                if (FirstMousePos.y >= LastMousePos.y)
                                                {
                                                    FirstMousePos = LastMousePos;
                                                }
                                                else
                                                {
                                                    FirstMousePos.x = LastMousePos.x;
                                                }
                                            }

                                            if (FirstMousePos.x <= LastMousePos.x)
                                            {
                                                if (FirstMousePos.y >= LastMousePos.y)
                                                {
                                                    FirstMousePos.y = LastMousePos.y;
                                                }
                                            }

                                            Vector3 BoxCorner = FirstMousePos - Canvas.localPosition;



                                            for (int i = 0; i < UnitsInRange.Count; i++)
                                            {
                                                //Convert the unit's world position to screen position:
                                                Vector3 ScreenPos = Camera.main.WorldToScreenPoint(UnitsInRange[i].transform.position);
                                                Vector3 CanvasPos = ScreenPos - Canvas.localPosition;
                                                if (CanvasPos.x > BoxCorner.x && CanvasPos.x <= (BoxCorner.x + Mathf.Abs(BoxCorner.x - BoxCenter.x) * 2) && CanvasPos.y >= BoxCorner.y && CanvasPos.y <= (BoxCorner.y + Mathf.Abs(BoxCorner.y - BoxCenter.y) * 2))
                                                {
                                                    //If unit is in selection box, then add it to the selected units:
                                                    SelectUnit(UnitsInRange[i], true);
                                                }
                                            }


                                            //Only play the selection audio if the unit belongs to the player's faction:
                                            if (SelectedUnits.Count > 0)
                                            {
                                                AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, SelectedUnits[0].SelectionAudio, false);
                                            }
                                        }

                                        UnitsInRange.Clear();
                                    }


                                }
                                else
                                {
                                    // Error when we couldnt hit the terrain object with the ray.
                                }

                            }
                            else
                            {
                                // Error when we couldnt hit the terrain object with the ray.
                            }

                        }
                        //Desactivate the selection box:
                        SelectionBox.gameObject.SetActive(false);
                    }
                }

                /***********************************************************************************************************************************************/
                //Selection Groups:
                if (EnableSelectionGroups == true)
                { //if enabled
                    int NumKey = GetNumKey();
                    if (NumKey != -1)
                    { //if key is pressed
                      //if there are units selected:
                        if (SelectedUnits.Count > 0)
                        {
                            //assign this new group to them
                            SelectionGroups[NumKey].Clear();
                            SelectionGroups[NumKey].AddRange(SelectedUnits);

                            //play audio:
                            if (GroupAssignedAudio)
                                AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, GroupAssignedAudio, false);

                            //inform player about selecting:
                            UIMgr.ShowPlayerMessage("Unit selection group set.", UIManager.MessageTypes.Info);
                        }
                        else
                        { //if no units are selected:
                          //go through the selection group
                            int i = 0;
                            bool Found = false;
                            while (i < SelectionGroups[NumKey].Count)
                            {
                                if (SelectionGroups[NumKey][i] != null)
                                { //if there's a unit
                                    Found = true;
                                    SelectUnit(SelectionGroups[NumKey][i], true); //select unit
                                    i++;
                                }
                                else
                                { //if there's no unit here
                                    SelectionGroups[NumKey].RemoveAt(i); //remove the field
                                }

                                //if no unit was selected:
                                if (Found == false)
                                {
                                    //print an error/error sound
                                    if (GroupEmptyAudio)
                                        AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, GroupEmptyAudio, false);
                                }
                            }
                        }
                    }
                }
            }
        }

        //get the num key that is pressed:
        public int GetNumKey()
        {
            if (Input.anyKeyDown)
            {
                if (Input.GetKey(KeyCode.Alpha0))
                {
                    return 0;
                }
                else if (Input.GetKey(KeyCode.Alpha1))
                {
                    return 1;
                }
                else if (Input.GetKey(KeyCode.Alpha2))
                {
                    return 2;
                }
                else if (Input.GetKey(KeyCode.Alpha3))
                {
                    return 3;
                }
                else if (Input.GetKey(KeyCode.Alpha4))
                {
                    return 4;
                }
                else if (Input.GetKey(KeyCode.Alpha5))
                {
                    return 5;
                }
                else if (Input.GetKey(KeyCode.Alpha6))
                {
                    return 6;
                }
                else if (Input.GetKey(KeyCode.Alpha7))
                {
                    return 7;
                }
                else if (Input.GetKey(KeyCode.Alpha8))
                {
                    return 8;
                }
                else if (Input.GetKey(KeyCode.Alpha9))
                {
                    return 9;
                }
            }

            return -1;
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        void ActionOnBuilding(Building HitBuilding, TaskManager.TaskTypes TaskType)
        {
            if(HitBuilding.Destroyed == true) //if the target building is already destroyed
            {
                UIMgr.ShowPlayerMessage("Target building has been destroyed!", UIManager.MessageTypes.Error);
                return;
            }

            //when TaskType is null that means that all tasks are allowed to be launched.
            if (SelectedUnits.Count > 0)
            { //Also make sure, at least a unit has been selected
                if (GameManager.PlayerFactionID == SelectedUnits[0].FactionID)
                { //Units from the player team can be moved by the player, others can't.
                    bool PortalUsed = false; //true if the portal has been used by the units and therefore no further action on the building will be taken
                    if (HitBuilding.GetComponent<Portal>() && (TaskType == TaskManager.TaskTypes.Null || TaskType == TaskManager.TaskTypes.Mvt))
                    {
                        //Portal:
                        Portal CurrentPortal = HitBuilding.GetComponent<Portal>();
                        if (CurrentPortal.CanUsePortal(SelectedUnits[0].FactionID)) //if the player can use the portal
                        {
                            PortalUsed = true; //so that no fruther action on the building is taken
                            for (int i = 0; i < SelectedUnits.Count; i++)
                            { //loop through the selected units
                                if (CurrentPortal.IsAllowed(SelectedUnits[i]) && SelectedUnits[i].CanBeMoved == true)
                                { //if the selected unit's category matches the allowed categories in the target portal.
                                    MvtMgr.Move(SelectedUnits[i], CurrentPortal.SpawnPos.position, HitBuilding.Radius, HitBuilding.gameObject, InputTargetMode.Portal);
                                }
                            }
                        }

                        FlashSelection(HitBuilding.gameObject, true);
                    }

                    if (PortalUsed == false) //if there was no portal or there was a portal and the units could not use it
                    {
                        if (SelectedUnits[0].FactionID == HitBuilding.FactionID || (HitBuilding.FreeBuilding == true && HitBuilding.IsBuilt == false))
                        { //If the units and the building are from the same team or this is a free building
                          //If the selected units can construct and the building needs construction & the task type is the build one

                            if (HitBuilding.Health < HitBuilding.MaxHealth && (TaskType == TaskManager.TaskTypes.Null || TaskType == TaskManager.TaskTypes.Build))
                            {
                                if (HitBuilding.WorkerMgr.CurrentWorkers < HitBuilding.WorkerMgr.WorkerPositions.Length)
                                {
                                    int i = 0; //counter
                                    bool MaxBuildersReached = false; //true when the maximum amount of builders for the hit building has been reached.
                                    Builder MainBuilder = null;
                                    int TempBuilderCounter = HitBuilding.WorkerMgr.CurrentWorkers;
                                    while (i < SelectedUnits.Count && MaxBuildersReached == false)
                                    { //loop through the selected as long as the max builders amount has not been reached.
                                        if (SelectedUnits[i].BuilderMgr && SelectedUnits[i].CanBeMoved == true)
                                        { //check if this unit has a builder comp (can actually build).
                                            //if this is a free building, the builder must be able to construct free buildings:
                                            if ((HitBuilding.FreeBuilding == true && SelectedUnits[i].BuilderMgr.BuildFreeBuildings == true) || HitBuilding.FreeBuilding == false)
                                            {
                                                //invisibility check:
                                                if (SelectedUnits[i].IsInvisible == false || (SelectedUnits[i].IsInvisible == true && SelectedUnits[i].InvisibilityMgr.CanBuild))
                                                {
                                                    //make sure that the maximum amount of builders has not been reached:
                                                    if (TempBuilderCounter < HitBuilding.WorkerMgr.WorkerPositions.Length)
                                                    {
                                                        //Make the units fix/build the building:
                                                        SelectedUnits[i].BuilderMgr.SetTargetBuilding(HitBuilding);
                                                        if (MainBuilder == null)
                                                            MainBuilder = SelectedUnits[i].BuilderMgr;
                                                        TempBuilderCounter++;
                                                    }
                                                    else
                                                    {
                                                        MaxBuildersReached = true;
                                                        //if the max builders amount has been reached.
                                                        //Show this message: 
                                                        UIMgr.ShowPlayerMessage("Max building amount for building has been reached!", UIManager.MessageTypes.Error);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                UIMgr.ShowPlayerMessage("Selected unit(s) can't construct free buildings!", UIManager.MessageTypes.Error);
                                            }
                                        }

                                        i++;
                                    }

                                    if (MainBuilder != null)
                                    {
                                        AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, MainBuilder.BuildingOrderAudio, false);
                                        FlashSelection(HitBuilding.gameObject, true);
                                        AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, BuildingMgr.SendToBuildAudio, false);
                                    }
                                }
                                else
                                {
                                    UIMgr.ShowPlayerMessage("Max building amount for building has been reached!", UIManager.MessageTypes.Error);
                                }
                            }
                            else if (HitBuilding.GetComponent<APC>() && (TaskType == TaskManager.TaskTypes.Null || TaskType == TaskManager.TaskTypes.Mvt))
                            {
                                //APC:
                                int i = 0;
                                APC CurrentAPC = HitBuilding.GetComponent<APC>();
                                while (i < SelectedUnits.Count && CurrentAPC.MaxAmount > CurrentAPC.CurrentUnits.Count)
                                { //loop through the selected units as long as the APC still have space
                                    if (!SelectedUnits[i].gameObject.GetComponent<APC>() && (CurrentAPC.UnitsList.Contains(SelectedUnits[i].Code) == CurrentAPC.AcceptUnitsInList || CurrentAPC.AllowAllUnits == true) && SelectedUnits[i].CanBeMoved == true)
                                    { //if the selected unit is no APC and the APC accepts the unit
                                      //send the unit to the APC vehicule:
                                        SelectedUnits[i].TargetAPC = CurrentAPC;
                                        MvtMgr.Move(SelectedUnits[i], CurrentAPC.InteractionPos.position, 0.0f, HitBuilding.gameObject, InputTargetMode.Building);
                                    }

                                    i++;
                                }

                                FlashSelection(HitBuilding.gameObject, true);
                            }
                        }
                        else if (TaskType == TaskManager.TaskTypes.Null || TaskType == TaskManager.TaskTypes.Attack)
                        {
                            //If the building is from a different team, we'll see if the unit can attack or not
                            if (HitBuilding.Health > 0.0f && HitBuilding.CanBeAttacked == true)
                            { //If the selected units can build and building has health	
                              //Make sure it's not peace time:
                                if (GameMgr.PeaceTime <= 0)
                                {
                                    if (SelectedUnits[0].AttackMgr)
                                        AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, SelectedUnits[0].AttackMgr.AttackOrderSound, false);

                                    MvtMgr.LaunchAttack(SelectedUnits, HitBuilding.gameObject, MovementManager.AttackModes.Full);
                                }
                                else
                                {
                                    UIMgr.ShowPlayerMessage("Can't attack in peace time!", UIManager.MessageTypes.Error);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void ActionOnResource(Resource HitResource, TaskManager.TaskTypes TaskType)
        {
            if (SelectedUnits.Count > 0)
            { //Also make sure, at least a unit has been selected
                if (TaskType == TaskManager.TaskTypes.Null || TaskType == TaskManager.TaskTypes.Collect)
                { //if the player comp task is a collect resource one or null
                    if (GameManager.PlayerFactionID == SelectedUnits[0].FactionID)
                    { //Units from the player team can be moved by the player, others can't.
                        if (HitResource.FactionID == GameManager.PlayerFactionID || HitResource.CollectOutsideBorder == true)
                        {
                            if (HitResource.Amount > 0)
                            { //If the selected units can gather resources, and make sure that the player can actually pick this up								
                                if (HitResource.WorkerMgr.CurrentWorkers < HitResource.WorkerMgr.WorkerPositions.Length)
                                { //Make sure that there's still room for another collectors:

                                    int i = 0; //counter
                                    bool MaxCollectorsReached = false; //true when the maximum amount of collectors for the hit resources has been reached.
                                    GatherResource MainCollector = null;
                                    int TempCollectorsCounter = HitResource.WorkerMgr.CurrentWorkers;
                                    while (i < SelectedUnits.Count && MaxCollectorsReached == false)
                                    { //loop through the selected as long as the max collectors amount has not been reached.
                                        if (SelectedUnits[i].ResourceMgr && SelectedUnits[i].CanBeMoved == true)
                                        { //check if this unit has a gather resource comp (can actually build).
                                          //invisibility check:
                                            if (SelectedUnits[i].IsInvisible == false || (SelectedUnits[i].IsInvisible == true && SelectedUnits[i].InvisibilityMgr.CanCollect))
                                            {
                                                //make sure that the maximum amount of collectors has not been reached:
                                                if (TempCollectorsCounter < HitResource.WorkerMgr.WorkerPositions.Length)
                                                {
                                                    //Collect the resource:
                                                    SelectedUnits[i].ResourceMgr.SetTargetResource(HitResource);
                                                    if (MainCollector == null)
                                                        MainCollector = SelectedUnits[i].ResourceMgr;
                                                    TempCollectorsCounter++;

                                                }
                                                else
                                                {
                                                    MaxCollectorsReached = true;
                                                    //if the max collectors amount has been reached.
                                                    //Show this message: 
                                                    UIMgr.ShowPlayerMessage("Max amount of collectors has been reached!", UIManager.MessageTypes.Error);

                                                }
                                            }
                                        }

                                        i++;
                                    }

                                    if (MainCollector != null)
                                    {
                                        FlashSelection(HitResource.gameObject, true);

                                        AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, MainCollector.SendToCollectAudio, false);
                                    }
                                }
                                else
                                {
                                    //Inform the player that there's no room for another collector in this resource:
                                    UIMgr.ShowPlayerMessage("Max amount of collectors has been reached!", UIManager.MessageTypes.Error);
                                }
                            }
                            else
                            {
                                UIMgr.ShowPlayerMessage("The targer resource is empty!", UIManager.MessageTypes.Error);
                            }
                        }
                        else
                        {
                            UIMgr.ShowPlayerMessage("The target resource is outside your faction's borders", UIManager.MessageTypes.Error);
                        }
                    }
                }
            }
        }

        public void ActionOnUnit(Unit HitUnit, TaskManager.TaskTypes TaskType)
        {
            if(HitUnit.Dead == true) //If the target unit is dead
            {
                UIMgr.ShowPlayerMessage("Target unit is dead!", UIManager.MessageTypes.Error);
                return; //no action will be taken
            }

            if (SelectedUnits.Count > 0)
            { //Also make sure, at least a unit has been selected
                if (GameManager.PlayerFactionID == SelectedUnits[0].FactionID)
                { //Units from the player team can be moved by the player, others can't.
                    if (HitUnit.FactionID != SelectedUnits[0].FactionID)
                    {
                        //make sure that the hit unit can be converted
                        if ((TaskType == TaskManager.TaskTypes.Null || TaskType == TaskManager.TaskTypes.Convert) && SelectedUnits[0].ConvertMgr != null)
                        { //if the pending comp task is a convert one
                            if (HitUnit.CanBeConverted == true) //if the target unit can be converted
                            {
                                if (SelectedUnits.Count == 1 && SelectedUnits[0].CanBeMoved == true)
                                { //if one unit is selected and it has the convert component
                                  //invisibility check:
                                    if (SelectedUnits[0].IsInvisible == false || (SelectedUnits[0].IsInvisible == true && SelectedUnits[0].InvisibilityMgr.CanConvert))
                                    {
                                        AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, SelectedUnits[0].ConvertMgr.ConvertOrderAudio, false);
                                        SelectedUnits[0].ConvertMgr.SetTargetUnit(HitUnit); //set the target unit to convert.

                                        FlashSelection(HitUnit.gameObject, false);
                                    }
                                }
                            }
                            else
                            {
                                UIMgr.ShowPlayerMessage("Target unit can't be converted.", UIManager.MessageTypes.Error);
                            }
                        }
                        else if (TaskType == TaskManager.TaskTypes.Null || TaskType == TaskManager.TaskTypes.Attack)
                        { //else if the pending comp task is an attack one.
                          //launch an attack if the peace time is over.
                            if (GameMgr.PeaceTime == 0.0f)
                            {
                                //make sure the target unit can be attacked:
                                if (HitUnit.IsInvisible == false)
                                {
                                    MvtMgr.LaunchAttack(SelectedUnits, HitUnit.gameObject, MovementManager.AttackModes.Full);
                                }
                            }
                            else
                            {
                                UIMgr.ShowPlayerMessage("Can't attack in peace time!", UIManager.MessageTypes.Error);
                            }
                        }
                    }
                    else
                    { //if the hit unit belongs to the player's faction
                        if (HitUnit.APCMgr != null && (TaskType == TaskManager.TaskTypes.Null || TaskType == TaskManager.TaskTypes.Mvt))
                        { //if the selected unit has a APC comp and the pending task is a mvt one
                          //APC:
                            int i = 0;
                            APC CurrentAPC = HitUnit.GetComponent<APC>();
                            while (i < SelectedUnits.Count && CurrentAPC.MaxAmount > CurrentAPC.CurrentUnits.Count)
                            { //loop through the selected units as long as the APC still have space
                              //if the selected unit is no APC and the APC accepts this unit
                                if (!SelectedUnits[i].gameObject.GetComponent<APC>() && (CurrentAPC.UnitsList.Contains(SelectedUnits[i].Code) == CurrentAPC.AcceptUnitsInList || CurrentAPC.AllowAllUnits == true) && SelectedUnits[i].CanBeMoved == true)
                                {
                                    //send the unit to the APC vehicule:
                                    SelectedUnits[i].TargetAPC = CurrentAPC;
                                    MvtMgr.Move(SelectedUnits[i], CurrentAPC.InteractionPos.position, 0.0f, HitUnit.gameObject, InputTargetMode.Unit);
                                }

                                i++;
                            }

                            FlashSelection(HitUnit.gameObject, true);
                        }
                        else if (TaskType == TaskManager.TaskTypes.Null || TaskType == TaskManager.TaskTypes.Heal)
                        { //if the selected unit(s) have a healer component
                            Healer MainHealer = null;
                            for (int i = 0; i < SelectedUnits.Count; i++)
                            {
                                if (SelectedUnits[i].CanBeMoved == true && SelectedUnits[i].HealMgr != null)
                                {
                                    //invisibility check:
                                    if (SelectedUnits[i].IsInvisible == false || (SelectedUnits[i].IsInvisible == true && SelectedUnits[i].InvisibilityMgr.CanHeal))
                                    {
                                        SelectedUnits[i].HealMgr.SetTargetUnit(HitUnit); //heal the target unit

                                        if (MainHealer != null)
                                            MainHealer = SelectedUnits[i].HealMgr;
                                    }
                                }
                            }
                            if (MainHealer != null)
                            {
                                AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, MainHealer.HealOrderAudio, false);
                                FlashSelection(HitUnit.gameObject, true);
                            }
                        }
                    }
                }
            }

            else if (SelectedBuilding != null) //if no units are selected but a building is
            {
                if (GameManager.PlayerFactionID == SelectedBuilding.FactionID && SelectedBuilding.AttackMgr != null)
                { //Making sure that the local player owns the building and that the building can attack
                    if (HitUnit.FactionID != SelectedBuilding.FactionID)
                    { //If the target unit has different team
                        if (TaskType == TaskManager.TaskTypes.Null || TaskType == TaskManager.TaskTypes.Attack)
                        { //if the pending comp task is an attack one.
                          //launch an attack if the peace time is over.
                            if (GameMgr.PeaceTime == 0.0f)
                            {
                                //make sure the target unit is visible to attack it:
                                if (HitUnit.IsInvisible == false)
                                {
                                    AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, SelectedBuilding.AttackMgr.AttackOrderSound, false);
                                    SelectedBuilding.AttackMgr.SetAttackTarget(HitUnit.gameObject);

                                    //flash target unit selection:
                                    FlashSelection(HitUnit.gameObject, false);
                                }
                            }
                            else
                            {
                                UIMgr.ShowPlayerMessage("Can't attack in peace time!", UIManager.MessageTypes.Error);
                            }
                        }
                    }
                }
            }
        }

        //Resource selection:
        public void SelectResource(Resource Resource)
        {
            if (SelectedResource != null) DeselectResource(); //Deselect the currently selected resource.
            DeselectUnits(); //Deselect currently selected units.
            DeselectBuilding(); //Deselect buildings.

            if (Resource.ResourcePlane)
            {
                //Activate the resource's plane object where we will show the selection texture.
                Resource.ResourcePlane.SetActive(true);

                //Show the selection texture and set its color.
                Resource.ResourcePlane.GetComponent<Renderer>().material.mainTexture = ResourceSelectionTexture;
                //Set the selection color to the resource color:
                Color SelectionColor = ResourceMgr.ResourceSelectionColor;
                Resource.ResourcePlane.GetComponent<Renderer>().material.color = new Color(SelectionColor.r, SelectionColor.g, SelectionColor.b, 0.5f);
            }

            SelectedResource = Resource;
            //Selected UI:
            UIMgr.UpdateResourceUI(Resource);

            //custom event:
            GameMgr.Events.OnResourceSelected(Resource);
        }

        public void DeselectResource()
        {
            if (SelectedResource != null)
            {
                UIMgr.HideTaskButtons();
                UIMgr.HideSelectionInfoPanel();
                UIMgr.HideTooltip();
            }

            //Deselect the resource by hiding the resource plane:
            if (SelectedResource != null)
            {
                GameMgr.Events.OnResourceDeselected(SelectedResource);
                if (SelectedResource.ResourcePlane)
                    SelectedResource.ResourcePlane.SetActive(false);
            }
            SelectedResource = null;
        }

        //building selection:
        public void SelectBuilding(Building Building)
        {
            //if the building is destroyed then don't proceed:
            if(Building.Destroyed == true)
            {
                return;
            }

            DeselectUnits();

            //If the building has been already placed.
            if (Building.Placed == true)
            {

                if (SelectedBuilding != null) DeselectBuilding(); //Deselect the currently selected building.
                DeselectUnits(); //Deselect currently selected units.
                DeselectResource(); //Deselect the selected resource if there is any

                if (Building.BuildingPlane)
                {
                    //Activate the building's plane object where we will show the selection texture.
                    Building.BuildingPlane.SetActive(true);

                    //Show the selection texture and set its color.
                    Building.BuildingPlane.GetComponent<Renderer>().material.mainTexture = BuildingSelectionTexture;
                    //Set the selection color to the building's team color:
                    Color SelectionColor = new Color();
                    if (Building.FreeBuilding == false)
                    {
                        SelectionColor = GameMgr.Factions[Building.FactionID].FactionColor;
                    }
                    else
                    {
                        SelectionColor = GameMgr.BuildingMgr.FreeBuildingSelectionColor;
                    }
                    Building.BuildingPlane.GetComponent<Renderer>().material.color = new Color(SelectionColor.r, SelectionColor.g, SelectionColor.b, 0.5f);
                }

                SelectedBuilding = Building;
                //Building UI:
                UIMgr.UpdateBuildingUI(Building);
                //If it has a go to position and if the building is already built:
                if (SelectedBuilding.GotoPosition != null && SelectedBuilding.IsBuilt == true && Building.FactionID == GameManager.PlayerFactionID)
                {
                    //Show the goto position:
                    SelectedBuilding.GotoPosition.gameObject.SetActive(true);
                }

                //custom event:
                if (GameMgr.Events) GameMgr.Events.OnBuildingSelected(Building);

            }
        }

        public void DeselectBuilding()
        {
            //Deselect the building by hiding the building plane:
            if (SelectedBuilding != null)
            {

                UIMgr.HideTaskButtons();
                UIMgr.HideSelectionInfoPanel();
                UIMgr.HideTooltip();

                if (SelectedBuilding.BuildingPlane)
                    SelectedBuilding.BuildingPlane.SetActive(false);
                //If it has a go to position:
                if (SelectedBuilding.GotoPosition != null)
                {
                    //Hide the goto position:
                    SelectedBuilding.GotoPosition.gameObject.SetActive(false);
                }

                if (GameMgr.Events) GameMgr.Events.OnBuildingDeselected(SelectedBuilding);
            }

            //custom event:
            SelectedBuilding = null;

        }

        //select units having the same code in a certain range:
        public void SelectUnitsInRange(Unit Unit)
        {
            if (GameMgr.Factions[GameManager.PlayerFactionID].FactionMgr.Units.Count > 0)
            {
                for (int x = 0; x < GameMgr.Factions[GameManager.PlayerFactionID].FactionMgr.Units.Count; x++)
                { //go through the present units in the scene
                    Unit ThisUnit = GameMgr.Factions[GameManager.PlayerFactionID].FactionMgr.Units[x];
                    if (Vector3.Distance(ThisUnit.transform.position, Unit.transform.position) <= DoubleClickSelectSize)
                    {
                        if (ThisUnit.Code == Unit.Code)
                        {
                            SelectUnit(ThisUnit, true);
                        }
                    }
                }
            }
        }

        //unit selection:
        public void SelectUnit(Unit Unit, bool Add)
        {
            if(Unit.Dead == true) //if the unit is dead.
            {
                return; //do not proceed.
            }

            //stop the cam from following the last selected unit it was already doing that.
            GameMgr.CamMov.UnitToFollow = null;

            DeselectBuilding();
            //If the unit is already selected
            if (IsUnitSelected(Unit) == true && MultipleSelectionKeyDown == true)
            {
                //Deselect it:
                DeselectUnit(Unit);
                return;
            }
            //If we're adding units to the current selection.
            if (Add == true && SelectedUnits.Count > 0)
            {
                //Make sure they belong to the same team:
                if (Unit.FactionID != SelectedUnits[0].FactionID)
                {
                    return; //Don't select this unit.
                }
            }

            if (Add == false || SelectedUnits.Count == 0) //If we choose to select this unit only or simply if a building was selected
            {
                DeselectBuilding(); //Deselect the currently selected building.
                DeselectResource();
                DeselectUnits(); //Deselect currently selected units.
            }


            if (Unit.UnitPlane)
            {
                //Activate the unit's plane object where we will show the selection texture.
                Unit.UnitPlane.SetActive(true);

                //Show the selection texture and set its color.
                Unit.UnitPlane.GetComponent<Renderer>().material.mainTexture = UnitSelectionTexture;
                //Set the selection color to the building's team color:
                Color SelectionColor = new Color();
                if (Unit.FreeUnit == false)
                {
                    SelectionColor = GameMgr.Factions[Unit.FactionID].FactionColor;
                }
                else
                {
                    SelectionColor = GameMgr.UnitMgr.FreeUnitSelectionColor;
                }
                Unit.UnitPlane.GetComponent<Renderer>().material.color = new Color(SelectionColor.r, SelectionColor.g, SelectionColor.b, 0.5f);
            }

            SelectedUnits.Add(Unit);

            //Unit UI:
            UIMgr.UpdateUnitUI(SelectedUnits[0]);

            //custom event to alert that we selected a unit:
            if (GameMgr.Events) GameMgr.Events.OnUnitSelected(Unit);
        }

        //Called to deselect all units:
        public void DeselectUnits()
        {
            UIMgr.HideTaskButtons();
            UIMgr.HideSelectionInfoPanel();
            UIMgr.HideTooltip();

            //stop the cam from following the last selected unit it was already doing that.
            GameMgr.CamMov.UnitToFollow = null;

            UIMgr.HideTaskButtons();
            UIMgr.HideSelectionInfoPanel();

            //Deselect all the units:
            if (SelectedUnits.Count > 0) //Loop through all selected units then 
            {
                for (int i = 0; i < SelectedUnits.Count; i++)
                {

                    if (SelectedUnits[i].UnitPlane)
                    {
                        SelectedUnits[i].UnitPlane.SetActive(false);
                    }

                    if (GameMgr.Events) GameMgr.Events.OnUnitDeselected(SelectedUnits[i]);
                }

            }

            SelectedUnits.Clear();
        }

        //called to deselect one unit
        public void DeselectUnit(Unit Unit)
        {
            //stop the cam from following the last selected unit it was already doing that.
            GameMgr.CamMov.UnitToFollow = null;

            if (IsUnitSelected(Unit) == true) //Make sure that the unit is selected
            {
                if (SelectedUnits.Count == 1)
                {
                    //If it's the only unit selected, deselect all units:
                    DeselectUnits();
                }
                else
                {
                    SelectedUnits.Remove(Unit);
                    Unit.UnitPlane.SetActive(false);

                    //Unit UI:
                    UIMgr.UpdateUnitUI(SelectedUnits[0]);

                    if (GameMgr.Events) GameMgr.Events.OnUnitDeselected(Unit);
                }
            }
        }

        //check if a unit is selected:
        public bool IsUnitSelected(Unit Unit)
        {
            //See if a unit is selected or not.
            if (SelectedUnits.Count > 0)
            {
                bool Found = false;
                int i = 0;
                //loop through all the units
                while (i < SelectedUnits.Count && Found == false)
                {
                    //look for the unit
                    if (SelectedUnits[i] == Unit)
                    {
                        Found = true;
                    }
                    i++;
                }
                return Found;
            }
            else
            {
                return false;
            }
        }

        //get the selected unit ID.
        public int GetSelectedUnitID(Unit Unit)
        {
            //Get the ID of a selected unit.
            if (SelectedUnits.Count > 0)
            {
                int i = 0;
                while (i < SelectedUnits.Count)
                {
                    if (SelectedUnits[i] == Unit)
                    {
                        return i;
                    }
                    i++;
                }
                return -1;
            }
            else
            {
                return -1;
            }
        }

        //Flash an object's selection plane (friendly or enemy flash):
        public void FlashSelection(GameObject TargetObj, bool Friendly)
        {
            //make sure the target object is valid:
            if (TargetObj != null)
            {
                if (TargetObj.GetComponent<Building>()) //if it's a building
                {
                    //enemy or friendly flash?
                    TargetObj.GetComponent<Building>().BuildingPlane.GetComponent<Renderer>().material.color = (Friendly == true) ? FriendlyFlashColor : EnemyFlashColor;
                    TargetObj.GetComponent<Building>().FlashTime = FlashTime;
                    TargetObj.GetComponent<Building>().InvokeRepeating("SelectionFlash", 0.0f, FlashRepeatTime);
                }
                else if (TargetObj.GetComponent<Unit>()) //if it's a unit
                {
                    //enemy or friendly flash?
                    TargetObj.GetComponent<Unit>().UnitPlane.GetComponent<Renderer>().material.color = (Friendly == true) ? FriendlyFlashColor : EnemyFlashColor;
                    TargetObj.GetComponent<Unit>().FlashTime = FlashTime;
                    TargetObj.GetComponent<Unit>().InvokeRepeating("SelectionFlash", 0.0f, FlashRepeatTime);
                }
                else if (TargetObj.GetComponent<Resource>()) //if it's a resource
                {
                    //enemy or friendly flash?
                    TargetObj.GetComponent<Resource>().ResourcePlane.GetComponent<Renderer>().material.color = (Friendly == true) ? FriendlyFlashColor : EnemyFlashColor;
                    TargetObj.GetComponent<Resource>().FlashTime = FlashTime;
                    TargetObj.GetComponent<Resource>().InvokeRepeating("SelectionFlash", 0.0f, FlashRepeatTime);
                }
            }
        }
    }
}