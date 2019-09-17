using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    //[CreateAssetMenu(fileName = "NewBuildingRegulator", menuName = "RTS Engine/Building Regulator", order = 54)]
    public class NPCBuildingRegulator : NPCRegulator<Building>
    {
        private Building buildingCenter; //the building center where the instance of the regulator is active.

        //settings for how the building should be placed:
        public enum placementOptions { aroundCenter, aroundBuilding, aroundResource};
        //either randomly, around a specific building or a around a specific resource.
        public placementOptions placementOption = placementOptions.aroundCenter;
        public string placementOptionInfo; //only valid when the placement option is set to aroundBuilding (provide building code) or aroundResource (provide resource name).
        //distance of the building from its closest building center:
        public float buildAroundDistance = 1.0f;

        //building creator:
        private NPCBuildingCreator buildingCreator_NPC; //the NPC Building Creator used to create all instances of the buildings regulated here.

        /* Initializing everything */
        public void Init(NPCManager npcMgr, NPCBuildingCreator buildingCreator_NPC, Building buildingCenter)
        {
            //assign the appropriate faction manager and building creator settings
            base.InitItem(npcMgr);
            this.buildingCreator_NPC = buildingCreator_NPC;
            this.buildingCenter = buildingCenter;

            //set the building code:
            code = prefabs[0].Code;

            //go through all spawned buildings to see if the buildings that should be regulated by this instance are created or not:
            foreach (Building b in this.factionMgr.Buildings)
            {
                if(b.Code == code)
                {
                    amount++; //increase amount
                    //add to list:
                    currentInstances.Add(b);
                }
            }

            //start listening to the required delegate events:
            CustomEvents.BuildingDestroyed += OnBuildingDestroyed;
            CustomEvents.BuildingStopPlacement += OnBuildingStopPlacement;
            CustomEvents.BuildingStartPlacement += OnBuildingStartPlacement;
            CustomEvents.BuildingPlaced += OnBuildingPlaced;
        }

        void OnDisable()
        {
            //stop listening to the delegate events:
            CustomEvents.BuildingDestroyed -= OnBuildingDestroyed;
            CustomEvents.BuildingStopPlacement -= OnBuildingStopPlacement;
            CustomEvents.BuildingStartPlacement -= OnBuildingStartPlacement;
            CustomEvents.BuildingPlaced -= OnBuildingPlaced;
        }

        //regulating the building:


        //removes a building:
        public override void RemoveItem(Building building)
        {
            base.RemoveItem(building);
            //if the target amount is now not reached anymore:
            if (!HasReachedMinAmount())
                buildingCreator_NPC.Activate(); //activate the building creator.
        }

        //check if a building is regulated by this component
        bool IsValidBuilding (Building building)
        {
            return (building.FactionID == factionMgr.FactionID && building.Code == code || currentInstances.Contains(building)) && buildingCenter == building.CurrentCenter.MainBuilding;
        }

        //called when a faction starts placing a building.
        void OnBuildingStartPlacement (Building building)
        {
            if (IsValidBuilding(building)) //is it regulated by this comp?
            { 
                amount++; //increase amount.
                //increase pending amount:
                pendingAmount++;
            }
        }

        //called when a faction places a building
        void OnBuildingPlaced(Building building)
        {
            if (IsValidBuilding(building)) //is it regulated by this comp?
            {
                //add building to list:
                currentInstances.Add(building);
                pendingAmount--; //decrease pending amount.
            }
        }

        //called when a faction stops placing a building
        void OnBuildingStopPlacement(Building building)
        {
            if (IsValidBuilding(building)) //is it regulated by this comp?
            {
                RemoveItem(building);
            }
        }

        //called when a building is destroyed.
        void OnBuildingDestroyed(Building building)
        {
            if (IsValidBuilding(building)) //is it regulated by this comp?
            {
                RemoveItem(building);
            }
        }
    }
}
