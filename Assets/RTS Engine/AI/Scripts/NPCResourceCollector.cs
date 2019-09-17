using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* NPC Resource Collector script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class NPCResourceCollector : NPCComponent
    {
        //the unit regulator component that regulates the creation of the resource collector unit(s).
        public NPCUnitRegulator collectorRegulator;
        private NPCUnitRegulator collectorRegulatorIns;

        //resources that will be collected along settings to regulate their collection.
        [System.Serializable]
        public class CollectionInfo
        {
            public ResourceTypeInfo resourceTypeInfo; //the resource type that will be affected.

            //the collectors ratio determines how many collectors will be assigned to collect the above resource type.
            public FloatRange instanceCollectorsRatio = new FloatRange(0.2f, 0.5f); //per resource instance!!

            public FloatRange maxCollectorsRatioRange = new FloatRange(0.3f, 0.4f); //the maximum amount of collectors that can collect this resource at the same time.
            [HideInInspector]
            public int collectorsAmount = 0; //keeps track the current amount of collectors.
        }
        public List<CollectionInfo> collectionInfoList = new List<CollectionInfo>();

        public class ResourceToCollectInfo
        {
            public Resource resource; //resource instance to collect.
            public CollectionInfo collectionInfo; //the corresponding collection info.
        }
        //holds a list of resources to collect
        private List<ResourceToCollectInfo> resourcesToCollect = new List<ResourceToCollectInfo>();

        //how often will this component check resources to collect?
        public FloatRange collectionTimerRange = new FloatRange(3.0f, 5.0f);
        private float collectionTimer;

        public bool collectOnDemand = true; //when another component requests to collect a resource, allow it or not?

        private bool isActive = true; //is this component active?

        void Start()
        {
            //activate the collector unit regulator:
            collectorRegulatorIns = npcMgr.unitCreator_NPC.ActivateUnitRegulator(collectorRegulator);

            //start collection timer:
            collectionTimer = collectionTimerRange.getRandomValue();

            //add event listeners:
            CustomEvents.UnitStopCollecting += OnUnitStopCollecting;
            CustomEvents.UnitStartCollecting += OnUnitStartCollecting;
            CustomEvents.ResourceEmpty += OnResourceEmpty;
            CustomEvents.UnitCreated += OnUnitCreated;
        }

        void OnDisable()
        {
            //remove event listeners:
            CustomEvents.UnitStopCollecting -= OnUnitStopCollecting;
            CustomEvents.UnitStartCollecting -= OnUnitStartCollecting;
            CustomEvents.ResourceEmpty -= OnResourceEmpty;
            CustomEvents.UnitCreated -= OnUnitCreated;
        }

        //called each time a unit is created
        void OnUnitCreated (Unit unit)
        {
            //if this unit belongs to this faction & it has a gather resource comp:
            if (unit.FactionID == factionMgr.FactionID && unit.ResourceMgr)
            {
                isActive = true; //activate component.
            }
        }

        //called each time a unit stops collecting a resource
        private void OnUnitStopCollecting (Unit unit, Resource resource)
        {
            ResourceToCollectInfo instance;

            //check if the resource is controlled by this component and that the unit belongs to the faction:
            if (unit.FactionID == factionMgr.FactionID && (instance = GetResourceToCollect(resource)) != null)
            {
                //activate the component so that it checks for resource collectors to this resource
                isActive = true;
                instance.collectionInfo.collectorsAmount--;
            }
        }

        //called each time a unit starts collecting a resource
        private void OnUnitStartCollecting(Unit unit, Resource resource)
        {
            ResourceToCollectInfo instance;

            //check if the resource is controlled by this component and that the unit belongs to the faction:
            if (unit.FactionID == factionMgr.FactionID && (instance = GetResourceToCollect(resource)) != null)
            {
                instance.collectionInfo.collectorsAmount++;
            }
        }

        //called whenever a resource is empty:
        private void OnResourceEmpty(Resource resource)
        {
            //Remove it.
            RemoveResource(resource);
        }

        //a method that gets the collection info element using the resource name
        public CollectionInfo GetCollectionInfo(string resourceName)
        {
            //go through all collection infos
            foreach (CollectionInfo ci in collectionInfoList)
            {
                if (ci.resourceTypeInfo.Name == resourceName) //if the collection info matches the input name.
                    return ci;
            }
            return null; //no collection info that matches the input name is found
        }

        //a method that returns whether a resource type can be further collected or not:
        public bool CanCollectResourceType(CollectionInfo ci)
        {
            //get the collection info:
            if (ci != null)
            {
                return collectorRegulatorIns.GetCurrentInstances().Count * ci.maxCollectorsRatioRange.getRandomValue() > ci.collectorsAmount;
            }

            return false;
        }

        //a method to check whether a resource instance is part of the resources to collect list:
        public bool InResourcesToCollect(Resource resource)
        {
            foreach(ResourceToCollectInfo rtc in resourcesToCollect)
            {
                if (rtc.resource == resource)
                    return true;
            }

            return false;
        }

        //a method to check whether a resource instance is part of the resources to collect list:
        public ResourceToCollectInfo GetResourceToCollect(Resource resource)
        {
            foreach (ResourceToCollectInfo rtc in resourcesToCollect)
            {
                if (rtc.resource == resource)
                    return rtc;
            }

            return null;
        }

        //a method ot add/remove a resource instance to the resources to collect list:
        public void UpdateResourcesToCollect(Resource resource, bool add)
        {
            if(add == true) //adding
            {
                //create new instance of the struct:
                ResourceToCollectInfo newResourceToCollect = new ResourceToCollectInfo()
                {
                    resource = resource,
                    collectionInfo = GetCollectionInfo(resource.Name)
                };
                //only add this resource type if it is actually supported by this component
                if (newResourceToCollect.collectionInfo != null)
                {
                    //add it to the list:
                    resourcesToCollect.Add(newResourceToCollect);
                }
            }
            else //removing:
            {
                //go through the list:
                int i = 0;
                while(i < resourcesToCollect.Count)
                {
                    //if resource we want to remove is found
                    if (resourcesToCollect[i].resource == resource)
                    {
                        resourcesToCollect.RemoveAt(i);
                        return;
                    }
                    i++;
                }
            }
        }

        //method used to add a resource to the resources to collect
        public void AddResource (Resource resource)
        {
            //if the resource isn't already in the list:
            if(InResourcesToCollect(resource) == false)
            {
                UpdateResourcesToCollect(resource, true); //add it.
                isActive = true; //activate component.
            }
        }

        //method used to remove a resource from the resources to collect list:
        public void RemoveResource (Resource resource)
        {
            //if the resource belonged to the resources to collect list:
            if (InResourcesToCollect(resource))
            {
                //remove it:
                UpdateResourcesToCollect(resource, false);
                //request to find a resource from the same type to collect:
                npcMgr.resourceManager_NPC.OnExploitedResourceEmpty(resource);
            }
        }

        //method used to remove a resource from the resources to collect.

        void Update ()
        {
            //is this component active?
            if(isActive == true)
            {
                //resource collection timer:
                if (collectionTimer > 0)
                    collectionTimer -= Time.deltaTime;
                else
                {
                    //reload timer
                    collectionTimer = collectionTimerRange.getRandomValue();

                    //isActive = false;

                    //go through the resources from which we need to collect
                    foreach (ResourceToCollectInfo rtc in resourcesToCollect)
                    {
                        //can the faction still collect from this resource type?
                        if (CanCollectResourceType(rtc.collectionInfo) == true)
                        {
                            int targetCollectorsAmount = GetTargetCollectorsAmount(rtc);
                            //does the resource still need collectors?
                            if (targetCollectorsAmount > rtc.resource.WorkerMgr.CurrentWorkers)
                            {
                                //send resource collectors for this one.
                                OnResourceCollectionRequest(rtc.resource, targetCollectorsAmount, true, false);
                                //set state to active:
                                isActive = true;
                            }
                        }
                    }
                }
            }
        }

        //get the targe collectors amount:
        public int GetTargetCollectorsAmount (ResourceToCollectInfo rtc)
        {
            //if there's one:
            if (rtc.collectionInfo != null)
            {
                //calculate how much collectors are required:
                int targetCollectors = (int)(rtc.resource.WorkerMgr.WorkerPositions.Length * rtc.collectionInfo.instanceCollectorsRatio.getRandomValue());
                if (targetCollectors <= 0) //can't be lower than one
                    targetCollectors = 1;
                return targetCollectors;
            }

            return 0; //resource is not regulated by this component, return 0 to send no collectors.
        }

        //Method used by other components to request to collect a certain resource:
        public void OnResourceCollectionRequest (Resource resource, int targetCollectorsAmount, bool auto, bool force)
        {
            if (auto == false && collectOnDemand == false) //if this was requested by another component and we don't allow collection on demand
                return;

            //making sure the resource is valid:
            if (resource != null)
            {
                //making sure the resource isn't empty:
                if (resource.Amount > 0 || resource.Infinite == true)
                {
                    //how much collectors is required?
                    int requiredCollectors = targetCollectorsAmount - resource.WorkerMgr.CurrentWorkers;

                    int i = 0; //counter.
                    List<Unit> currentCollectors = collectorRegulatorIns.GetIdleUnitsFirst(); //get the list of the current faction collectors.

                    //while we still need collectors for the building and we haven't gone through all collectors.
                    while (i < currentCollectors.Count && requiredCollectors > 0)
                    {
                        //making sure the collector is valid:
                        if (currentCollectors[i] != null)
                        {
                            //is the collector currently in idle mode or do we force him to construct this building?
                            //& make sure it's not already collecting this resource.
                            if ((currentCollectors[i].IsIdle() || force == true) && currentCollectors[i].ResourceMgr.TargetResource != resource)
                            {
                                //send to collect the resource:
                                currentCollectors[i].ResourceMgr.SetTargetResource(resource);
                                //decrement amount of required builders:
                                requiredCollectors--;
                            }
                        }

                        i++;
                    }
                }
            }
        }
    }
}
