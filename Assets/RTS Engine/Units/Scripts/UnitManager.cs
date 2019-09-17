using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace RTSEngine
{
	public class UnitManager : MonoBehaviour {

		public static UnitManager Instance;

		[Header("Free Units:")]
		public Unit[] FreeUnits; //units that don't belong to any faction here.
		public Color FreeUnitSelectionColor = Color.black;

        [Header("Animations:")]
        public AnimatorOverrideController DefaultAnimController; //default override animation controller: used when there's no animation override controller assigned to a unit

        //a list of upgrades that can be applied to units:
        public class UpgradeListVars
		{
			public float Speed = 0.0f;
			public float UnitDamage = 0.0f;
			public float BuildingDamage = 0.0f;
			public float AttackReload = -0.2f;
			public float SearchRange = 3.0f;
			public float FoWSize = 3.0f;
			public float MaxHealth = 30.0f;
		}

		public class FactionUnitUpradesVars
		{
			//a list of units who receive upgrades in the current game:
			[HideInInspector]
			public List<string> UnitsToUpgrade = new List<string>();
			[HideInInspector]
			//list of upgrades for units in the list above:
			public List<UpgradeListVars> UpgradeList = new List<UpgradeListVars>();
		}
		public FactionUnitUpradesVars[] FactionUnitUpgrades; //each faction has a slot in this list

		void Awake ()
		{
			if (Instance == null) {
				Instance = this;
			} else if (Instance != this) {
				Destroy (gameObject);
			}
		}

		void Start ()
		{
			FactionUnitUpgrades = new FactionUnitUpradesVars[GameManager.Instance.Factions.Count]; //create a slot for each faction:
			for (int i = 0; i < GameManager.Instance.Factions.Count; i++) {
				FactionUnitUpgrades [i] = new FactionUnitUpradesVars ();

				//reset the unit- and upgrade list at the start:
				FactionUnitUpgrades [i].UnitsToUpgrade.Clear ();
				FactionUnitUpgrades [i].UpgradeList.Clear ();
			}

		}

        void OnEnable ()
		{
			CustomEvents.UnitCreated += OnUnitCreated;
			CustomEvents.UnitConverted += OnUnitConverted;
		}

		void OnDisable ()
		{
			CustomEvents.UnitCreated -= OnUnitCreated;
			CustomEvents.UnitConverted -= OnUnitConverted;
		}

		void OnUnitCreated (Unit Unit)
		{
            if (Unit.FreeUnit) //if this is a free unit then don't proceed
                return;

			//when a reseach on units is done, they are added to a list in this class and whenever they are created, they get their upgrades here:
			if (FactionUnitUpgrades[Unit.FactionID].UnitsToUpgrade.Contains (Unit.Code)) { //when the unit is in the upgrade list:
				int ID = FactionUnitUpgrades[Unit.FactionID].UnitsToUpgrade.IndexOf(Unit.Code); //get the ID of this unit inside the upgrade list
				Unit.Speed += FactionUnitUpgrades[Unit.FactionID].UpgradeList[ID].Speed;
				Unit.MaxHealth += FactionUnitUpgrades[Unit.FactionID].UpgradeList[ID].MaxHealth;
				Unit.Health = Unit.MaxHealth;
				if (Unit.gameObject.GetComponent<Attack> ()) {
					Unit.gameObject.GetComponent<Attack> ().UnitDamage += FactionUnitUpgrades[Unit.FactionID].UpgradeList [ID].UnitDamage;
					Unit.gameObject.GetComponent<Attack> ().BuildingDamage += FactionUnitUpgrades[Unit.FactionID].UpgradeList [ID].BuildingDamage;
					//NEED A WAY TO MAKE DAMAGE HIGHER FOR CUSTOM DAMAGE AS WELL
					Unit.gameObject.GetComponent<Attack> ().AttackReload += FactionUnitUpgrades[Unit.FactionID].UpgradeList [ID].AttackReload;
					Unit.gameObject.GetComponent<Attack> ().SearchRange += FactionUnitUpgrades[Unit.FactionID].UpgradeList [ID].SearchRange;
				}
			}
		}

		//Converter events:
		void OnUnitConverted (Unit Unit, Unit TargetUnit)
		{
			if (TargetUnit != null) {
				//if the unit is selected:
				if (GameManager.Instance.SelectionMgr.SelectedUnits.Contains (TargetUnit)) {
					//if this is the faction that the unit got converted to or this is the only unit that the player is selecting:
					if (TargetUnit.FactionID == GameManager.PlayerFactionID || GameManager.Instance.SelectionMgr.SelectedUnits.Count == 1) {
						//simply re-select the player:
						GameManager.Instance.SelectionMgr.SelectUnit (TargetUnit, false);
					} else { //this means this is the faction that the target belonged to before and that player is selecting multiple units including the newly converted target unit
						//deselect it:
						GameManager.Instance.SelectionMgr.DeselectUnit (TargetUnit);
					}
				}
			}
		}

        public static Unit CreateUnit (Unit unitPrefab, Vector3 spawnPos, int factionID, Building createdBy, bool freeUnit = false)
        {
            //only if the prefab is valid:
            if (unitPrefab == null)
                return null;

            // create the new unit:
            unitPrefab.gameObject.GetComponent<NavMeshAgent>().enabled = false; //disable this component before spawning the unit as it might place the unit in an unwanted position when spawned
            Unit newUnit = Instantiate(unitPrefab.gameObject, spawnPos, unitPrefab.transform.rotation).GetComponent<Unit>();

            //set the unit faction ID.
            if(freeUnit == false)
                newUnit.FactionID = factionID;
            else
            {
                newUnit.FreeUnit = true;
            }

            newUnit.CreatedBy = createdBy; //unit is created by which building? (if there's any)

            newUnit.gameObject.GetComponent<NavMeshAgent>().enabled = true; //enable the nav mesh agent component for the newly created unit

            return newUnit;
        }
    }
}