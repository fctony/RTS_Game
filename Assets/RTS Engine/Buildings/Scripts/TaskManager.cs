using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
	public class TaskManager : MonoBehaviour {

		//task type
		public enum TaskTypes {
			Null,
			Mvt,
			PlaceBuilding, Build,
			ResourceGen, Collect,
			Convert,
			Heal,
			APCRelease, APCCall, 
			CreateUnit, DestroyBuilding, Research, TaskUpgrade, UpgradeBuilding, CancelPendingTask, //building related tasks:
			ToggleInvisibility, //invisibility tasks
			AttackTypeSelection, Attack, //attack tasks
            ToggleWander, //Wandering tasks
            Destroy,
            CustomTask
        };

        //Unit Upgrade Vars:
        [System.Serializable]
        public class UnitResearchTask
        {
            public float AddSpeed = 1.0f;
            public float AddUnitDamage = 1.0f;
            public float AddBuildingDamage = 1.0f;
            public float AddAttackReload = -0.2f;
            public float AddSearchRange = 3.0f;
            public float AddMaxHealth = 50.0f;

            public Unit[] Prefabs;
        }

        //Unit Creation Vars:
        [System.Serializable]
        public class UnitCreationTask
        {
            public Unit[] Prefabs; //one prefab of this list will be randomly chosen to be created. Simply have one element here if you wish to use one prefab.

            public List<UnitUpgradeSubTask> Upgrades = new List<UnitUpgradeSubTask>();

            //Task upgrades vars:
            [System.Serializable]
            public class UnitUpgradeSubTask
            {
                public Unit[] TargetPrefabs; //Target unit to upgrade to.
                public string UpgradeDescription ="describe the upgrade task here"; //a short description of the upgrade.
                public int TaskPanelCategory = 0; //same as above, but for upgrade tasks of this task.
                public Sprite UpgradeIcon; //The icon that will appear in the task panel to launch this upgrade.
                public float UpgradeReload = 5.0f; //how long will the upgrade last to take effect.
                public ResourceManager.Resources[] UpgradeResources; //Resources required to to upgrade the task.

                public Sprite NewTaskIcon; //The icon that will replace the old task's icon.
                public string NewTaskDescription = "new description for the task after upgrade"; //the new description of the task.
                public ResourceManager.Resources[] NewTaskResources; //Resources required to complete the task after the upgrade (leave empty to make no changes).
                public float NewReloadTime = 3.0f; //how long will the task take after the upgrade.
            }
        }

        //Task panel:
        [Header("Task Components:")]
        //icons for component tasks for units and their UI task button parent category (if you don't want to use task components, then simply don't assign the icons below):
        public Sprite MvtTaskIcon;
        public int MvtTaskCategory = 0;

        public Sprite BuildTaskIcon;
        public int BuildTaskCategory = 0;

        public Sprite CollectTaskIcon;
        public int CollectTaskCategory = 0;

        public Sprite AttackTaskIcon;
        public int AttackTaskCategory = 0;

        public Sprite HealTaskIcon;
        public int HealTaskCategory = 0;

        public Sprite ConvertTaskIcon;
        public int ConvertTaskCategory = 0;

        public Sprite EnableWanderIcon;
        public Sprite DisableWanderIcon;
        public int WanderTaskCategory = 0;

        [HideInInspector]
        public TaskManager.TaskTypes AwaitingTaskType; //registers the pending task type:
        public bool ChangeMouseTexture = false; //change the mouse texture when having an awaiting task type?

        GameManager GameMgr;
        SelectionManager SelectionMgr;
        BuildingPlacement PlacementMgr;
        UIManager UIMgr;
        ResourceManager ResourceMgr;

        void Awake()
        {
            AwaitingTaskType = TaskManager.TaskTypes.Null;
        }

        void Start ()
        {
            GameMgr = GameManager.Instance;
            SelectionMgr = GameMgr.SelectionMgr;
            PlacementMgr = GameMgr.PlacementMgr;
            UIMgr = GameMgr.UIMgr;
            ResourceMgr = GameMgr.ResourceMgr;
        }

        //Component Tasks:
        public void SetAwaitingTaskType(TaskManager.TaskTypes TaskType, Sprite Sprite)
        {
            AwaitingTaskType = TaskType; //set the new task type
            if (ChangeMouseTexture == true && Sprite != null)
            { //if it is allowed to change the mouse texture
              //change it:
                Cursor.SetCursor(Sprite.texture, Vector2.zero, CursorMode.Auto);
            }
        }

        //reset the awaiting task type:
        public void ResetAwaitingTaskType()
        {
            AwaitingTaskType = TaskManager.TaskTypes.Null;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        public enum AddTaskMsg {Success, Destroyed, Upgrading, LowHealth, MaxTasksReached, LowResources, MaxPopulationReached, LimitReached}

        //for local player only, not for NPC factions
        public AddTaskMsg CanAddTask(TaskLauncher TaskComp, int FactionID, int TaskID, TaskTypes TaskType)
        {
            if (TaskComp.TaskHolder == TaskLauncher.TaskHolders.Unit) //if the task holder is a unit
            {
                if(TaskComp.RefUnit.Dead == true) //if the unit is already dead
                {
                    if (FactionID == GameManager.PlayerFactionID) //message and sound effect if it is the player faciton
                    {
                        UIMgr.ShowPlayerMessage("Unit is dead, can not launch tasks!", UIManager.MessageTypes.Error);
                        AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, TaskComp.DeclinedTaskAudio, false); //Declined task audio.
                    }
                    return AddTaskMsg.Destroyed;
                }
            }
            else //if not, make sure the building is built, not destroyed, not upgrading and has enough health.
            {
                if (TaskComp.RefBuilding.Destroyed == true) //if the unit is already dead
                {
                    if (FactionID == GameManager.PlayerFactionID) //message and sound effect if it is the player faciton
                    {
                        UIMgr.ShowPlayerMessage("Building is destroyed, can not launch tasks!", UIManager.MessageTypes.Error);
                        AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, TaskComp.DeclinedTaskAudio, false); //Declined task audio.
                    }
                    return AddTaskMsg.Destroyed;
                }
                else if(TaskComp.RefBuilding.BuildingUpgrading == true) //if the building is currently upgrading:
                {
                    if (FactionID == GameManager.PlayerFactionID) //message and sound effect if it is the player faciton
                    {
                        UIMgr.ShowPlayerMessage("Building is upgrading, can not launch tasks at the moment!", UIManager.MessageTypes.Error);
                        AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, TaskComp.DeclinedTaskAudio, false); //Declined task audio.
                    }
                    return AddTaskMsg.Upgrading;
                }
            }
            
            //Always check that the health is above the minimal limit to launch tasks and that the building was built (to max health) at least once:
            if (TaskComp.GetTaskHolderHealth() < TaskComp.MinTaskHealth)
            {
                if (FactionID == GameManager.PlayerFactionID) //message and sound effect if it is the player faciton
                {
                    UIMgr.ShowPlayerMessage("Health is too low to launch task!", UIManager.MessageTypes.Error);
                    AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, TaskComp.DeclinedTaskAudio, false); //Declined task audio.
                }
                return AddTaskMsg.LowHealth;
            }

            if (TaskComp.MaxTasks <= TaskComp.TasksQueue.Count)
            {
                if (FactionID == GameManager.PlayerFactionID) //message and sound effect if it is the player faciton
                {
                    //Notify the player that the maximum amount of tasks for this building has been reached
                    UIMgr.ShowPlayerMessage("Maximum building tasks has been reached", UIManager.MessageTypes.Error);
                    AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, TaskComp.DeclinedTaskAudio, false); //Declined task audio.
                }
                return AddTaskMsg.MaxTasksReached;
            }

            //Do we have enough resources for this task?
            if (TaskType == TaskManager.TaskTypes.TaskUpgrade)
            { //upgrade task:
              //Make sure there are enough resources
                if (ResourceMgr.CheckResources(TaskComp.TasksList[TaskID].UnitCreationSettings.Upgrades[TaskComp.TasksList[TaskID].CurrentUpgradeLevel].UpgradeResources, FactionID) == false)
                {
                    if (FactionID == GameManager.PlayerFactionID) //message and sound effect if it is the player faciton
                    {
                        UIMgr.ShowPlayerMessage("Not enough resources to launch upgrade task!", UIManager.MessageTypes.Error);
                        AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, TaskComp.DeclinedTaskAudio, false); //Declined task audio.
                    }
                    return AddTaskMsg.LowResources;
                }
            }
            else
            {
                if (ResourceMgr.CheckResources(TaskComp.TasksList[TaskID].RequiredResources, FactionID) == false)
                {
                    if (FactionID == GameManager.PlayerFactionID) //message and sound effect if it is the player faciton
                    {
                        UIMgr.ShowPlayerMessage("Not enough resources to launch task!", UIManager.MessageTypes.Error);
                        AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, TaskComp.DeclinedTaskAudio, false); //Declined task audio.
                    }
                    return AddTaskMsg.LowResources;
                }

                if (TaskType == TaskManager.TaskTypes.CreateUnit) 
                { //create unit task
                    if (GameMgr.Factions[FactionID].GetCurrentPopulation() >= GameMgr.Factions[FactionID].GetMaxPopulation())
                    {
                        if (FactionID == GameManager.PlayerFactionID) //message and sound effect if it is the player faciton
                        {
                            //Inform the player that there's no more room for new units.
                            UIMgr.ShowPlayerMessage("Maximum population has been reached!", UIManager.MessageTypes.Error);
                            AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, TaskComp.DeclinedTaskAudio, false); //Declined task audio.
                        }
                        return AddTaskMsg.MaxPopulationReached;
                    }
                    //if there's population slots but the local faction already hit the limit with this faction
                    else if (GameMgr.Factions[GameManager.PlayerFactionID].FactionMgr.HasReachedLimit(TaskComp.TasksList[TaskID].UnitCreationSettings.Prefabs[0].Code))
                    {
                        if (FactionID == GameManager.PlayerFactionID) //message and sound effect if it is the player faciton
                        {
                            //inform the player that he can't create this unit
                            UIMgr.ShowPlayerMessage("This unit has reached its creation limit", UIManager.MessageTypes.Error);
                            AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, TaskComp.DeclinedTaskAudio, false); //Declined task audio.
                        }
                        return AddTaskMsg.MaxPopulationReached;
                    }
                }
            }

            return AddTaskMsg.Success;
        }

        //This component handles adding tasks to the task queue of a Task Launcher component
        public AddTaskMsg AddTask(TaskLauncher TaskComp, int TaskID, TaskTypes TaskType)
        {
            //If this task is simply cancelling a pending task, then execute it directly and don't proceed:
            if(TaskType == TaskTypes.CancelPendingTask)
            {
                TaskComp.CancelInProgressTask(TaskID);
                return AddTaskMsg.Success; //instant success
            }

            //check if the task can be added or not.
            AddTaskMsg addTaskMsg = CanAddTask(TaskComp, TaskComp.FactionID, TaskID, TaskType);
            if (addTaskMsg != AddTaskMsg.Success) //if it's not success
                return addTaskMsg; //then return failure reason and stop

            if (TaskType == TaskTypes.CreateUnit) //if this is a unit creation task
            {
                GameMgr.Factions[TaskComp.FactionID].UpdateCurrentPopulation(1); //add population.

                if (GameManager.PlayerFactionID == TaskComp.FactionID)
                    GameMgr.UIMgr.UpdatePopulationUI(); //if it's the local player then change the population UI.

                //update the limits list:
                GameMgr.Factions[TaskComp.FactionID].FactionMgr.UpdateLimitsList(TaskComp.TasksList[TaskID].UnitCreationSettings.Prefabs[0].Code, true);
            }
            else
            {
                GameMgr.UIMgr.HideTooltip(); //if this is another task then simply hide the tooltip as the actual task would disappear upon activation
            }

            //Add the new task to the building's task queue
            TaskLauncher.TasksQueueInfo Item = new TaskLauncher.TasksQueueInfo();
            Item.ID = TaskID;
            Item.Upgrade = (TaskType == TaskTypes.TaskUpgrade);
            TaskComp.TasksQueue.Add(Item);

            if (Item.Upgrade == false)
            { //if the task is no upgrade task

                //Launch the timer if there was no other tasks, else, the timer will launch automatically.
                if (TaskComp.TasksQueue.Count == 1)
                {
                    TaskComp.TaskQueueTimer = TaskComp.TasksList[TaskID].ReloadTime;
                }
                //Take the required resources:
                GameMgr.ResourceMgr.TakeResources(TaskComp.TasksList[TaskID].RequiredResources, TaskComp.FactionID);

            }
            else //if this an upgrade task, use the upgrade timer
            {
                //Launch the timer if there was no other tasks, else, the timer will launch automatically.
                if (TaskComp.TasksQueue.Count == 1)
                {
                    TaskComp.TaskQueueTimer = TaskComp.TasksList[TaskID].UnitCreationSettings.Upgrades[TaskComp.TasksList[TaskID].CurrentUpgradeLevel].UpgradeReload;
                }
                //take the upgrade's resource and launch it.
                GameMgr.ResourceMgr.TakeResources(TaskComp.TasksList[TaskID].UnitCreationSettings.Upgrades[TaskComp.TasksList[TaskID].CurrentUpgradeLevel].UpgradeResources, TaskComp.FactionID);
            }

            //custom events:
            if(GameMgr.Events)
                GameMgr.Events.OnTaskLaunched(TaskComp, TaskID, TaskComp.TasksQueue.Count-1);

            //Unity event:
            TaskComp.TasksList[TaskID].TaskLaunchEvent.Invoke();

            if (GameManager.PlayerFactionID == TaskComp.FactionID) //if this is the local player:
            {
                //If this is an upgrade task or a research one:
                if (Item.Upgrade == true || TaskComp.TasksList[TaskID].TaskType == TaskTypes.Research)
                {
                    //set the task status to active
                    TaskComp.TasksList[TaskID].Active = true;
                    TaskComp.CheckTaskUpgrades(TaskID, true, false);
                }

                if (TaskComp.IsTaskHolderSelected()) //if the task holder is selected
                {
                    GameMgr.UIMgr.UpdateInProgressTasksUI(); //update the UI:
                    GameMgr.UIMgr.UpdateTaskPanel();
                }

                AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, TaskComp.LaunchTaskAudio, false); //Launched task audio.
            }

            //If there's a task component associated with this task:
            if(TaskComp != null)
            {
                //If we're only allowed to launch this task once:
                if(TaskComp.TasksList[TaskID].UseOnce == true)
                {
                    //remove the task from the tasks list so it won't be used anymore.
                    TaskComp.TasksList.RemoveAt(TaskID);
                    //reload the task panel UI:
                    UIMgr.UpdateTaskPanel();
                }
            }

            return AddTaskMsg.Success;
        }

        //This component handles tasks athat do not get added to the task quque and are not handled by the task launcher.
        public void AddTask(int TaskID, TaskTypes TaskType, Sprite TaskSprite)
        {
            APC APCComp = null;

            switch (TaskType)
            {
                case TaskManager.TaskTypes.ResourceGen: //resource gen task:

                    SelectionMgr.SelectedBuilding.ResourceGen.CollectResources(SelectionMgr.SelectedBuilding.ResourceGen.ReadyToCollect[TaskID]);

                    break;
                case TaskManager.TaskTypes.APCRelease: //APC release task.

                    //get the APC component:
                    if (SelectionMgr.SelectedBuilding)
                    {
                        APCComp = SelectionMgr.SelectedBuilding.APCMgr;
                    }
                    else
                    {
                        APCComp = SelectionMgr.SelectedUnits[0].APCMgr;
                    }

                    //drop off units
                    APCComp.DropOffUnits(TaskID);

                    break;
                case TaskManager.TaskTypes.APCCall: //apc calling units

                    //get the APC component:
                    if (SelectionMgr.SelectedBuilding)
                    {
                        APCComp = SelectionMgr.SelectedBuilding.APCMgr;
                    }
                    else
                    {
                        APCComp = SelectionMgr.SelectedUnits[0].APCMgr;
                    }

                    APCComp.CallForUnits();
                    break;
                case TaskManager.TaskTypes.PlaceBuilding:

                    //make sure the building hasn't reached its limits:
                    if (!GameMgr.Factions[GameManager.PlayerFactionID].FactionMgr.HasReachedLimit(PlacementMgr.AllBuildings[TaskID].Code))
                    {
                        //Start building:
                        PlacementMgr.StartPlacingBuilding(TaskID);
                    }
                    else
                    {
                        //building limit reached, send message to player:
                        UIMgr.ShowPlayerMessage("Building " + PlacementMgr.AllBuildings[TaskID].Name + "has reached its placement limit", UIManager.MessageTypes.Error);
                    }

                    break;
                case TaskManager.TaskTypes.ToggleInvisibility: //Toggling invisibility:

                    SelectionMgr.SelectedUnits[0].InvisibilityMgr.ToggleInvisibility();

                    break;

                case TaskManager.TaskTypes.AttackTypeSelection:

                    //make sure the attack type is not in cooldown mode
                    if (SelectionMgr.SelectedUnits[0].MultipleAttacksMgr.AttackTypes[TaskID].InCoolDownMode == false)
                    {
                        SelectionMgr.SelectedUnits[0].MultipleAttacksMgr.EnableAttackType(TaskID);
                    }
                    else
                    {
                        UIMgr.ShowPlayerMessage("Attack type in cooldown mode!", UIManager.MessageTypes.Error);
                    }

                    break;
                case TaskManager.TaskTypes.ToggleWander:

                    SelectionMgr.SelectedUnits[0].Wandering = !SelectionMgr.SelectedUnits[0].Wandering;
                    if (SelectionMgr.SelectedUnits[0].Wandering == true) //if wandering is now enabled
                    {
                        if (SelectionMgr.SelectedUnits[0].FixedWanderCenter == true)
                        {
                            SelectionMgr.SelectedUnits[0].WanderCenter = SelectionMgr.SelectedUnits[0].transform.position;
                        }
                        //make the unit wander:
                        SelectionMgr.SelectedUnits[0].Wander();
                        //update the tasks UI:
                        UIMgr.UpdateTaskPanel();
                    }
                    break;
                case TaskManager.TaskTypes.UpgradeBuilding:

                    SelectionMgr.SelectedBuilding.CheckBuildingUpgrade();

                    break;
                default:
                    SetAwaitingTaskType(TaskType, TaskSprite);
                    break;
            }
        }
    }
}