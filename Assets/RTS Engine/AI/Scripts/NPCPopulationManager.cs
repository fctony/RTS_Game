using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* NPC Population Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class NPCPopulationManager : NPCComponent
    {
        public NPCBuildingRegulator populationBuilding = null; //main building regulator that the AI will use to increase its population slots.

        public FloatRange targetPopulationRange = new FloatRange(40, 50); //the population slots that the NPC faction aims to reach
        int targetPopulation;
        int maxPopulation; //holds the maximum population slots.

        //the following class handles population increase requests
        [System.Serializable]
        public class PopulationOnDemand
        {
            public bool enabled = true; //increase the population when another component requests more population slots?
            //if the above option is set to false, then the population manager would ignore all requests to increase population.

            public FloatRange acceptanceRange = new FloatRange(0.5f, 0.7f); //between 0.0 and 1.0, with 0.0 meaning always refuse and 1.0 meaning always accept.
            //if the population on demand is enabled, then a random value X (between 0.0 and 1.0) will be generated and compared to a random value Y between the above...
            //...values and the population increase request will be acceped if it is X <= Y.
        }
        public PopulationOnDemand populationOnDemand;

        public int minFreePopulationSlots = 3; //whenever the free population slots amount is <= this value then this would attempt to place a new population building.
        //when set to -1 (or below) there will be no minimum free population slots amount that trigger placing a population building.

        bool isActive = false; //is this component active?

        private void Start()
        {
            //pick a target population amount:
            targetPopulation = (int)Random.Range(targetPopulationRange.min, targetPopulationRange.max);
        }

        public void ActivatePopulationBuilding ()
        {
            //activate the population building regulator:
            npcMgr.buildingCreator_NPC.ActivateBuildingRegulator(populationBuilding);
            isActive = true; //activate the population manager
        }

        private void OnEnable()
        {
            //custom event listeners:
            CustomEvents.MaxPopulationUpdated += OnMaxPopulationUpdated;
        }

        private void OnDisable()
        {
            //custom event listeners:
            CustomEvents.MaxPopulationUpdated -= OnMaxPopulationUpdated;
        }

        //other NPC components request a population increase through 
        public bool OnAddPopulationRequest()
        {
            //if we reached the target max population
            //or if this NPC faction doesn't add population on demand:
            //or this component is simply not active
            if (isActive == false || populationOnDemand.enabled == false || maxPopulation >= targetPopulation)
                return false;

            //randomly decide (based on input values) if this would be accepetd or not:
            if (Random.value <= populationOnDemand.acceptanceRange.getRandomValue())
            {
                //population increase request accepted:
                //attempt to add population building
                npcMgr.buildingCreator_NPC.OnCreateBuildingRequest(populationBuilding, false, null, true);
                return true;
            }
            else
                return false;
        }

        public void OnMaxPopulationUpdated(GameManager.FactionInfo factionInfo, int value)
        {
            //if this update belongs to the faction managed by this component:
            if (factionInfo.FactionMgr.FactionID == factionMgr.FactionID)
            {
                maxPopulation = factionInfo.GetMaxPopulation();
                CheckFreePopulationSlots(factionInfo.GetFreePopulation());
            }
        }

        //checks the free population slots and whether we need to add a new population or not
        void CheckFreePopulationSlots(int slotsLeft)
        {
            //if we reached the target max population or the component isn't active:
            if (isActive == false || maxPopulation >= targetPopulation)
                return; //do not proceed

            //if the amount of slots left is <= than the minimum allowed free slots
            if (slotsLeft <= minFreePopulationSlots)
            {
                //attempt to add population building
                npcMgr.buildingCreator_NPC.OnCreateBuildingRequest(populationBuilding, false, null, true);
            }
        }

    }
}
