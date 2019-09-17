using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* NPC Unit Regulator script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    //[CreateAssetMenu(fileName = "NewUnitRegulator", menuName = "RTS Engine/Unit Regulator", order = 53)]
    public class NPCUnitRegulator : NPCRegulator<Unit>
    {
        //define the units amount ratio in relation to the population slots available for the faction
        public FloatRange ratioRange = new FloatRange(0.1f, 0.2f);
        private float ratio;

        /* Below are attribtues that manage the actual creation of the unit(s) */

        public bool autoCreate = true; //Automatically create this unit type to meet the ratio requirements.
        //whether Auto Create is true or false, the minimum amount chosen above must be met.
        
        //target amount that this unit regulator is trying to create will be here:
        private int targetAmount = 0;

        //struct has the source task launchers that can create the units regulated by this component
        public struct UnitCreatorInfo
        {
            public TaskLauncher taskLauncher; //reference to the actual task launcher that can create the above unit
            public int taskID; //ID of the unit creation task
        }

        //a list of the task launchers that can create the units defined in this component
        private List<UnitCreatorInfo> unitCreatorList = new List<UnitCreatorInfo>();

        //unit creator:
        private NPCUnitCreator unitCreator_NPC; //the NPC Unit Creator used to create all instances of the units regulated here.

        /* Initializing everything */
        public void Init(NPCManager npcMgr, NPCUnitCreator unitCreator_NPC)
        {
            //assign the appropriate faction manager and unit creator settings
            base.InitItem(npcMgr);
            this.unitCreator_NPC = unitCreator_NPC;

            //set the building code:
            code = prefabs[0].Code;

            //pick the rest random settings from the given info.
            ratio = ratioRange.getRandomValue();

            //update the target amount
            UpdateTargetAmount(GameManager.Instance.Factions[this.factionMgr.FactionID].GetMaxPopulation());

            //go through all spawned units to see if the units that should be regulated by this instance are created or not:
            foreach (Unit u in this.factionMgr.Units)
            {
                //only if the unit belongs to this regulator:
                if (u.Code == code)
                {
                    amount++;
                    currentInstances.Add(u);
                }
            }

            //go through all spawned task launchers of the faction and see if there's one that can create this unit type:
            foreach(TaskLauncher tl in this.factionMgr.TaskLaunchers)
            {
                UpdateUnitCreatorList(tl, true);
            }

            //start listening to the required delegate events:
            CustomEvents.UnitCreated += OnUnitCreated;
            CustomEvents.UnitConverted += OnUnitConverted;
            CustomEvents.UnitDead += OnUnitDead;
            CustomEvents.TaskLaunched += OnTaskLaunched;
            CustomEvents.TaskCanceled += OnTaskCanceled;
            CustomEvents.TaskLauncherAdded += OnTaskLauncherAdded;
            CustomEvents.TaskLauncherRemoved += OnTaskLauncherRemoved;
            CustomEvents.MaxPopulationUpdated += OnMaxPopulationUpdated;
        }

        void OnDisable()
        {
            //stop listening to the delegate events:
            CustomEvents.UnitCreated -= OnUnitCreated;
            CustomEvents.UnitConverted -= OnUnitConverted;
            CustomEvents.UnitDead -= OnUnitDead;
            CustomEvents.TaskLaunched -= OnTaskLaunched;
            CustomEvents.TaskCanceled -= OnTaskCanceled;
            CustomEvents.TaskLauncherAdded -= OnTaskLauncherAdded;
            CustomEvents.TaskLauncherRemoved -= OnTaskLauncherRemoved;
            CustomEvents.MaxPopulationUpdated -= OnMaxPopulationUpdated;
        }

        /* Regulating the unit */

        //remove a unit from the amount based on its code.
        public override void RemoveItem(Unit unit)
        {
            base.RemoveItem(unit);
            //if the target amount is now not reached anymore:
            if (!HasReachedMaxAmount())
                unitCreator_NPC.Activate(); //activate the unit creator
        }

        //check if a unit is regulated by this component or not:
        bool IsValidUnit(Unit unit)
        {
            return unit.FactionID == factionMgr.FactionID && unit.Code == code || currentInstances.Contains(unit);
        }

        //called whenever a task is launched
        void OnTaskLaunched(TaskLauncher taskLauncher, int taskID, int taskQueueID)
        {
            //if this task is supposed to create a unit and the task launcher belongs to this faction:
            if (taskLauncher.TasksList[taskID].TaskType == TaskManager.TaskTypes.CreateUnit && taskLauncher.FactionID == factionMgr.FactionID)
            {
                //the prefabs assigned have the same code.
                if (taskLauncher.TasksList[taskID].UnitCreationSettings.Prefabs.Length > 0)
                {
                    //we'll pick the first unit in array.
                    Unit firstUnit = taskLauncher.TasksList[taskID].UnitCreationSettings.Prefabs[0];
                    if (firstUnit.Code == code)
                    {
                        //increase amount
                        amount++;
                        pendingAmount++;
                    }
                }
            }
        }

        //called whenever a unit is spawned
        void OnUnitCreated (Unit unit)
        {
            //if the unit is managed by this regulator
            if(IsValidUnit(unit))
            {
                //add it to list:
                currentInstances.Add(unit);
                pendingAmount--; //decrease pending amount
            }
        }

        //called whenever a unit is destroyed
        void OnUnitDead(Unit unit)
        {
            //if the unit is regulated by this unit regulator
            if(IsValidUnit(unit))
            {
                RemoveItem(unit);
            }
        }

        //called whenever a unit is converted
        void OnUnitConverted (Unit sourceUnit, Unit targetUnit)
        {
            //if the target unit (one that has been converted) is regulated by this unit regulator
            if (IsValidUnit(targetUnit))
            {
                RemoveItem(targetUnit);
            }
        }

        //called whenever a task is cancelled
        void OnTaskCanceled(TaskLauncher taskLauncher, int taskID, int taskQueueID)
        {
            //if this task is supposed to create a unit and the task launcher belongs to this faction:
            if (taskLauncher.TasksList[taskID].TaskType == TaskManager.TaskTypes.CreateUnit && taskLauncher.FactionID == factionMgr.FactionID)
            {
                //the prefabs assigned have the same code.
                if (taskLauncher.TasksList[taskID].UnitCreationSettings.Prefabs.Length > 0)
                {
                    //we'll pick the first unit in array.
                    Unit firstUnit = taskLauncher.TasksList[taskID].UnitCreationSettings.Prefabs[0];
                    if (firstUnit.Code == code)
                    {
                        RemoveItem(firstUnit);
                    }
                }
            }
        }

        //called when the maximum population slots are updated
        public void OnMaxPopulationUpdated(GameManager.FactionInfo factionInfo, int value)
        {
            //if this update belongs to the faction managed by this component:
            if (factionInfo.FactionMgr.FactionID == factionMgr.FactionID)
            {
                UpdateTargetAmount(factionInfo.GetMaxPopulation());
            }
        }

        //update the target amount:
        public void UpdateTargetAmount(int maxPopulation)
        {
            targetAmount = (int)(maxPopulation * ratio); //calculate new target amount:

            //make sure the new target is in terms with the max and min values:
            if (targetAmount < minAmount)
                targetAmount = minAmount;
            if (targetAmount > maxAmount)
                targetAmount = maxAmount;

            //if the target amount is now not reached anymore:
            if (!HasReachedMaxAmount())
                unitCreator_NPC.Activate(); //activate the unit creator
        }

        //get the current target amount:
        public int GetTargetAmount ()
        {
            return targetAmount;
        }

        /* Unit creation */

        void OnTaskLauncherAdded(TaskLauncher taskLauncher, int taskID = -1, int taskQueueID = -1) //called when a new task launcher has been added
        {
            if (taskLauncher.FactionID == factionMgr.FactionID) //if the task launcher belongs to this faction
            {
                //update the source task launchers:
                UpdateUnitCreatorList(taskLauncher, true);
            }
        }

        void OnTaskLauncherRemoved(TaskLauncher taskLauncher, int taskID = -1, int taskQueueID = -1) //called when a task launcher has been removed
        {
            if (taskLauncher.FactionID == factionMgr.FactionID) //if the task launcher belongs to this faction
            {
                //update the source task launchers:
                UpdateUnitCreatorList(taskLauncher, false);
            }
        }

        //get the unit creators list:
        public List<UnitCreatorInfo> GetUnitCreatorList ()
        {
            return unitCreatorList;
        }

        //whenever a building is added or removed, this method will be called to add/remove it to the lists in the spawn units:
        public void UpdateUnitCreatorList (TaskLauncher taskLauncher, bool add)
        {
            //Make sure that this task launcher can actually create units and that there units to spawn
            if (taskLauncher.UnitCreationTasks.Count > 0)
            {
                //loop through the unit creation tasks
                foreach (int taskID in taskLauncher.UnitCreationTasks)
                {
                    if (add == false) //if this task launcher is getting removed.
                    {
                        int i = 0;
                        //go through all registerd task launchers in this component
                        while (i < unitCreatorList.Count)
                        {
                            if (unitCreatorList[i].taskLauncher == taskLauncher) //if the task launcher matches:
                            {
                                //remove it:
                                unitCreatorList.RemoveAt(i);
                            }
                            else
                            {
                                i++; //go to the next register unit creator
                            }
                        }
                    }
                    else //if we are adding this task launcher
                    {
                      //if there are valid prefab(s) assigned to this unit creation task
                      if(taskLauncher.TasksList[taskID].UnitCreationSettings.Prefabs.Length > 0)
                        {
                            string prefabCode = taskLauncher.TasksList[taskID].UnitCreationSettings.Prefabs[0].Code;
                            //if the unit code is included in the list => that unit is managed by this regulator
                            if (prefabCode == code)
                            {
                                //go ahead and add it:
                                UnitCreatorInfo newUnitCreator = new UnitCreatorInfo
                                {
                                    //set the unit creator info:
                                    taskLauncher = taskLauncher,
                                    taskID = taskID
                                };
                                //add it to the list:
                                unitCreatorList.Add(newUnitCreator);
                            }
                        }
                    }
                }
            }
        }

        //prioritize idle units in the list:
        public List<Unit> GetIdleUnitsFirst ()
        {
            List<Unit> idleUnits = new List<Unit>();
            List<Unit> allUnits = new List<Unit>();
            allUnits.AddRange(GetCurrentInstances());

            int i = 0;
            while (i < allUnits.Count)
            {
                //if the unit is idle, don't increment &:
                if (allUnits[i].IsIdle())
                {
                    idleUnits.Add(allUnits[i]); //add it to the idle units list
                    allUnits.RemoveAt(i); //remove from the all units list
                }
                else //unit is not idle, we'll keep it in the all units list and..
                    i++; //increment counter
            }

            //add the non idle units to the idle units list:
            idleUnits.AddRange(allUnits);
            return idleUnits; //list that has idle units first.
        }
    }
}