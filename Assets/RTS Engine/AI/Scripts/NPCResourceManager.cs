using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* NPC Resource Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class NPCResourceManager : NPCComponent
    {
        //Must always be > 1.0. This determines how safe the NPC team would like to use their resources.
        //For example, if the "ResourceNeedRatio" is set to 2.0 and the faction needs 200 woods. Only when 400 (200 x 2) wood is available, the 200 can be used.
        public FloatRange resourceNeedRatioRange = new FloatRange(1.0f, 1.2f);

        //determine how many resources will be exploited by default by the faction.
        //0.0f -> no resources, 1.0f -> all available resources.
        public FloatRange resourceExploitRatioRange = new FloatRange(0.8f, 1.0f);

        public class FactionResources
        {
            public Building buildingCenter = null; //the building center that has the below resources in its territory.

            //a list that holds resources that aren't being collected by this faction.
            public List<Resource> idleResources = new List<Resource>();
            //a list that holds resources that are currently being exploited inside the territory of the above center:
            public List<Resource> exploitedResources = new List<Resource>();
        }
        private List<FactionResources> factionResources = new List<FactionResources>(); //resources that belong to this faction...
        //...and which are not being collected will remain in this list with the building centers they belong to.

        ResourceManager resourceMgr;

        private void Start()
        {
            //get the Resource Manager component
            resourceMgr = GameManager.Instance.ResourceMgr;

            //set the resource need ratio for this NPC faction.
            resourceMgr.FactionResourcesInfo[factionMgr.FactionID].SetResourceNeedRatio(resourceNeedRatioRange.getRandomValue());
        }

        void Awake()
        {
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
            //if the building center that activated the border belongs to this faction
            if (border.MainBuilding.FactionID == factionMgr.FactionID)
            {
                //when border is activated then we'd send some of them to the resources to collect list of the NPC Resource Collector...
                //...and leave the rest idle and that is all depending on the resource exploit ratio defined in this component:

                //create a new slot in the faction resources for the new border:
                factionResources.Add(new FactionResources { buildingCenter = border.MainBuilding, idleResources = new List<Resource>() });
                FactionResources newElem = factionResources[factionResources.Count - 1];

                //go through the border resources:
                foreach (Resource r in border.ResourcesInRange)
                {
                    //randomly decided if this resource is to be exploited:
                    if (Random.Range(0.0f, 1.0f) <= resourceExploitRatioRange.getRandomValue())
                    {
                        //if yes, then add it:
                        newElem.exploitedResources.Add(r);
                        npcMgr.resourceCollector_NPC.AddResource(r);
                    }
                    else
                    {
                        //if not add resource to idle list:
                        newElem.idleResources.Add(r);
                    }
                }
            }
        }

        //called whenver a building is destroyed:
        void OnBuildingDestroyed(Building building)
        {
            //if the building belongs to this faction & has a Border component:
            if (building.FactionID == factionMgr.FactionID && building.BorderMgr == building.CurrentCenter)
            {
                FactionResources elemToRemove = null;

                //go through building centers and idle/exploited resources under their control:
                //go through the border resources:
                foreach (FactionResources fr in factionResources)
                {
                    //if this is the building center that has been destroyed:
                    if (fr.buildingCenter == building)
                    {
                        //this element will be removed from the faction resources list later
                        elemToRemove = fr;

                        //go through exploited resources:
                        foreach (Resource r in fr.exploitedResources)
                        {
                            //remove them from the resources to collect list in the NPC Resource Collector
                            npcMgr.resourceCollector_NPC.RemoveResource(r);

                            //see if resource has collectors:
                            for (int i = 0; i < r.WorkerMgr.WorkerPositions.Length; i++)
                            {
                                //if this is a valid collector:
                                if (r.WorkerMgr.WorkerPositions[0].CurrentUnit != null)
                                {
                                    //cancel collection & stop mvt:
                                    r.WorkerMgr.WorkerPositions[0].CurrentUnit.StopMvt();
                                    r.WorkerMgr.WorkerPositions[0].CurrentUnit.CancelCollecting();
                                }
                            }
                        }
                    }
                }

                if (elemToRemove != null) //if the building center has been identified
                    factionResources.Remove(elemToRemove); //remove it.
            }
        }

        //a method called when a resource is empty and another needs to be exploited:
        public void OnExploitedResourceEmpty (Resource resource)
        {
            //search for it in the building centers & resources list:
            foreach (FactionResources fr in factionResources)
            {
                if (fr.exploitedResources.Contains(resource)) //if this is where the empty list is at:
                {
                    int i = 0;
                    //go through idle resources to look for an idle resource...
                    while (i < fr.idleResources.Count)
                    {
                        //...that has the same name as in the paramter:
                        if (fr.idleResources[i].Name == resource.Name)
                        {
                            //add it to the resources to collect lists:
                            npcMgr.resourceCollector_NPC.AddResource(fr.idleResources[i]);
                            fr.exploitedResources.Add(fr.idleResources[i]);
                            //remove it from the idle resources list:
                            fr.idleResources.RemoveAt(i);

                            //stop here:
                            break;
                        }

                        i++;
                    }

                    break;
                }
            }
            }

        //USE BORDER ACTIVE EVENT IN BUILDING CREATOR COMPONENT
    }
}
