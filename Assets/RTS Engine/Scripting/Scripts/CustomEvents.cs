using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
	public class CustomEvents : MonoBehaviour {

		public bool DebugEnabled = false;

		public delegate void UnitEventHandler(Unit Unit);
		public static event UnitEventHandler UnitCreated = delegate {};
		public static event UnitEventHandler UnitDead = delegate {};
		public static event UnitEventHandler UnitSelected = delegate {};
		public static event UnitEventHandler UnitDeselcted = delegate {};
        public static event UnitEventHandler UnitMoveAttempt = delegate {};

        public delegate void UnitHealthEventHandler(Unit Unit, float HealthPoints, GameObject Source);
        public static event UnitHealthEventHandler UnitHealthUpdated = delegate { };

        public delegate void UnitResourceEventHandler(Unit UnitComp, Resource Resource);
		public static event UnitResourceEventHandler UnitStartCollecting = delegate {};
		public static event UnitResourceEventHandler UnitStopCollecting = delegate {};

		public delegate void UnitBuildingEventHandler (Unit UnitComp, Building Building);
		public static event UnitBuildingEventHandler UnitStartBuilding = delegate {};
		public static event UnitBuildingEventHandler UnitStopBuilding = delegate {};

		public delegate void UnitHealingEventHandler (Unit UnitComp, Unit TargetUnit);
		public static event UnitHealingEventHandler UnitStartHealing = delegate {};
		public static event UnitHealingEventHandler UnitStopHealing = delegate {};

		public delegate void UnitConvertingEventHandler (Unit UnitComp, Unit TargetUnit);
		public static event UnitConvertingEventHandler UnitStartConverting = delegate {};
		public static event UnitConvertingEventHandler UnitStopConverting = delegate {};
		public static event UnitConvertingEventHandler UnitConverted = delegate {};

		public delegate void UnitSwitchingAttackEventHandler (Unit Unit, Attack From, Attack To);
		public static event UnitSwitchingAttackEventHandler UnitSwitchAttack = delegate {};

        public delegate void UnitAttackEventHandler(Attack Source, GameObject Target);
        public static event UnitAttackEventHandler AttackTargetLocked = delegate { };
        public static event UnitAttackEventHandler AttackPerformed = delegate { };
        public static event UnitAttackEventHandler AttackerInRange = delegate { };

        public delegate void BuildingEventHandler (Building Building);
		public static event BuildingEventHandler BuildingPlaced = delegate {};
		public static event BuildingEventHandler BuildingBuilt = delegate {};
		public static event BuildingEventHandler BuildingDestroyed = delegate {};
		public static event BuildingEventHandler BuildingSelected = delegate {};
		public static event BuildingEventHandler BuildingDeselected = delegate {};
        public static event BuildingEventHandler BuildingStartPlacement = delegate {};
        public static event BuildingEventHandler BuildingStopPlacement = delegate {};

        public delegate void BuildingHealthEventHandler(Building Building, float Value, GameObject Source);
        public static event BuildingHealthEventHandler BuildingHealthUpdated = delegate {};

        public delegate void BuildingUpgradeEventHandler (Building Building, bool Direct);
		public static event BuildingUpgradeEventHandler BuildingStartUpgrade = delegate {};
		public static event BuildingUpgradeEventHandler BuildingCompleteUpgrade = delegate {};

        //Border component related:
        public delegate void BorderEventHandler(Border border);
        public static event BorderEventHandler BorderActivated = delegate {};

        //Task Launcher related:
		public delegate void TaskEventHandler (TaskLauncher TaskComp, int TaskID = -1, int TaskQueueID = -1);
        public static event TaskEventHandler TaskLauncherAdded = delegate {};
        public static event TaskEventHandler TaskLauncherRemoved = delegate {};
        public static event TaskEventHandler TaskLaunched = delegate {};
		public static event TaskEventHandler TaskCanceled = delegate {};
		public static event TaskEventHandler TaskCompleted = delegate {};

        //Population related:
        public delegate void PopulationEventHandler(GameManager.FactionInfo factionInfo, int value);
        public static event PopulationEventHandler CurrentPopulationUpdated = delegate { };
        public static event PopulationEventHandler MaxPopulationUpdated = delegate { };

        public delegate void ResourceEventHandler (Resource Resource);
		public static event ResourceEventHandler ResourceEmpty = delegate {};
		public static event ResourceEventHandler ResourceSelected = delegate {};
		public static event ResourceEventHandler ResourceDeselected = delegate {};

		public delegate void APCEventHandler (APC APC, Unit Unit);
		public static event APCEventHandler APCAddUnit = delegate {};
		public static event APCEventHandler APCRemoveUnit = delegate {}; 
		public static event APCEventHandler APCCallUnits = delegate {};

		public delegate void PortalEventHandler (Portal From, Portal To, Unit Unit);
		public static event PortalEventHandler UnitTeleport = delegate {};
		public static event PortalEventHandler PortalDoubleClick = delegate {}; 

		public delegate void GameEventHandler (GameManager.FactionInfo FactionInfo);
		public static event GameEventHandler FactionEliminated = delegate {};
		public static event GameEventHandler FactionWin = delegate {}; 

		public delegate void InvisibilityEventHandler (Unit Unit);
		public static event InvisibilityEventHandler UnitGoInvisible = delegate {};
		public static event InvisibilityEventHandler UnitGoVisible = delegate {};

        public delegate void SelectionObjEventHandler (SelectionObj Source, SelectionObj Target);
        public static event SelectionObjEventHandler SelectionObjEnter = delegate {};

		public delegate void CustomCommandEventHandler (GameObject Source, GameObject Target, Vector3 InitialPos, Vector3 TargetPos, int Value);
		public static event CustomCommandEventHandler CustomCommand = delegate {};

        public static CustomEvents instance = null;

        void Awake()
        {
            //one single Custom Events component:
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(this);
            }
        }

        //Unit custom events:
        public void OnUnitCreated (Unit Unit) //called when a unit is created.
		{
			if (DebugEnabled == true) {
				Debug.Log ("Unit '"+Unit.Name+"' (Faction ID "+Unit.FactionID+") created");
			}
			UnitCreated (Unit);
		}
		public void OnUnitDead (Unit Unit) //called when a unit is dead
		{
			if (DebugEnabled == true) {
				Debug.Log ("Unit '"+Unit.Name+"' (Faction ID "+Unit.FactionID+") dead");
			}
			UnitDead (Unit);
		}
		public void OnUnitSelected (Unit Unit) //called when a unit is selected
		{
			if (DebugEnabled == true) {
				Debug.Log ("Unit '"+Unit.Name+"' (Faction ID "+Unit.FactionID+") selected");
			}
			UnitSelected (Unit);
		}
		public void OnUnitDeselected (Unit Unit) //called when a unit is deselected
		{
			if (DebugEnabled == true) {
				Debug.Log ("Unit '"+Unit.Name+"' (Faction ID "+Unit.FactionID+") deselected");
			}
			UnitDeselcted (Unit);
		}
        public void OnUnitMoveAttempt(Unit Unit) //when the player attempts to move a unit
        {
            if (DebugEnabled == true)
            {
                Debug.Log("Player attempted to move Unit: '" + Unit.Name + "'");
            }
            UnitMoveAttempt(Unit);
        }

        //Unit-Health events:

        public void OnUnitHealthUpdated (Unit Unit, float HealthPoints, GameObject Source)
        {
            if (DebugEnabled == true)
            {
                Debug.Log("Unit '" + Unit.Name + "' (Faction ID " + Unit.FactionID + ") health has been updated by " + HealthPoints);
            }
            UnitHealthUpdated(Unit, HealthPoints, Source);
        }

        //Unit-Resource events:
        public void OnUnitStartCollecting (Unit Unit, Resource Resource) //called when a unit starts collecting a resource 
		{
			if (DebugEnabled == true) {
				Debug.Log ("Unit '"+Unit.Name+"' (Faction ID "+Unit.FactionID+") start collecting resource: '"+Resource.Name+"' (ID: "+Resource.ID+")");
			}
			UnitStartCollecting (Unit, Resource);
		}
		public void OnUnitStopCollecting (Unit Unit, Resource Resource) //called when a unit stops collecting a resource
		{
			if (DebugEnabled == true) {
				Debug.Log ("Unit '"+Unit.Name+"' (Faction ID "+Unit.FactionID+") stopped collecting resource: '"+Resource.Name+"' (ID: "+Resource.ID+")");
			}
			UnitStopCollecting (Unit, Resource);
		}

		//Unit-Building events:
		public void OnUnitStartBuilding (Unit Unit, Building Building) //called when a unit starts constructing a building
		{
			if (DebugEnabled == true) {
				Debug.Log ("Unit '"+Unit.Name+"' (Faction ID "+Unit.FactionID+") started constructing building: '"+Building.Name+"'");
			}
			UnitStartBuilding (Unit, Building);
		}
		public void OnUnitStopBuilding (Unit Unit, Building Building) //called when a unit stops constructing a building
		{
			if (DebugEnabled == true) {
				Debug.Log ("Unit '"+Unit.Name+"' (Faction ID "+Unit.FactionID+") stopped constructing building: '"+Building.Name+"'");
			}
			UnitStopBuilding (Unit, Building);
		}

		//Portal:
		public void OnUnitTeleport (Portal From, Portal To, Unit Unit) //called when a unit teleports in a portal
		{
			if (DebugEnabled == true) {
				Debug.Log ("Unit '"+Unit.Name+"' (Faction ID "+Unit.FactionID+") teleported from: '"+From.Name+"' to '"+To.Name+"'");
			}
			UnitTeleport (From,To,Unit);
		}
		public void OnPortalDoubleClick (Portal From, Portal To, Unit Unit) //called when a unit teleports in a portal
		{
			if (DebugEnabled == true) {
				Debug.Log ("Moved camera view from '"+From.Name+"' to '"+To.Name+"'");
			}
			PortalDoubleClick (From,To,Unit);
		}

		//Attack:
		public void OnUnitSwitchAttack (Unit Unit, Attack From, Attack To) //called when a unit switchs attack type:
		{
			if (DebugEnabled == true) {
				Debug.Log ("Unit '"+ Unit.Name +"' (Faction ID: "+Unit.FactionID+") has changed its attack type");
			}
			UnitSwitchAttack (Unit,From,To);
		}

		//Unit-Healing events:
		public void OnUnitStartHealing (Unit Unit, Unit TargetUnit) //called when a unit starts healing another unit
		{
			if (DebugEnabled == true) {
				Debug.Log ("Unit '"+Unit.Name+"' (Faction ID "+Unit.FactionID+") started healing unit: '"+TargetUnit.Name+"'");
			}
			UnitStartHealing (Unit, TargetUnit);
		}
		public void OnUnitStopHealing (Unit Unit, Unit TargetUnit) //called when a unit stops healing another unit
		{
			if (DebugEnabled == true) {
				Debug.Log ("Unit '"+Unit.Name+"' (Faction ID "+Unit.FactionID+") stopped healing unit: '"+TargetUnit.Name+"'");
			}
			UnitStopHealing (Unit, TargetUnit);
		}

		//Unit-Converting events:
		public void OnUnitStartConverting (Unit Unit, Unit TargetUnit) //called when a unit starts converting another unit
		{
			if (DebugEnabled == true) {
				Debug.Log ("Unit '"+Unit.Name+"' (Faction ID "+Unit.FactionID+") started converting unit: '"+TargetUnit.Name+"'");
			}
			UnitStartConverting (Unit, TargetUnit);
		}
		public void OnUnitStopConverting (Unit Unit, Unit TargetUnit) //called when a unit stops converting another unit
		{
			if (DebugEnabled == true) {
				Debug.Log ("Unit '"+Unit.Name+"' (Faction ID "+Unit.FactionID+") stopped converting unit: '"+TargetUnit.Name+"'");
			}
			UnitStopConverting (Unit, TargetUnit);
		}
		public void OnUnitConverted (Unit Unit, Unit TargetUnit) //called when a unit is converted
		{
			if (DebugEnabled == true) {
				Debug.Log (TargetUnit.Name+" has been converted.");
			}
			UnitConverted (Unit, TargetUnit);
		}

		//Building custom events:
		public void OnBuildingPlaced (Building Building) //called when a building is placed:
		{
			if (DebugEnabled == true) {
				Debug.Log ("Building '"+Building.Name+"' (Faction ID "+Building.FactionID+") placed");
			}
			BuildingPlaced (Building);
		}
		public void OnBuildingBuilt (Building Building) //called when a building is built:
		{
			if (DebugEnabled == true) {
				Debug.Log ("Building '"+Building.Name+"' (Faction ID "+Building.FactionID+") built");
			}
			BuildingBuilt (Building);
		}
		public void OnBuildingDestroyed (Building Building) //called when a building is placed:
		{
			if (DebugEnabled == true) {
				Debug.Log ("Building '"+Building.Name+"' (Faction ID "+Building.FactionID+") destroyed");
			}
			BuildingDestroyed (Building);
		}
		public void OnBuildingSelected (Building Building) //called when a building is placed:
		{
			if (DebugEnabled == true) {
				Debug.Log ("Building '"+Building.Name+"' (Faction ID "+Building.FactionID+") selected");
			}
			BuildingSelected (Building);
		}
		public void OnBuildingDeselected (Building Building) //called when a building is placed:
		{
			if (DebugEnabled == true) {
				Debug.Log ("Building '"+Building.Name+"' (Faction ID "+Building.FactionID+") deselected");
			}
			BuildingDeselected (Building);
		}
		public void OnBuildingStartUpgrade (Building Building, bool Direct) //called when a building starts the process of an upgrade:
		{
			if (DebugEnabled == true) {
				Debug.Log("Building '"+Building.Name+"' (Faction ID "+Building.FactionID+") started an upgrade.");
			}
			BuildingStartUpgrade(Building, Direct);
		}
		public void OnBuildingCompleteUpgrade (Building Building, bool Direct) //called when a building starts the process of an upgrade:
		{
			if (DebugEnabled == true) {
				Debug.Log("Building '"+Building.Name+"' (Faction ID "+Building.FactionID+") is the result of a building upgrade.");
			}
			BuildingCompleteUpgrade(Building, Direct);
		}
        public void OnBuildingStartPlacement(Building Building) //called when a building started getting placed.
        {
            if (DebugEnabled == true)
            {
                Debug.Log("Building '" + Building.Name + "' (Faction ID " + Building.FactionID + ") started getting placed.");
            }
            BuildingStartPlacement(Building);
        }
        public void OnBuildingStopPlacement(Building Building) //called when a building stopped getting placed.
        {
            if (DebugEnabled == true)
            {
                Debug.Log("Building '" + Building.Name + "' (Faction ID " + Building.FactionID + ") stopped getting placed.");
            }
            BuildingStopPlacement(Building);
        }

        //Building's Health related event:
        public void OnBuildingHealthUpdated (Building building, float value, GameObject source)
        {
            if(DebugEnabled == true)
            {
                Debug.Log("Building '" + building.Name + "' (Faction ID " + building.FactionID + ") health has been updated by: " + value.ToString());
            }
            BuildingHealthUpdated(building, value, source);
        }

        //Border componen's health related event:
        public void OnBorderActivated (Border border)
        {
            if(DebugEnabled == true)
            {
                Debug.Log("Border attached to building '" + border.MainBuilding.Name + "' (Faction ID " + border.MainBuilding.FactionID + ") has been activated.");
            }
            BorderActivated(border);
        }

        //APC:
        public void OnAPCAddUnit (APC APC, Unit Unit) //called when an APC adds a unit.
		{
			if (DebugEnabled == true) {
				string APCName = "";
				if (APC.gameObject.GetComponent<Unit> ()) {
					APCName = APC.gameObject.GetComponent<Unit> ().Name;
				} else if (APC.gameObject.GetComponent<Building> ()) {
					APCName = APC.gameObject.GetComponent<Building> ().Name;
				}
				Debug.Log("APC '"+APCName+"' added unit: "+Unit.Name);
			}
			APCAddUnit(APC, Unit);
		}

		public void OnAPCRemoveUnit (APC APC, Unit Unit) //called when an APC removes a unit.
		{
			if (DebugEnabled == true) {
				string APCName = "";
				if (APC.gameObject.GetComponent<Unit> ()) {
					APCName = APC.gameObject.GetComponent<Unit> ().Name;
				} else if (APC.gameObject.GetComponent<Building> ()) {
					APCName = APC.gameObject.GetComponent<Building> ().Name;
				}
				Debug.Log("APC '"+APCName+"' removed unit: "+Unit.Name);
			}
			APCRemoveUnit(APC, Unit);
		}

		public void OnAPCCallUnits (APC APC, Unit Unit) //called when an APC removes a unit (Unit here is irrelevant)
		{
			if (DebugEnabled == true) {
				string APCName = "";
				if (APC.gameObject.GetComponent<Unit> ()) {
					APCName = APC.gameObject.GetComponent<Unit> ().Name;
				} else if (APC.gameObject.GetComponent<Building> ()) {
					APCName = APC.gameObject.GetComponent<Building> ().Name;
				}
				Debug.Log("APC '"+APCName+"' is calling for units.");
			}
			APCCallUnits(APC, Unit);
		}

		//Task Events:
        public void OnTaskLauncherAdded (TaskLauncher TaskComp) //called when a new task launcher has been added
        {
            if (DebugEnabled == true)
            {
                Debug.Log("Task Launcher has been added.");
            }
            TaskLauncherAdded(TaskComp);
        }
         
        public void OnTaskLauncherRemoved(TaskLauncher TaskComp) //called when a task launcher has been removed
        {
            if (DebugEnabled == true)
            {
                Debug.Log("Task Launcher has been removed.");
            }
            TaskLauncherRemoved(TaskComp);
        }

        public void OnTaskLaunched (TaskLauncher TaskComp, int TaskID, int TaskQueueID) //called when a task launcher launches a task
		{
			if (DebugEnabled == true) {
				Debug.Log ("Task Launcher launched a task.");
			}
			TaskLaunched (TaskComp, TaskID, TaskQueueID);
		}
		public void OnTaskCanceled(TaskLauncher TaskComp, int TaskID, int TaskQueueID) //called when a task launcher cancels a task
        {
			if (DebugEnabled == true) {
				Debug.Log ("Task Launcher canceled a pending task.");
			}
			TaskCanceled(TaskComp, TaskID, TaskQueueID);
        }
		public void OnTaskCompleted(TaskLauncher TaskComp, int TaskID, int TaskQueueID) //called when a task launcher completes a task
        {
			if (DebugEnabled == true) {
				Debug.Log ("Task Launcher completed a task.");
			}
			TaskCompleted(TaskComp, TaskID, TaskQueueID);
        }

        //Population Events:
        public void OnCurrentPopulationUpdated (GameManager.FactionInfo factionInfo, int value) //called when the current population of a faction is updated
        {
            if (DebugEnabled == true)
            {
                Debug.Log(factionInfo.Name + " just updated its current population by "+ value.ToString());
            }
            CurrentPopulationUpdated(factionInfo, value);
        }

        public void OnMaxPopulationUpdated(GameManager.FactionInfo factionInfo, int value)
        {
            if (DebugEnabled == true)
            {
                Debug.Log(factionInfo.Name + " just updated its max population by " + value.ToString());
            }
            MaxPopulationUpdated(factionInfo, value);
        }

        //Resource events:
        public void OnResourceEmpty (Resource Resource) //called when a resource is empty
		{
			if (DebugEnabled == true) {
				Debug.Log ("Resource '"+Resource.Name+"' is now empty");
			}
			ResourceEmpty (Resource);
		}
		public void OnResourceSelected (Resource Resource) //called when a resource is selected
		{
			if (DebugEnabled == true) {
				Debug.Log ("Resource '"+Resource.Name+"' is selected");
			}
			ResourceSelected (Resource);
		}
		public void OnResourceDeselected (Resource Resource) //called when a resource is desselected
		{
			if (DebugEnabled == true) {
				Debug.Log ("Resource '"+Resource.Name+"' is deselected");
			}
			ResourceDeselected (Resource);
		}

		//Game events:
		public void OnFactionEliminated (GameManager.FactionInfo FactionInfo)
		{
			if (DebugEnabled == true) {
				Debug.Log ("Faction: " + FactionInfo.Name + " has been eliminated from the game.");
			}
			FactionEliminated (FactionInfo);
		}
		public void OnFactionWin (GameManager.FactionInfo FactionInfo)
		{
			if (DebugEnabled == true) {
				Debug.Log ("Faction: " + FactionInfo.Name + " won the game.");
			}
			FactionWin (FactionInfo);
		}

		//Invisibility events:
		public void OnUnitGoInvisible (Unit Unit)
		{
			if (DebugEnabled == true) {
				Debug.Log ("Unit: " + Unit.Name + " just went invisible.");
			}
			UnitGoInvisible (Unit);
		}
		public void OnUnitGoVisible (Unit Unit)
		{
			if (DebugEnabled == true) {
				Debug.Log ("Unit: " + Unit.Name + " just went visible.");
			}
			UnitGoVisible (Unit);
		}

        public void OnAttackTargetLocked(Attack Source, GameObject Target)
        {
            if (DebugEnabled == true)
            {
                string TargetName = "NULL";
                //it's either a unit or a building
                if (Target.gameObject.GetComponent<Unit>())
                {
                    TargetName = Target.gameObject.GetComponent<Unit>().Name;
                }
                else
                {
                    TargetName = Target.gameObject.GetComponent<Building>().Name;
                }
                Debug.Log("Attacker: " + Source.gameObject.name + " just locked victim: " + TargetName);
            }
            AttackTargetLocked(Source, Target);
        }

        public void OnAttackPerformed(Attack Source, GameObject Target)
        {
            if (DebugEnabled == true)
            {
                string TargetName = "NULL";
                //it's either a unit or a building
                if (Target.gameObject.GetComponent<Unit>())
                {
                    TargetName = Target.gameObject.GetComponent<Unit>().Name;
                }
                else
                {
                    TargetName = Target.gameObject.GetComponent<Building>().Name;
                }
                Debug.Log("Attacker: " + Source.gameObject.name + " just performed attack on victim: " + TargetName);
            }
            AttackPerformed(Source, Target);
        }

        public void OnAttackerInRange(Attack Source, GameObject Target)
        {
            if (DebugEnabled == true)
            {
                string TargetName = "NULL";
                //it's either a unit or a building
                if (Target.gameObject.GetComponent<Unit>())
                {
                    TargetName = Target.gameObject.GetComponent<Unit>().Name;
                }
                else
                {
                    TargetName = Target.gameObject.GetComponent<Building>().Name;
                }
                Debug.Log("Attacker: " + Source.gameObject.name + " is in range of " + TargetName);
            }
            AttackerInRange(Source, Target);
        }

        //Selection Obj collision:
        public void OnSelectionObjEnter (SelectionObj Source, SelectionObj Target)
        {
            if (DebugEnabled == true)
            {
                Debug.Log("Collision detected between a " + Source.ObjType + " and a " + Target.ObjType);
            }

            SelectionObjEnter(Source, Target);
        }

        //custom action events:
        public void OnCustomCommand(GameObject Source, GameObject Target, Vector3 InitialPos, Vector3 TargetPos, int Value)
        {
            CustomCommand(Source, Target, InitialPos, TargetPos, Value);
		}
	}
}