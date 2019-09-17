using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* NPC Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class NPCManager : MonoBehaviour
    {
        public string Name = "New NPC Manager";
        public string code = "new_npc_manager";

        public string prefabPath = "Assets/RTS Engine/AI/NPC Managers";

        //NPC Components:
        public NPCBuildingCreator buildingCreator_NPC { get; private set; }
        public NPCBuildingPlacer buildingPlacer_NPC { get; private set; }
        public NPCBuildingConstructor buildingConstructor_NPC { get; private set; }

        public NPCUnitCreator unitCreator_NPC { get; private set; }

        public NPCResourceManager resourceManager_NPC { get; private set; }
        public NPCResourceCollector resourceCollector_NPC { get; private set; }
        public NPCTerritoryManager territoryManager_NPC { get; private set; }

        public NPCPopulationManager populationManager_NPC { get; private set; }

        public NPCTaskManager taskManager_NPC { get; private set; }

        public NPCAttackManager attackManager_NPC { get; private set; }
        public NPCArmyCreator armyCreator_NPC { get; private set; }
        public NPCDefenseManager defenseManager_NPC { get; private set; }

        //the faction manager that this NPC manager is managing.
        public FactionManager FactionMgr;

        [HideInInspector]
        public List<NPCUnitRegulator> unitRegulatorAssets = new List<NPCUnitRegulator>();
        [HideInInspector]
        public List<NPCBuildingRegulator> buildingRegulatorAssets = new List<NPCBuildingRegulator>();



        //temporary ->
        public void Init()
        {
            //go through the children of this game objec and get the references for the required components:
            foreach(Transform child in this.transform)
            {
                //building related:
                if(child.GetComponent<NPCBuildingCreator>())
                {
                    buildingCreator_NPC = child.GetComponent<NPCBuildingCreator>();
                    buildingCreator_NPC.InitManagers(this, FactionMgr);
                }
                if (child.GetComponent<NPCBuildingPlacer>())
                {
                    buildingPlacer_NPC = child.GetComponent<NPCBuildingPlacer>();
                    buildingPlacer_NPC.InitManagers(this, FactionMgr);
                }
                if (child.GetComponent<NPCBuildingConstructor>())
                {
                    buildingConstructor_NPC = child.GetComponent<NPCBuildingConstructor>();
                    buildingConstructor_NPC.InitManagers(this, FactionMgr);
                }

                //unit related:
                if (child.GetComponent<NPCUnitCreator>())
                {
                    unitCreator_NPC = child.GetComponent<NPCUnitCreator>();
                    unitCreator_NPC.InitManagers(this, FactionMgr);
                }

                //resource related:
                if (child.GetComponent<NPCResourceManager>())
                {
                    resourceManager_NPC = child.GetComponent<NPCResourceManager>();
                    resourceManager_NPC.InitManagers(this, FactionMgr);
                }
                if (child.GetComponent<NPCResourceCollector>())
                {
                    resourceCollector_NPC = child.GetComponent<NPCResourceCollector>();
                    resourceCollector_NPC.InitManagers(this, FactionMgr);
                }
                if (child.GetComponent<NPCTerritoryManager>())
                {
                    territoryManager_NPC = child.GetComponent<NPCTerritoryManager>();
                    territoryManager_NPC.InitManagers(this, FactionMgr);
                }

                //army related:
                if (child.GetComponent<NPCAttackManager>())
                {
                    attackManager_NPC = child.GetComponent<NPCAttackManager>();
                    attackManager_NPC.InitManagers(this, FactionMgr);
                }
                if (child.GetComponent<NPCArmyCreator>())
                {
                    armyCreator_NPC = child.GetComponent<NPCArmyCreator>();
                    armyCreator_NPC.InitManagers(this, FactionMgr);
                }
                if (child.GetComponent<NPCDefenseManager>())
                {
                    defenseManager_NPC = child.GetComponent<NPCDefenseManager>();
                    defenseManager_NPC.InitManagers(this, FactionMgr);
                }

                //other:
                if (child.GetComponent<NPCPopulationManager>())
                {
                    populationManager_NPC = child.GetComponent<NPCPopulationManager>();
                    populationManager_NPC.InitManagers(this, FactionMgr);
                }
                if (child.GetComponent<NPCTaskManager>())
                {
                    taskManager_NPC = child.GetComponent<NPCTaskManager>();
                    taskManager_NPC.InitManagers(this, FactionMgr);
                }
            }
        }

        //a method that gets a unit regulator from prefab:
        public NPCUnitRegulator GetUnitRegulatorAsset(Unit prefab)
        {
            foreach (NPCUnitRegulator nur in unitRegulatorAssets)
                if (nur.prefabs.Contains(prefab))
                    return nur;

            return null;
        }

        //a method that gets a unit regulator from prefab:
        public NPCBuildingRegulator GetBuildingRegulatorAsset(Building prefab)
        {
            foreach (NPCBuildingRegulator nbr in buildingRegulatorAssets)
                if (nbr.prefabs.Contains(prefab))
                    return nbr;

            return null;
        }

        //Destroy the active instances of the regulators.
        void OnDestroy()
        {
            //destroy the active unit regulators
            unitCreator_NPC.DestroyActiveRegulators();
            //destroy the active building regulators:
            buildingCreator_NPC.DestroyActiveRegulators();
        }
    }
}
