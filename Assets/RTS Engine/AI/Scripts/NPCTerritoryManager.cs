using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* NPC Territory Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class NPCTerritoryManager : NPCComponent
    {
        //the building regulator for the building center which will be used to expand the territory:
        public NPCBuildingRegulator centerRegulator;
        private NPCBuildingRegulator centerRegulatorIns;

        public bool expandOnDemand = true; //Can other NPC components request to expand the faction's territory?

        //min & max map territory ratio to control:
        //the territory manager will actively attempt at least control the min ratio & will not exceed the max ratio specified below:
        public FloatRange territoryRatio = new FloatRange(0.1f, 0.5f);
        private float currentTerritoryRatio = 0;

        //time reload at which this component decides whether to expand or not
        public FloatRange expandReloadRange = new FloatRange(2.0f, 7.0f);
        private float expandTimer;

        //is the component active?
        private bool isActive = true;

        TerrainManager terrainMgr;

        void Awake()
        {
            currentTerritoryRatio = 0.0f; //initially set to 0.0f
            //start the expand timer:
            expandTimer = expandReloadRange.getRandomValue();
            //set as active by default:
            isActive = true;

            //listen to delegate events:
            CustomEvents.BorderActivated += OnBorderActivated;
            CustomEvents.BuildingDestroyed += OnBuildingDestroyed;
        }

        public override void InitManagers (NPCManager npcMgr, FactionManager factionMgr)
        {
            base.InitManagers(npcMgr, factionMgr);
            terrainMgr = TerrainManager.Instance;
        }

        public void ActivateCenterRegulator ()
        {
            //activate the center regulator:
            if (centerRegulator != null)
            {
                centerRegulatorIns = npcMgr.buildingCreator_NPC.ActivateBuildingRegulator(centerRegulator, npcMgr.buildingCreator_NPC.GetCapitalRegualtor());
            }
            else
                Debug.LogError("The building center regulator hasn't been assigned in the NPC Territory Manager component!");
        }

        void OnDisable()
        {
            //listen to delegate events:
            CustomEvents.BorderActivated -= OnBorderActivated;
            CustomEvents.BuildingDestroyed -= OnBuildingDestroyed;
        }

        //called whenever a new border component is activated:
        void OnBorderActivated (Border border)
        {
            //if this border belongs to this faction
            if(border.MainBuilding.FactionID == factionMgr.FactionID)
            {
                //increase current territory
                UpdateCurrentTerritory(Mathf.PI * Mathf.Pow(border.Size, 2));
            }
        }

        //called whenever a building is destroyed:
        void OnBuildingDestroyed (Building building)
        {
            //if the building belongs to this faction and it has border component:
            if(building.FactionID == factionMgr.FactionID && building.BorderMgr != null)
            {
                //decrease the current territory ratio.
                UpdateCurrentTerritory(-Mathf.PI * Mathf.Pow(building.BorderMgr.Size, 2));
            }
        }

        //increase/decrease the current faction's territory.
        void UpdateCurrentTerritory (float value)
        {
            currentTerritoryRatio += (value/terrainMgr.mapSize); //update the value.
            if (value > 0) //increase?
            {
                //see if the minimum ratio has been reached:
                if (HasReachedMinTerritory() == true)
                    isActive = false; //deactivate the territory manager
            }
            else //decrease:
            {
                //see if the minimum ratio is now not available:
                if (HasReachedMinTerritory() == false)
                    isActive = true; //activate the territory manager
            }
        }

        //did the faction reach the minimum required territory ratio?
        bool HasReachedMinTerritory ()
        {
            return currentTerritoryRatio >= territoryRatio.min;
        }

        //did the faction reach the maximum allowed territory ratio?
        bool HasReachedMaxTerritory ()
        {
            return currentTerritoryRatio >= territoryRatio.max;
        }

        void Update()
        {
            //if the component is active:
            if(isActive == true)
            {
                //expansion timer:
                if (expandTimer > 0)
                    expandTimer -= Time.deltaTime;
                else
                {
                    //reload timer:
                    expandTimer = expandReloadRange.getRandomValue();

                    
                }
            }
        }

        public void OnExpandRequest (bool auto)
        {
            //if this has been requested by another NPC component yet it's not allowed:
            if (auto == false && expandOnDemand == false)
                return; //do not proceed.

            //request building creator to create new instance:
            npcMgr.buildingCreator_NPC.OnCreateBuildingRequest(centerRegulatorIns, false, npcMgr.buildingCreator_NPC.GetCapitalRegualtor().buildingCenter);
        }
    }
}
