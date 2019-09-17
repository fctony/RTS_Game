using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* NPC Building Creator script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class NPCBuildingCreator : NPCComponent
    {
        //The independent building regulators list are not managed by any other NPC component.
        public List<NPCBuildingRegulator> independentBuildingRegulators = new List<NPCBuildingRegulator>();
        public class BuildingCenterRegulator
        {
            public Building buildingCenter; //the building center where the whole building regulation will happen at.
            //all the information needed regarding an active building regulator
            public class ActiveBuildingRegulator
            {
                public NPCBuildingRegulator instance; //the active instance of the building regulator.
                public float spawnTimer; //spawn timer for the active building regulators.

                public NPCBuildingRegulator source; //the source/prefab of the active regulator instance.
            }
            public List<ActiveBuildingRegulator> activeBuildingRegulators = new List<ActiveBuildingRegulator>(); //holds the active building regulators
        }
        private List<BuildingCenterRegulator> buildingCenterRegulators = new List<BuildingCenterRegulator>(); //a list that holds building centers and their corresponding active building regulators

        //is the building creator active or in idle mode?
        private bool isActive = false;

        //activate the building creator
        public void Activate()
        {
            isActive = true;
        }

        //other components:
        ResourceManager resourceMgr;

        //a method to add a new instance of a unit regulator
        public void ActivateBuildingRegulator(NPCBuildingRegulator buildingRegulator)
        {
            //go through the building cenetr regulators:
            foreach (BuildingCenterRegulator bcr in buildingCenterRegulators)
            {
                ActivateBuildingRegulator(buildingRegulator, bcr);
            }
        }

        public NPCBuildingRegulator ActivateBuildingRegulator(NPCBuildingRegulator buildingRegulator, BuildingCenterRegulator centerRegulator)
        {
            BuildingCenterRegulator.ActiveBuildingRegulator abr = IsRegulatorActive(buildingRegulator, centerRegulator);
            if(abr != null) //regulator is already active
            {
                return abr.instance; //return the instance.
            }

            //we will be activating the building regulator for the input center only
            BuildingCenterRegulator.ActiveBuildingRegulator newBuildingRegulator = new BuildingCenterRegulator.ActiveBuildingRegulator()
            {
                //create new instance
                instance = Instantiate(buildingRegulator),
                source = buildingRegulator, //assign the source/prefab.
                //initial spawning timer: regular spawn reload + start creating after value
                spawnTimer = buildingRegulator.spawnReloadRange.getRandomValue() + buildingRegulator.startCreatingAfter.getRandomValue()
            };

            //add it to the active building regulators list:
            centerRegulator.activeBuildingRegulators.Add(newBuildingRegulator);

            newBuildingRegulator.instance.Init(npcMgr, this, centerRegulator.buildingCenter); //initialize the building regulator.

            //whenever a new regulator is added to the active regulators list, then move the building creator into the active state
            isActive = true;

            //return the new created instance:
            return newBuildingRegulator.instance;
        }

        //is a certain NPC Building Regulator already active?
        public BuildingCenterRegulator.ActiveBuildingRegulator IsRegulatorActive (NPCBuildingRegulator nbr, BuildingCenterRegulator centerRegulator)
        {
            //go through the building center's active building regulators:
            foreach(BuildingCenterRegulator.ActiveBuildingRegulator abr in centerRegulator.activeBuildingRegulators)
            {
                if (abr.source == nbr) //the regulator is already active:
                    return abr;
            }

            return null; //regulator hasn't been found.
        }

        //a method to remove active instances:
        public void DestroyActiveRegulators()
        {
            foreach (BuildingCenterRegulator bcr in buildingCenterRegulators) //go through the active regulators
                foreach (BuildingCenterRegulator.ActiveBuildingRegulator abr in bcr.activeBuildingRegulators)
                    Destroy(abr.instance); //destroy the active instances.
            //clear the list:
            buildingCenterRegulators.Clear();
        }

        void Awake()
        {
            //clear the active unit regulator list per default:
            buildingCenterRegulators.Clear();

            //start listening to the required delegate events:
            CustomEvents.BuildingDestroyed += OnBuildingDestroyed;
            CustomEvents.BorderActivated += OnBorderActivated;
        }

        void OnDisable()
        {
            //stop listening to the delegate events:
            CustomEvents.BuildingDestroyed -= OnBuildingDestroyed;
            CustomEvents.BorderActivated -= OnBorderActivated;
        }

        //called whenever a border component is activated:
        void OnBorderActivated (Border border)
        {
            //if the building belongs to this faction & has a Border component:
            if (border.MainBuilding.FactionID == factionMgr.FactionID)
            {
                //new building center added and therefore create a new element for it in the list:
                BuildingCenterRegulator newCenterRegulator = new BuildingCenterRegulator
                {
                    buildingCenter = border.MainBuilding,
                    activeBuildingRegulators = new List<BuildingCenterRegulator.ActiveBuildingRegulator>()
                };
                //add it to the list:
                buildingCenterRegulators.Add(newCenterRegulator);

                //activate the independent building regulators for this center regulator only
                foreach (NPCBuildingRegulator nbe in independentBuildingRegulators)
                    ActivateBuildingRegulator(nbe, buildingCenterRegulators[buildingCenterRegulators.Count - 1]);

                //if this is the first border component that has been activated => capital building:
                if (buildingCenterRegulators.Count == 1)
                {
                    npcMgr.territoryManager_NPC.ActivateCenterRegulator(); //activate the center regulator in the territory manager.
                    npcMgr.populationManager_NPC.ActivatePopulationBuilding(); //activate the population building
                }
            }
        }

        //called whenver a building is destroyed:
        void OnBuildingDestroyed (Building building)
        {
            //if the building belongs to this faction & has a Border component:
            if (building.FactionID == factionMgr.FactionID && building.BorderMgr == building.CurrentCenter)
            {
                //remove building center regulator from list since it has been destroyed:
                int i = 0;
                while(i < buildingCenterRegulators.Count)
                {
                    //if this is the center we're looking for:
                    if (buildingCenterRegulators[i].buildingCenter == building)
                    {
                        //go through all active building regulators:
                        foreach(BuildingCenterRegulator.ActiveBuildingRegulator nbr in buildingCenterRegulators[i].activeBuildingRegulators)
                        {
                            Destroy(nbr.instance); //remove the active instance
                        }
                        //remove it
                        buildingCenterRegulators.RemoveAt(i);
                        //done:
                        return;
                    }
                    else
                        i++; //continue looking

                }
            }
        }

        //get the capital regulator:
        public BuildingCenterRegulator GetCapitalRegualtor()
        {
            //first building center regulator in the list refers to the capital:
            return buildingCenterRegulators[0];
        }

        void Start()
        {
            //get the resource manager component:
            resourceMgr = GameManager.Instance.ResourceMgr;
        }

        void Update()
        {
            //if the building creator is active:
            if (isActive == true)
            {
                isActive = false; //assume that the building creator has finished its job with the current active building regulators.

                //go through all the building center regulators:
                foreach (BuildingCenterRegulator bcr in buildingCenterRegulators)
                {
                    //go through the active unit regulators:
                    foreach (BuildingCenterRegulator.ActiveBuildingRegulator abr in bcr.activeBuildingRegulators)
                    {
                        //if the building didn't reach its max amount yet and still didn't reach its min amount.
                        //buildings are only automatically created if they haven't reached their min amount
                        if (abr.instance.HasReachedMinAmount() == false && abr.instance.HasReachedMaxAmount() == false)
                        {
                            //we are active since the min amount of one of the buildings regulated hasn't been reached
                            isActive = true;

                            //spawn timer:
                            if (abr.spawnTimer > 0.0f)
                                abr.spawnTimer -= Time.deltaTime;
                            else
                            {
                                //reload timer:
                                abr.spawnTimer = abr.instance.spawnReloadRange.getRandomValue();
                                //attempt to create as much as it is possible from this building:
                                OnCreateBuildingRequest(abr.instance, true, bcr.buildingCenter);
                            }
                        }
                    }
                }
            }
        }

        //supply a building regulator prefab and get a valid active instance:
        NPCBuildingRegulator GetActiveInstance (NPCBuildingRegulator source)
        {
            //search for the first suitable instance that has the same building source regulator:
            //go through all the building center regulators:
            foreach (BuildingCenterRegulator bcr in buildingCenterRegulators)
            {
                //go through the active unit regulators:
                foreach (BuildingCenterRegulator.ActiveBuildingRegulator abr in bcr.activeBuildingRegulators)
                {
                    //if this active building regulator hasn't reached its max amount and it has the same source/prefab:
                    if (!abr.instance.HasReachedMaxAmount() && abr.source == source)
                    {
                        //set the instance:
                        return abr.instance;
                    }
                }
            }

            return null;
        }

        //before a building is placed, a request must be done to create it:
        public void OnCreateBuildingRequest(NPCBuildingRegulator instance, bool auto, Building buildingCenter, bool sourceRegulator = false)
        {
            //if the building regulator that has been used is the parameter is the source/prefab not the actual instance:
            if (sourceRegulator == true)
            {
                instance = GetActiveInstance(instance);
            }

            //if an active instance isn't provided: 
            if (instance == null)
                return; //do not proceed

            //if this has been requested from another NPC component and the regulator doesn't allow it
            if (auto == false && instance.createOnDemand == false)
            {
                return; //do not proceed.
            }

            if (!instance.HasReachedMaxAmount()) //as long as we haven't reached the max amount
            {
                //pick a building prefab:
                Building buildingPrefab = instance.prefabs[Random.Range(0, instance.prefabs.Count)];

                //check if faction has enough resources to place the chosen prefab above.
                if (resourceMgr.CheckResources(buildingPrefab.BuildingResources, factionMgr.FactionID) == true)
                {
                    //if the building center hasn't been chosen:
                    if(buildingCenter == null)
                    {
                        //need to look for one:
                        //this method will look for a center that will get a center that can have this building placed.
                        //if none is found then stop here.
                        if((buildingCenter = GetFreeBuildingCenter(buildingPrefab)) == null)
                        {
                            //TO BE MODIFIED -> no building center is found -> request to place a building center?
                            return;
                        }
                    }

                    GameObject buildAround = null; //this is the object that the building will be built around

                    //go through all the building placement option cases:
                    switch(instance.placementOption)
                    {
                        case NPCBuildingRegulator.placementOptions.aroundResource:
                            //building will be placed around a resource:
                            //get the list of the resources in the building center where the building will be placed with the requested resource name
                            List<Resource> availableResourceList = ResourceManager.FilterResourceList(buildingCenter.BorderMgr.ResourcesInRange, instance.placementOptionInfo);
                            if(availableResourceList.Count > 0) //if there are resources found:
                            {
                                //pick one randomly:
                                buildAround = availableResourceList[Random.Range(0, availableResourceList.Count)].gameObject;
                            }
                            break;
                        case NPCBuildingRegulator.placementOptions.aroundBuilding:
                            //building will be placed around another building
                            //get the list of the buildings that match the requested code around the building center
                            List<Building> buildingList = buildingCenter.BorderMgr.BuildingsInRange;
                            buildingList.Add(buildingCenter);
                            List<Building> availableBuildingList = BuildingManager.FilterBuildingList(buildingList, instance.placementOptionInfo);

                            if (availableBuildingList.Count > 0) //if there are buildings found:
                            {
                                //pick one randomly:
                                buildAround = availableBuildingList[Random.Range(0, availableBuildingList.Count)].gameObject;
                            } 
                            break;
                        default:
                            //no option?
                            buildAround = buildingCenter.gameObject; //build around building center.
                            break;
                    }

                    npcMgr.buildingPlacer_NPC.OnBuildingPlacementRequest(buildingPrefab, buildAround, buildingCenter, instance.buildAroundDistance);
                }
                else
                {
                    //TO BE MODIFIED -> NO RESOURCES FOUND -> ASK FOR SOME.
                }
            }
        }

        //a method that looks for a building center that allows a building to be placed:
        public Building GetFreeBuildingCenter (Building buildingToPlace)
        {
            //go through the building centers of the faciton
            foreach(Building center in factionMgr.BuildingCenters)
            {
                //see if the building center can have the input building placed around it:
                if(center.BorderMgr.AllowBuildingInBorder(buildingToPlace.Code))
                {
                    //if yes then return this center:
                    return center;
                }
            }

            //no center found? 
            return null;
        }
    }
}
