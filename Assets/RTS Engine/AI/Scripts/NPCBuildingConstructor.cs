using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* NPC Building Constructor script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class NPCBuildingConstructor : NPCComponent
    {
        public NPCUnitRegulator builderRegulator; //the main builder regulator unit.
        private NPCUnitRegulator builderRegulatorIns; //the active instance of the builder regulator.

        private List<Building> buildingsToConstruct = new List<Building>(); //holds a list of the faction buildings that need construction.
        //timer that each it's through and this component is active: it goes over the buildings in the above list and sends builders to construct it.
        public FloatRange constructionTimerRange = new FloatRange(4.0f, 7.0f);
        private float constructionTimer;

        public int initialQueueID = 1;

        //the value below represensts the ratio of the builders that will be assigned to each building:
        public FloatRange targetBuildersRatio = new FloatRange(0.5f, 0.8f);

        public bool constructOnDemand = true; //if another NPC component requests the construction of one of a building, is it allowed or not?

        private bool isActive = false; //is this component active?

        void Awake()
        {
            //reset construction timer:
            constructionTimer = constructionTimerRange.getRandomValue();

            //this component isn't active by default:
            isActive = false;
        }

        void Start()
        {
            //Go ahead and add the builder regulator (if there's one)..
            if (builderRegulator != null)
            {
                //.. to the NPC Unit Creator component
                builderRegulatorIns = npcMgr.unitCreator_NPC.ActivateUnitRegulator(builderRegulator);
            }
            else
            {
                //Error to user, because builder regulator hasn't been assigned and it is required:
                Debug.LogError("NPC Faction ID: " + factionMgr.FactionID + " NPC Building Constructor component doesn't have a builder regulator assigned!");
            }

            //add event listeners for following delegate events:
            CustomEvents.BuildingPlaced += OnBuildingPlaced;
            CustomEvents.BuildingHealthUpdated += OnBuildingHealthUpdated;
        }

        void OnDisable()
        {
            //remove delegate event listeners:
            CustomEvents.BuildingPlaced -= OnBuildingPlaced;
            CustomEvents.BuildingHealthUpdated -= OnBuildingHealthUpdated;
        }

        //called when a building is placed:
        void OnBuildingPlaced (Building building)
        {
            if(building.FactionID == factionMgr.FactionID) //if it belongs to this faction
            {
                //add it to the buildings in construction list:
                OnConstructionStateChange(building, true);
            }
        }

        //called when a building has its health updated:
        void OnBuildingHealthUpdated (Building building, float value, GameObject source)
        {
            //if the building belongs to this faction:
            if (building.FactionID == factionMgr.FactionID)
            {
                //if the building doesn't have max health:
                if (building.Health < building.MaxHealth)
                {
                    //add it to the buildings in construction list.
                    OnConstructionStateChange(building, true);
                }
                else
                {
                    //remove it from the buildings in construction list.
                    OnConstructionStateChange(building, false);
                }
            }
        }

        //called when a building is to be added or removed from the buildings that need construction list
        public void OnConstructionStateChange(Building building, bool add)
        {
            if (add == false) //remove building
                buildingsToConstruct.Remove(building);
            else //add building to list
            {
                //if the building is not in the list already:
                if (!buildingsToConstruct.Contains(building))
                {
                    buildingsToConstruct.Add(building);
                    isActive = true; //NPC Building Constructor is now active again.

                    //add the construction task to the task manager queue:
                    npcMgr.taskManager_NPC.AddTask(NPCTaskTypes.constructBuilding, building.gameObject, initialQueueID);
                }
            }
        }

        void Update()
        {
            //if the component is active:
            if(isActive == true)
            {
                //checking buildings timer:
                if (constructionTimer > 0)
                    constructionTimer -= Time.deltaTime;
                else
                {
                    //reset construction timer:
                    constructionTimer = constructionTimerRange.getRandomValue();

                    //set to non active at the beginning.
                    isActive = false;

                    //go through the buildings to construct list
                    foreach (Building b in buildingsToConstruct)
                    {
                        //there are still buildings to consruct, then:
                        isActive = true; //we'll be checking again soon.

                        //see if the amount hasn't been reached:
                        if (GetTargetBuildersAmount(b) > b.WorkerMgr.CurrentWorkers)
                        {
                            //request to send more workers then:
                            OnBuildingConstructionRequest(b, true, false);
                        }
                    }
                }
            }
        }

        public int GetTargetBuildersAmount (Building building)
        {
            //how many builders do we need to assign for this building?
            int targetBuildersAmount = (int)(building.WorkerMgr.WorkerPositions.Length * targetBuildersRatio.getRandomValue());
            if (targetBuildersAmount <= 0) //can't be lower than one.
                targetBuildersAmount = 1;
            return targetBuildersAmount;
        }

        //determine whether a building is in the buildings to construct queue or not
        public bool IsBuildingUnderConstruction (Building building)
        {
            return buildingsToConstruct.Contains(building);
        }

        //used to request the NPC Building Constructor to send units to construct this building.
        public void OnBuildingConstructionRequest (Building building, bool auto, bool force)
        {
            if (auto == false && constructOnDemand == false) //if this is a request from another NPC component and this component doesn't allow that.
                return; //do not proceed.

            //making sure the building is valid:
            if(building != null)
            {
                //making sure the building needs construction:
                if(building.Health < building.MaxHealth)
                {
                    //how much builders does this building can have?
                    int requiredBuilders = GetTargetBuildersAmount(building) - building.WorkerMgr.CurrentWorkers;

                    int i = 0; //counter.
                    List<Unit> currentBuilders = builderRegulatorIns.GetIdleUnitsFirst(); //get the list of the current faction builders.

                    //while we still need builders for the building and we haven't gone through all builders.
                    while (i < currentBuilders.Count && requiredBuilders > 0)
                    {
                        //making sure the builder is valid:
                        if(currentBuilders[i] != null)
                        {
                            //is the builder currently in idle mode or do we force him to construct this building?
                            //& make sure it's not already constructing a building.
                            if ((currentBuilders[i].IsIdle() || force == true) && currentBuilders[i].BuilderMgr.TargetBuilding == null)
                            {
                                //send to construct the building:
                                currentBuilders[i].BuilderMgr.SetTargetBuilding(building);
                                //decrement amount of required builders:
                                requiredBuilders--;
                            }
                        }

                        i++;
                    }
                }
            }
        }
    }
}
