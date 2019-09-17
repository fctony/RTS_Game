using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTSEngine;

/* NPC Unit Creator script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class NPCUnitCreator : NPCComponent
    {
        //The independent unit regulators list are not managed by any other NPC component.
        public List<NPCUnitRegulator> independentUnitRegulators = new List<NPCUnitRegulator>();

        //all the information needed regarding an active unit regulator
        public class ActiveUnitRegulator
        {
            public NPCUnitRegulator instance; //the active instance of the unit regulator.
            public float spawnTimer; //spawn timer for the active unit regulators.

            public NPCUnitRegulator source; //the NPC Unit Regulator source/prefab.
        }
        private List<ActiveUnitRegulator> activeUnitRegulators = new List<ActiveUnitRegulator>(); //holds the active unit regulators

        //is the unit creator active or in idle mode?
        private bool isActive = false;

        //activate the unit creator
        public void Activate()
        {
            isActive = true;
        }

        //other components:
        private TaskManager taskMgr;

        //a method to add a new instance of a unit regulator
        public NPCUnitRegulator ActivateUnitRegulator(NPCUnitRegulator unitRegulator)
        {
            //see if the unit regulator is already active or not
            ActiveUnitRegulator aue = IsUnitRegulatorActive(unitRegulator);
            if (aue != null) //if it is
                return aue.instance; //return the already active instance.

            //default 
            ActiveUnitRegulator newUnitRegulator = new ActiveUnitRegulator()
            {
                //create new instance
                instance = Instantiate(unitRegulator),
                source = unitRegulator, //set the prefab/resource
                //initial spawning timer: regular spawn reload + start creating after value
                spawnTimer = unitRegulator.spawnReloadRange.getRandomValue() + unitRegulator.startCreatingAfter.getRandomValue()
            };

            //add it to the active unit regulators list:
            activeUnitRegulators.Add(newUnitRegulator);

            newUnitRegulator.instance.Init(npcMgr, this); //initialize the unit regulator.

            //whenever a new regulator is added to the active regulators list, then move the unit creator into the active state
            isActive = true;

            return newUnitRegulator.instance;
        }

        //a method to check if a unit regulator is already active or not.
        public ActiveUnitRegulator IsUnitRegulatorActive (NPCUnitRegulator unitRegulator)
        {
            //go through all active unit regulators:
            foreach(ActiveUnitRegulator aur in activeUnitRegulators)
            {
                if(aur.source == unitRegulator) //if the source matches the input unit regulator
                {
                    //then return this active unit regulator.
                    return aur;
                }
            }

            return null;
        }

        //a method to remove active instances:
        public void DestroyActiveRegulators ()
        {
            foreach (ActiveUnitRegulator aur in activeUnitRegulators) //go through the active regulators
                Destroy(aur.instance); //destroy the active instances.
            //clear the list:
            activeUnitRegulators.Clear();   
        }

        void Awake ()
        {
            //clear the active unit regulator list per default:
            activeUnitRegulators.Clear();
        }

        void Start()
        {
            taskMgr = GameManager.Instance.TaskMgr; //get the task manager (will be needed to launch tasks).

            //activate the independent unit regulators 
            foreach (NPCUnitRegulator nue in independentUnitRegulators)
                ActivateUnitRegulator(nue);
        }

        void Update()
        {
            //if the unit creator is active:
            if (isActive == true)
            {
                isActive = false; //assume that the unit creator has finished its job with the current active unit regulators.
                //go through the active unit regulators:
                foreach (ActiveUnitRegulator aur in activeUnitRegulators)
                {
                    //if we can auto create this:
                    if (aur.instance.HasReachedMaxAmount() == false)
                    {
                        //we are active since the max amount of one of the units regulated hasn't been reached
                        isActive = true;

                        //spawn timer:
                        if (aur.spawnTimer > 0.0f)
                            aur.spawnTimer -= Time.deltaTime;
                        else
                        {
                            //reload timer:
                            aur.spawnTimer = aur.instance.spawnReloadRange.getRandomValue();
                            //attempt to create as much as it is possible from this unit:
                            OnCreateUnitRequest(aur.instance, true, aur.instance.GetTargetAmount());
                        }
                    }
                }
            }
        }
        
        //method that attempts to create the unit from a regulator
        public void OnCreateUnitRequest (NPCUnitRegulator instance, bool auto, int maxAmount)
        {
            //if this attempt is done automatically (from the NPC Unit Creator itself) and the regulator doesn't allow it
            if (auto == true && instance.autoCreate == false)
            {
                return; //do not proceed.
            }
            
            //if this has been requested from another NPC component and the regulator doesn't allow it
            if(auto == false && instance.createOnDemand == false)
            {
                return; //do not proceed.
            }

            int createAmount = maxAmount; //this will be the amount that we aim to spawn
            if(!instance.HasReachedMaxAmount() && createAmount > 0) //as long as we haven't reached the max amount
            {
                if(instance.GetUnitCreatorList().Count > 0) //if there are task launchers assigned to this regulator:
                {
                    //go through the task launchers that this unit regulator uses:
                    foreach (NPCUnitRegulator.UnitCreatorInfo uci in instance.GetUnitCreatorList())
                    {
                        TaskManager.AddTaskMsg addTaskMsg = TaskManager.AddTaskMsg.Success;
                        //as long as launching the unit creation task is successful and we still have units to create
                        while (addTaskMsg == TaskManager.AddTaskMsg.Success && createAmount > 0)
                        {
                            addTaskMsg = taskMgr.AddTask(uci.taskLauncher, uci.taskID, TaskManager.TaskTypes.CreateUnit);
                            //handle cases:
                            switch(addTaskMsg)
                            {
                                case TaskManager.AddTaskMsg.Success: //in case of success
                                    createAmount--; //decrease amount required
                                    break;
                                case TaskManager.AddTaskMsg.MaxPopulationReached: //in case of failure due to max population reach.
                                    //ask the NPC population manager to add a new population building and stop the whole process:
                                    npcMgr.populationManager_NPC.OnAddPopulationRequest();
                                    return;
                                default:
                                    break; //FUTURE FEATURE: HANLDE OTHER FAILURE MESSAGES SUCH AS: sending builders to fix low health buildings, 
                                    //... or asking resource manager to collect resource in case of missing resource.
                            }
                        }

                        //as soon we create all the required units then stop this whole thing:
                        if (createAmount == 0)
                            return;
                    }
                }
                else
                {
                    //FUTURE FEATURE: Communicate with the NPC Building Creator in order to ask to spawn one of the task launchers.
                }
            }
        }

    }
}
