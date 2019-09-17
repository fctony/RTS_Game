using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.AI;
using UnityEngine.Events;

/* Task Launcher script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class TaskLauncher : MonoBehaviour
    {
        //This component can be attached to units and buildings only.
        public enum TaskHolders { Unit, Building};
        [HideInInspector]
        public TaskHolders TaskHolder;

        //unique code for each task launcher:
        public string Code = "unique_task_launcher_code";

        //Components that the Task Launcher is attached to.
        [HideInInspector]
        public Building RefBuilding;
        [HideInInspector]
        public Unit RefUnit;

        //The amount of total tasks will be saved here:
        [HideInInspector]
        public int TotalTasksAmount = 0;

        public float MinTaskHealth = 70.0f; //minimum health required in order to launch/complete a task. 

        public int MaxTasks = 4; //The amount of maximum tasks that this component can handle at the same time.

        public enum AllowedTaskTypes {CreateUnit, Research, Destroy, CustomTask};

        [System.Serializable]
        public class TasksVars
        {
            //Unique code for every task:
            public string Code = "unique_task_code";

            //Can this task be used only by a specific faction?
            public bool FactionSpecific = false;
            public string FactionCode = "Faction001";

            public string Description = "describe your task here"; //description shown in the task panel when hovering over the task button.
            [HideInInspector]
            public TaskManager.TaskTypes TaskType = TaskManager.TaskTypes.CreateUnit; //the type of the task.
            public AllowedTaskTypes AllowedTaskType = AllowedTaskTypes.CreateUnit; //so that only allowed task types are entered in the inspector
            public int TaskPanelCategory = 0; //if you are using different categories in the task panel then assign this for each task.
            public Sprite TaskIcon; //the icon shown in the tasks panel
            //Timers:
            public float ReloadTime = 3.0f; //how long does the task last?

            public ResourceManager.Resources[] RequiredResources; //Resources required to complete this task.

            public AudioClip TaskCompletedAudio; //Audio clip played when the task is completed.

            public bool UseOnce = false; //Can this task only be once used? 

            public TaskManager.UnitResearchTask UnitResearchSettings; //will be shown only in case the task type is a unit research.

            public TaskManager.UnitCreationTask UnitCreationSettings; //will be shown only in case the task type is a unit creation.

            [HideInInspector]
            public bool Active = false; //is this task currently active?
            [HideInInspector]
            public bool Reached = false; //has this task been done? 

            [HideInInspector]
            public int CurrentUpgradeLevel = 0; //The current upgrade level.

            //Events: Besides the custom delegate events, you can directly use the event triggers below to further customize the behavior of the tasks:
            public UnityEvent TaskLaunchEvent;
            public UnityEvent TaskCompleteEvent;
            public UnityEvent TaskCancelEvent;
        }
        public List<TasksVars> TasksList = new List<TasksVars>(); //all tasks go here.

        //a list of the pending tasks:
        [System.Serializable]
        public class TasksQueueInfo
        {
            public int ID = -1; //the task ID.
            public bool Upgrade = false; //is the current pending task an upgrade for a unit creation task?

            //NPC and unit creation tasks:
            public Building TargetBuilding = null; //if the unit will be sent to construct a building right after creation, then this is the building.
            public Resource TargetResource = null; //if the unit will be sent to collect a resource right after creation, then this is that resource.
        }
        [HideInInspector]
        public List<TasksQueueInfo> TasksQueue = new List<TasksQueueInfo>(); //this is the task's queue which holds all the pending tasks
        [HideInInspector]
        public float TaskQueueTimer = 0.0f; //this is the task's timer. when it's done, one task out of the queue is done.

        //The variables below determine the types of the tasks this building has:
        [HideInInspector]
        public List<int> UnitCreationTasks = new List<int>(); //if the building has a task that produces any type of units then it will be added in this list.
        [HideInInspector]
        public List<int> ArmyUnits = new List<int>(); //if the building has a task that produce units with the attack comp, the task ID will be added to this list.
        [HideInInspector]
        public List<int> BuilderUnits = new List<int>(); //if the building has a task that produce units with the builder comp, the task ID will be added to this list.
        [HideInInspector]
        public List<int> ResourceUnits = new List<int>(); //if the building has a task that produce units with the resource gather comp, the task ID will be added to this list.

        //Audio:
        public AudioClip LaunchTaskAudio; //Audio played when a new building task is launched.
        public AudioClip DeclinedTaskAudio; //When the task is declined due to lack of resources, the fact that the maximum in progress task has been reached or the min task health is not present. 

        //Faction info:
        [HideInInspector]
        public int FactionID;
        public FactionManager FactionMgr;
        
        //Other components:
        GameManager GameMgr;
        UIManager UIMgr;
        TerrainManager TerrainMgr;
        SelectionManager SelectionMgr;
        ResourceManager ResourceMgr;

        private bool isActive = false; //is the task launcher active or not?

        //Used for the custom editor:
        [HideInInspector]
        public int TaskID; //Current task ID that the user is configuring.
        [HideInInspector]
        public int UpgradeID; //Current upgrade ID that the user is configuring.
        [HideInInspector]
        public int TabID; //Current tab that the user is viewing

        //Called when the building/unit is ready to initialize the tasks
        public void OnTasksInit()
        {
            //get the building/unit components
            RefUnit = gameObject.GetComponent<Unit>();
            RefBuilding = gameObject.GetComponent<Building>();

            //we expect the Task Launcher to be attached to either a building or a unit
            Assert.IsTrue(RefUnit != null || RefBuilding != null);

            TaskHolder = (RefUnit != null) ? TaskHolders.Unit : TaskHolders.Building; //set the task holder.

            //Get the other components:
            GameMgr = GameManager.Instance;
            UIMgr = GameMgr.UIMgr;
            TerrainMgr = TerrainManager.Instance;
            SelectionMgr = GameMgr.SelectionMgr;
            ResourceMgr = GameMgr.ResourceMgr;

            if (TasksList.Count > 0) //loop through all tasks
            {
                for (int i = 0; i < TasksList.Count; i++)
                {
                    TotalTasksAmount++; //sum of the tasks
                    if (TasksList[i].UnitCreationSettings.Upgrades.Count > 0) //if a task has an upgrade
                    {
                        TotalTasksAmount++; //add it to the total amount
                    }
                }
            }

            SetFactionInfo();

            FactionMgr.TaskLaunchers.Add(this); //add the task launcher here.

            SetTaskTypes();

            isActive = true;

            //Launch the delegate event:
            if (GameMgr.Events)
                GameMgr.Events.OnTaskLauncherAdded(this);
        }

        //a method to determine whether the task holder is capable of launching a task:
        private bool CanManageTask ()
        {
            if(TaskHolder == TaskHolders.Unit) //if the task holder is a unit
            {
                return RefUnit.Health >= MinTaskHealth && RefUnit.Dead == false; //make sure the unit is not dead and has enough health
            }
            else //if not, make sure the building is built, not destroyed, not upgrading and has enough health.
            {
                return RefBuilding.IsBuilt == true && RefBuilding.Destroyed == false && RefBuilding.BuildingUpgrading == false && RefBuilding.Health >= MinTaskHealth;
            }
        }

        private bool CanLaunchTaskd ()
        {
            return CanManageTask() && TasksQueue.Count < MaxTasks;
        }

        //a method to get the faction ID of the task holder:
        public void SetFactionInfo ()
        {
            if(TaskHolder == TaskHolders.Unit)
            {
                FactionID = RefUnit.FactionID;
                FactionMgr = RefUnit.FactionMgr; 
            }
            else
            {
                FactionID = RefBuilding.FactionID;
                FactionMgr = RefBuilding.FactionMgr;
            }
        }

        //a method to check if the task holder is selected:
        public bool IsTaskHolderSelected ()
        {
            if(TaskHolder == TaskHolders.Unit)
            {
                return SelectionMgr.SelectedUnits.Contains(RefUnit);
            }
            else
            {
                return SelectionMgr.SelectedBuilding == RefBuilding;
            }
        }

        //get the task holder's health
        public float GetTaskHolderHealth()
        {
            if(TaskHolder == TaskHolders.Unit)
            {
                return RefUnit.Health;
            }
            else
            {
                return RefBuilding.Health;
            }
        }

        //a method to get the spawn position for newly created units:
        public Vector3 GetSpawnPosition ()
        {
            if(TaskHolder == TaskHolders.Building) //if the task holder is a building
            {
                return new Vector3(RefBuilding.SpawnPosition.position.x, TerrainMgr.SampleHeight(RefBuilding.SpawnPosition.position), RefBuilding.SpawnPosition.position.z); //return the building's assigned spawn position
            }
            else //if this is a unit
            {
                return transform.position; //return the unit's position
            }
        }

        void Update()
        {
            //only if the task launcher is active
            if (isActive == true)
            {
                if (CanManageTask() == true) //can the task holder manage tasks now?
                {
                    if (TasksQueue.Count > 0) //if there are pending tasks
                    {
                        //keep updating them:
                        UpdateTasks();
                    }
                }
            }
        }

        //Setting the task types will help factions pick the task that they need:
        void SetTaskTypes()
        {
            //initialize the task lists:
            UnitCreationTasks = new List<int>();
            ArmyUnits = new List<int>();
            BuilderUnits = new List<int>();
            ResourceUnits = new List<int>();

            //Make sure the building has a task launcher attached to it.

            if (TasksList.Count > 0)
            { //if the building actually has tasks:
                int i = 0;
                while (i < TasksList.Count)
                {
                    bool TaskRemoved = false;
                    //if the faction is controlled by the player in a single player or a multiplayer game:
                    if (FactionID == GameManager.PlayerFactionID)
                    {
                        if (TasksList[i].FactionSpecific == true)
                        { //if this task is faction type specific:
                            bool Remove = false;
                            //make sure that a faction type is assigned:
                            if(GameMgr.Factions[FactionID].TypeInfo == null)
                            {
                                Remove = true;
                            }
                            else if (TasksList[i].FactionCode != GameMgr.Factions[FactionID].TypeInfo.Code)
                            { //if the faction code does not match
                                Remove = true;
                            }

                            if(Remove == true)
                            {
                                TasksList.RemoveAt(i);
                                TaskRemoved = true;
                            }
                        }
                    }
                    if (TaskRemoved == false)
                    {
                        //loop through all the building's task
                        if (TasksList[i].TaskType == TaskManager.TaskTypes.CreateUnit)
                        {
                            UnitCreationTasks.Add(i);
                            //for the task that create units, add the task to a list depending on the unit's abilities:
                            if (TasksList[i].UnitCreationSettings.Prefabs[0].gameObject.GetComponent<Attack>())
                            {
                                ArmyUnits.Add(i);
                            }
                            if (TasksList[i].UnitCreationSettings.Prefabs[0].gameObject.GetComponent<GatherResource>())
                            {
                                ResourceUnits.Add(i);
                            }
                            if (TasksList[i].UnitCreationSettings.Prefabs[0].gameObject.GetComponent<Builder>())
                            {
                                BuilderUnits.Add(i);
                            }
                        }
                        i++;
                    }
                }
            }
        }

        //method called when the task launcher has pending tasks:
        void UpdateTasks()
        {
            //if the task timer is still going and we are not using the god mode
            if (TaskQueueTimer > 0 && GodMode.Enabled == false)
            {
                TaskQueueTimer -= Time.deltaTime;

                if(IsTaskHolderSelected())
                    UIMgr.UpdateInProgressTasksUI();
            }
            //till it stops:
            else
            {
                //play the task complete audio if this is the player's faction
                if (TasksList[TasksQueue[0].ID].TaskCompletedAudio != null && FactionID == GameManager.PlayerFactionID)
                {
                    AudioManager.PlayAudio(gameObject, TasksList[TasksQueue[0].ID].TaskCompletedAudio, false); //Play the audio clip
                }

                //delegate event:
                if(GameMgr.Events)
                    GameMgr.Events.OnTaskCompleted(this, TasksQueue[0].ID, 0);

                //Unity event:
                TasksList[TasksQueue[0].ID].TaskCompleteEvent.Invoke();

                if (TasksQueue[0].Upgrade == true) //if this is an upgrade task
                {
                    //complete it
                    OnUpgradeTaskCompleted();
                }
                else //if this is a normal task
                {
                    OnTaskCompleted();
                }

                if (TasksQueue.Count > 0)
                    TasksQueue.RemoveAt(0);// Remove this task.

                if(IsTaskHolderSelected())
                {
                    //update the selection panel UI to show that this task is no longer in progress.
                    UIMgr.UpdateTaskPanel();
                    UIMgr.UpdateInProgressTasksUI();
                }

                if (TasksQueue.Count > 0)
                {
                    //if there are more tasks in the queue
                    TaskQueueTimer = TasksList[TasksQueue[0].ID].ReloadTime; //set the reload for the next task and start over.
                }
            }
        }

        //a method called when an upgrade task is complete:
        void OnUpgradeTaskCompleted()
        {
            //making sure that the current upgrade level is not the maximal one
            if (TasksList[TasksQueue[0].ID].CurrentUpgradeLevel < TasksList[TasksQueue[0].ID].UnitCreationSettings.Upgrades.Count)
            {
                //update the upgrade on all similar buildings
                CheckTaskUpgrades(TasksQueue[0].ID, false, false);
                //if this is an upgrade task:
                UpgradeTask(TasksQueue[0].ID, TasksList[TasksQueue[0].ID].CurrentUpgradeLevel);
            }
        }

        //a method that checks if all the task launchers (from the same code) are be in the same task upgrade level:
        public void CheckTaskUpgrades(int TaskID, bool Pending, bool Canceled)
        {
            if (FactionMgr.TaskLaunchers.Count > 0)
            {
                //loop through all the faction's task launchers:
                for (int i = 0; i < FactionMgr.TaskLaunchers.Count; i++)
                {
                    //find buildings with similar codes:
                    if (FactionMgr.TaskLaunchers[i].Code == Code && FactionMgr.TaskLaunchers[i] != this)
                    {
                        //apply the same tasks' upgrade state:
                        if (Canceled == false)
                        {
                            if (Pending == true) //if the task is still pending, then simply activate it on all other task launchers so it can't be launched twice
                            {
                                FactionMgr.TaskLaunchers[i].TasksList[TaskID].Active = true;
                            }
                            else //if the task is done
                            {
                                //then sync it with simlair task launchers
                                if (FactionMgr.TaskLaunchers[i].TasksList[TaskID].TaskType == TaskManager.TaskTypes.Research)
                                {
                                    FactionMgr.TaskLaunchers[i].TasksList[TaskID].Reached = true;
                                }
                                else
                                {
                                    FactionMgr.TaskLaunchers[i].UpgradeTask(TaskID, TasksList[TaskID].CurrentUpgradeLevel);
                                }
                            }
                        }
                        else
                        {
                            FactionMgr.TaskLaunchers[i].TasksList[TaskID].Active = false;
                        }
                    }
                }
            }
        }

        //upgrade a task:
        public void UpgradeTask(int TaskID, int TargetLevel)
        {
            //set the upgraded task settings:
            TasksList[TaskID].UnitCreationSettings.Prefabs = TasksList[TaskID].UnitCreationSettings.Upgrades[TargetLevel].TargetPrefabs;
            TasksList[TaskID].TaskIcon = TasksList[TaskID].UnitCreationSettings.Upgrades[TargetLevel].NewTaskIcon;
            TasksList[TaskID].Description = TasksList[TaskID].UnitCreationSettings.Upgrades[TargetLevel].NewTaskDescription;
            TasksList[TaskID].ReloadTime = TasksList[TaskID].UnitCreationSettings.Upgrades[TargetLevel].NewReloadTime;

            //take the resources of the upgrade
            if (TasksList[TaskID].UnitCreationSettings.Upgrades[TargetLevel].NewTaskResources.Length > 0)
            {
                TasksList[TaskID].RequiredResources = TasksList[TaskID].UnitCreationSettings.Upgrades[TargetLevel].NewTaskResources;
            }

            //move to the next upgrade level:
            TasksList[TaskID].CurrentUpgradeLevel = TargetLevel + 1;
            TasksList[TaskID].Active = false;

            if (TasksList[TaskID].UnitCreationSettings.Upgrades.Count == TargetLevel)
            { //if this is the last upgrade
                TotalTasksAmount--; //then decrease the amount of the total tasks amount
            }
        }


        //sync the building's upgrade level when it's spawned:
        public void SyncTaskUpgradeLevel()
        {
            //if the task launcher has tasks:
            if (TasksList.Count > 0)
            {
                if (FactionMgr.TaskLaunchers.Count > 0)
                {
                    //loop through all the faction's task launchers:
                    for (int i = 0; i < FactionMgr.TaskLaunchers.Count; i++)
                    {
                        //find task launcher with similar codes:
                        if (FactionMgr.TaskLaunchers[i].Code == Code && FactionMgr.TaskLaunchers[i] != this)
                        {
                            for (int j = 0; j < TasksList.Count; j++)
                            {
                                if (TasksList[j].TaskType == TaskManager.TaskTypes.CreateUnit)
                                { //if the task produces units and its current upgrade level isn't maximal yet
                                    if (TasksList[j].UnitCreationSettings.Upgrades.Count > 0 && TasksList[j].CurrentUpgradeLevel < FactionMgr.TaskLaunchers[i].TasksList[j].CurrentUpgradeLevel)
                                    {
                                        UpgradeTask(j, FactionMgr.TaskLaunchers[i].TasksList[j].CurrentUpgradeLevel - 1); //upgrade the task to sync it.
                                    }
                                }
                                else if (TasksList[j].TaskType == TaskManager.TaskTypes.Research) //if the task type is research
                                {
                                    if (FactionMgr.TaskLaunchers[i].TasksList[j].Reached == true) //if the task has been already reached
                                    {
                                        //lock it here as well
                                        TasksList[j].Active = true;
                                        TasksList[j].Reached = true;
                                    }
                                }
                            }

                            return;
                        }
                    }
                }
            }
        }


        //a method called when a normal task (not an upgrade one) is complete:
        void OnTaskCompleted()
        {
            //If the first task in the queue is about creating units.
            if (TasksList[TasksQueue[0].ID].TaskType == TaskManager.TaskTypes.CreateUnit)
            {
                //Randomly pick a prefab to produce:
                Unit UnitPrefab = TasksList[TasksQueue[0].ID].UnitCreationSettings.Prefabs[Random.Range(0, TasksList[TasksQueue[0].ID].UnitCreationSettings.Prefabs.Length)];

                if (GameManager.MultiplayerGame == false) //if this is a single player game
                {
                    bool Cancel = false;
                    //if this is a NPC faction
                    if (GameManager.PlayerFactionID != FactionID)
                    {
                        //If the new unit is supposed to go contrusct a building or go collect resources:
                        //Check if there are places available to construct or collect the resource, if not, we'll cancel creating the unit
                        if (TasksQueue[0].TargetBuilding != null)
                        {
                            if (TasksQueue[0].TargetBuilding.WorkerMgr.CurrentWorkers == TasksQueue[0].TargetBuilding.WorkerMgr.WorkerPositions.Length)
                            {
                                CancelInProgressTask(0);
                                Cancel = true;
                            }
                        }
                        else if (TasksQueue[0].TargetResource != null)
                        {
                            if (TasksQueue[0].TargetResource.WorkerMgr.CurrentWorkers == TasksQueue[0].TargetResource.WorkerMgr.WorkerPositions.Length)
                            {
                                CancelInProgressTask(0);
                                Cancel = true;
                            }
                        }
                    }

                    if (Cancel == false) //if the task is not to be cancelled
                    {

                        Unit NewUnit = UnitManager.CreateUnit(UnitPrefab, GetSpawnPosition(), FactionID, RefBuilding);

                        //rallypoint for NPC players:
                        //if the new unit must construct a building, send the unit to build.
                        if (TasksQueue[0].TargetBuilding != null && NewUnit.GetComponent<Builder>())
                        {
                            NewUnit.GetComponent<Builder>().SetTargetBuilding(TasksQueue[0].TargetBuilding);
                        }
                        //if the new unit is entitled to collect a resource, send the unit to collect.
                        else if (TasksQueue[0].TargetResource != null && NewUnit.GetComponent<GatherResource>())
                        {
                            NewUnit.GetComponent<GatherResource>().SetTargetResource(TasksQueue[0].TargetResource);
                        }
                    }
                }
                else
                {
                    //if it's a MP game, then ask the server to spawn the unit.
                    //send input action to the input manager
                    InputVars NewInputAction = new InputVars();
                    //mode:
                    NewInputAction.SourceMode = (byte)InputSourceMode.Create;

                    NewInputAction.Source = UnitPrefab.gameObject;
                    NewInputAction.Target = (TaskHolder == TaskHolders.Building) ? RefBuilding.gameObject : null;

                    NewInputAction.InitialPos = GetSpawnPosition();

                    InputManager.SendInput(NewInputAction);
                }

            }
            else if (TasksList[TasksQueue[0].ID].TaskType == TaskManager.TaskTypes.Research)
            { //if the tasks upgrades certain units' abilities:
                if (GameManager.MultiplayerGame == false)
                { //if this an offline game:
                    LaunchResearchTaskLocal(TasksQueue[0].ID); //launch the task directly
                }
                else
                {
                    LaunchResearchTask(TasksQueue[0].ID);
                }

            }
            else if (TasksList[TasksQueue[0].ID].TaskType == TaskManager.TaskTypes.Destroy)
            { //if this task has a goal to self destroy the building.
                DestroyTaskHolder();
            }
        }

        //a method to destroy the task holder:
        public void DestroyTaskHolder ()
        {
            if(TaskHolder == TaskHolders.Building)
            {
                //if this building is selected:
                if(SelectionMgr.SelectedBuilding == RefBuilding)
                    SelectionMgr.DeselectBuilding(); //deselect it.

                //Destroy building:
                RefBuilding.DestroyBuilding(false);
            }
            else
            {
                //deselect if selected:
                if (SelectionMgr.SelectedUnits.Contains(RefUnit))
                    SelectionMgr.DeselectUnit(RefUnit);

                //destroy the unit.
                RefUnit.DestroyUnit();
            }
        }

        //a method to launch a research task on this building:
        public void LaunchResearchTask(int ID)
        {
            if (GameManager.MultiplayerGame == true)
            { //if this is a MP game and it's the local player:
                if (GameMgr.IsLocalPlayer(gameObject))
                { //just checking if this is a local player:
                  //send input action to the input manager
                    InputVars NewInputAction = new InputVars();
                    //mode:
                    NewInputAction.SourceMode = (byte)InputSourceMode.CustomCommand;
                    NewInputAction.TargetMode = (byte)InputCustomMode.Research;

                    NewInputAction.Source = gameObject;
                    NewInputAction.Value = ID;

                    InputManager.SendInput(NewInputAction);
                }
            }
            else
            {
                //offline game? update the attack type directly:
                LaunchResearchTaskLocal(ID);
            }
        }

        public void LaunchResearchTaskLocal(int ID)
        {
            TaskManager.UnitResearchTask ResearchTask = TasksList[ID].UnitResearchSettings;
            if (ResearchTask.Prefabs.Length > 0)
            { //if there are actually units to upgrade:
                for (int n = 0; n < ResearchTask.Prefabs.Length; n++)
                {
                    //do the upgrades:
                    //register this upgrade in the unit manager for this current unit:
                    UnitManager.UpgradeListVars NewUpgradeList = new UnitManager.UpgradeListVars();
                    //set the upgrade values:
                    NewUpgradeList.Speed = ResearchTask.AddSpeed;
                    NewUpgradeList.UnitDamage = ResearchTask.AddUnitDamage;
                    NewUpgradeList.BuildingDamage = ResearchTask.AddBuildingDamage;
                    NewUpgradeList.SearchRange = ResearchTask.AddSearchRange;
                    NewUpgradeList.AttackReload = ResearchTask.AddAttackReload;
                    NewUpgradeList.MaxHealth = ResearchTask.AddMaxHealth;
                    //add the upgrade to the list:
                    GameMgr.UnitMgr.FactionUnitUpgrades[FactionID].UpgradeList.Add(NewUpgradeList);
                    //now add the unit to units to be upgraded list:
                    GameMgr.UnitMgr.FactionUnitUpgrades[FactionID].UnitsToUpgrade.Add(ResearchTask.Prefabs[n].Code);

                    if (FactionMgr.Units.Count > 0)
                    {
                        for (int x = 0; x < FactionMgr.Units.Count; x++)
                        { //go through the present units in the scene
                            if (FactionMgr.Units[x].Code == ResearchTask.Prefabs[n].Code)
                            { //
                                FactionMgr.Units[x].Speed += ResearchTask.AddSpeed;

                                if (ResearchTask.AddMaxHealth >= 0.0f) //if the max health is to be upgraded:
                                {
                                    FactionMgr.Units[x].MaxHealth += ResearchTask.AddMaxHealth;
                                    FactionMgr.Units[x].Health = FactionMgr.Units[x].MaxHealth;
                                }

                                FactionMgr.Units[x].NavAgent.speed += ResearchTask.AddSpeed;

                                if (FactionMgr.Units[x].AttackMgr)
                                {
                                    FactionMgr.Units[x].AttackMgr.UnitDamage += ResearchTask.AddUnitDamage;
                                    FactionMgr.Units[x].AttackMgr.BuildingDamage += ResearchTask.AddBuildingDamage;
                                    FactionMgr.Units[x].AttackMgr.SearchRange += ResearchTask.AddSearchRange;
                                    FactionMgr.Units[x].AttackMgr.AttackReload += ResearchTask.AddAttackReload;
                                }

                                if (SelectionMgr.SelectedUnits.Contains(FactionMgr.Units[x]))
                                { //if this unit is selected
                                    UIMgr.UpdateUnitUI(FactionMgr.Units[x]);
                                }
                            }
                        }
                    }
                }
            }

            //Remove the task:
            TasksList[ID].Reached = true;
            TotalTasksAmount--;
            //Sync the upgrade:
            CheckTaskUpgrades(ID, false, false);
        }

        //Cancel a task in progress:
        public void CancelInProgressTask(int ID)
        {
            //make sure the task ID is valid:
            if (TasksQueue.Count > ID && ID >= 0)
            {
                if (TasksQueue[ID].Upgrade == false)
                {
                    //If it's a task that produces units, then make sure we empty a slot in the population count:
                    if (TasksList[TasksQueue[ID].ID].TaskType == TaskManager.TaskTypes.CreateUnit)
                    {
                        UIMgr.GameMgr.Factions[FactionID].UpdateCurrentPopulation(-1); //update the population slots
                        if (GameManager.PlayerFactionID == FactionID)
                        {
                            UIMgr.UpdatePopulationUI();
                        }

                        //update the limits list:
                        FactionMgr.UpdateLimitsList(TasksList[TasksQueue[ID].ID].UnitCreationSettings.Prefabs[0].Code, false);
                    }
                    ResourceMgr.GiveBackResources(TasksList[TasksQueue[ID].ID].RequiredResources, FactionID); //Give back the task resources.
                }
                else
                {
                    ResourceMgr.GiveBackResources(TasksList[TasksQueue[ID].ID].UnitCreationSettings.Upgrades[TasksList[TasksQueue[ID].ID].CurrentUpgradeLevel].UpgradeResources, FactionID);
                }

                if (TasksQueue[ID].Upgrade == true || TasksList[TasksQueue[ID].ID].TaskType == TaskManager.TaskTypes.Research)
                {
                    CheckTaskUpgrades(TasksQueue[ID].ID, false, true);
                }

                //custom events
                if(GameMgr.Events)
                    GameMgr.Events.OnTaskCanceled(this, TasksQueue[ID].ID, ID);

                //Unity event:
                TasksList[TasksQueue[0].ID].TaskCancelEvent.Invoke();

                TasksList[TasksQueue[ID].ID].Active = false;
                TasksQueue.RemoveAt(ID);// Remove this task:

                if (ID == 0 && TasksQueue.Count > 0)
                {
                    //If it's the first task in the queue, reload the timer for the next task:
                    TaskQueueTimer = TasksList[TasksQueue[0].ID].ReloadTime;
                }

                UIMgr.UpdateTaskPanel();
                UIMgr.UpdateInProgressTasksUI();

            }
        }
    }
}
