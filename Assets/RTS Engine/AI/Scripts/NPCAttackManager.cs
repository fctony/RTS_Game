using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* NPC Attack Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class NPCAttackManager : NPCComponent
    {
        //picking an attack target:
        public bool canAttack = true; //can this faction attack?
        public bool pickWeakestFaction = true; //will this faction pick the weakest opponent to attack?
        public FloatRange setTargetFactionDelay = new FloatRange(10, 15); //target faction will only be set after this delay is done
        private float setTargetFactionTimer;
        private FactionManager targetFaction; //the faction manager component of the target faction.

        //launching the attack:
        private bool isAttacking = false; //is the faction currently in an attack?

        public bool IsAttacking() { return isAttacking; } //get if the attack manager is attacking or not.

        //timer at which the faction decides to attack target faction or not:
        public FloatRange launchAttackReloadRange = new FloatRange(10.0f, 15.0f);
        private float launchAttackTimer;

        //the launch attack power is required to have as the current attack power in order to launch an attack on another faction.
        public IntRange launchAttackPowerRange = new IntRange(300, 400);

        //attacking:
        //whenever the below timer is through, this component will point army units to a target.
        public FloatRange attackOrderReloadRange = new FloatRange(3.0f, 7.0f);
        private float attackOrderTimer;

        private Vector3 lastAttackPos; //the last position of the target building in the attack
        //list of units that will participate in the attack:
        private List<Unit> currentAttackUnits = new List<Unit>();

        //a list of the buildings codes that this faction will attempt to attack:
        public List<Building> targetBuildings = new List<Building>();
        private List<string> targetBuildingCodes = new List<string>(); //codes of the above buildings will be saved here.
        private Building currentTargetBuilding; //the current building that this faction is attempting to destroy.

        //when the faction's army attack power goes below this value while the faction attacking another one, then a retreat will take place:
        public IntRange surrenderAttackPowerRange = new IntRange(100, 200);

        GameManager gameMgr;
        MovementManager mvtMgr;

        //CREATE NEW NPC HEALERS MANAGER: MOVE HEALERS FROM POS A TO POS B when army is moving
        //SAME THING TO APPLY FOR CONVERTERS.

        //NPC Manager: Editor script that can create NPC difficulty levels and manage them including:
        //a generate unit regulator tool that scans already created regulators and unit prefabs to produce unit regulators
        //a generate buildings regulator tool that does the same for buildings
        //user will have created regulators appear in a scroll list and he can select them and they will be selected in inspector to modify
        //create everything in known paths in the project and manipulate everything using that path.

        void Start()
        {
            gameMgr = GameManager.Instance;
            mvtMgr = MovementManager.Instance;

            //if we can't attack then disable this component:
            if (canAttack == false)
                enabled = false;
            else
            {
                targetFaction = null;
                setTargetFactionTimer = setTargetFactionDelay.getRandomValue(); //start the set attack target timer.
            }

            //start timers:
            setTargetFactionTimer = setTargetFactionDelay.getRandomValue();
            launchAttackTimer = launchAttackReloadRange.getRandomValue();

            //assign target buildings codes:
            UpdateTargetBuildingCodes();

            //start listening to events:
            CustomEvents.UnitDead += OnUnitDead;
            CustomEvents.UnitConverted += OnUnitConverted;
            CustomEvents.FactionEliminated += OnFactionEliminated;
        }

        private void OnDisable()
        {
            //stop listening to events:
            CustomEvents.UnitDead -= OnUnitDead;
            CustomEvents.UnitConverted -= OnUnitConverted;
            CustomEvents.FactionEliminated -= OnFactionEliminated;
        }

        //called whenever a unit is dead:
        void OnUnitDead (Unit unit)
        {
            //if the unit is in the current attack units list, it will be removed:
            currentAttackUnits.Remove(unit);
        }

        //called whenever a unit is converted:
        void OnUnitConverted (Unit converter, Unit target)
        {
            //if the unit is in the current attack units list, it will be removed:
            currentAttackUnits.Remove(target);
        }

        //called whenever a faction is eliminated:
        void OnFactionEliminated (GameManager.FactionInfo factionInfo)
        {
            //if this is the current target faction?
            if(factionInfo.FactionMgr == targetFaction)
            {
                CancelAttack(); //cancel the attack
            }
        }

        //a method to assign codes of the target buildings list in another list:
        void UpdateTargetBuildingCodes ()
        {
            targetBuildingCodes.Clear();

            //go through the target buildings
            foreach(Building b in targetBuildings)
            {
                if (targetBuildingCodes.Contains(b.Code) == false) //if the building's code doesn't already exist.
                    targetBuildingCodes.Add(b.Code);
            }
        }

        void Update()
        {
            //this component is only active if the peace time is over:
            if (gameMgr.PeaceTime > 0)
                return; 

            SetTargetProgress();

            LaunchAttackProgress();

            AttackProgress();
        }

        //a method that runs the set target faction timer in order to assign a new target faction
        void SetTargetProgress ()
        {
            //as long as there's no target faction assigned & peace time is over:
            if (targetFaction == null && gameMgr.PeaceTime <= 0.0f)
            {
                //setting the attack target timer:
                if (setTargetFactionTimer > 0)
                    setTargetFactionTimer -= Time.deltaTime;
                else
                {
                    setTargetFactionTimer = setTargetFactionDelay.getRandomValue(); //reload the set attack target timer.
                    SetTargetFaction(); //find a target faction.
                }
            }
        }

        //a method to set a target faction:
        void SetTargetFaction()
        {
            //first get the factions that are not yet defeated in a list:
            List<GameManager.FactionInfo> activeFactions = new List<GameManager.FactionInfo>();
            activeFactions.AddRange(gameMgr.Factions);

            //remove the defeated factions and pick the weakest:
            FactionManager weakestFaction = null;

            int i = 0; //counter
            while(i < activeFactions.Count)
            {
                //if this is the player's faction or faction is already defeated:
                if(activeFactions[i].FactionMgr == factionMgr || activeFactions[i].Lost == true)
                {
                    //remove from list:
                    activeFactions.RemoveAt(i);
                }
                else
                {
                    if (pickWeakestFaction == true) //if we're picking the weakest faction as the target:
                    {
                        //look for weakest faction:
                        if (weakestFaction == null)
                            weakestFaction = activeFactions[i].FactionMgr;
                        //if this faction has less army power than the current weakest:
                        else if (weakestFaction.GetCurrentAttackPower() > activeFactions[i].FactionMgr.GetCurrentAttackPower())
                        {
                            //assign new weakest faction:
                            weakestFaction = activeFactions[i].FactionMgr;
                        }
                    }

                    //increment timer:
                    i++;
                }
            }

            //pick weakest faction or random faction:
            targetFaction = (pickWeakestFaction == true) ? weakestFaction : activeFactions[Random.Range(0, activeFactions.Count)].FactionMgr;
        }

        void LaunchAttackProgress ()
        {
            //launching an attack:
            //if we aren't currently in an attack and there's a valid target faction and the faction isn't defending its territory:
            if (IsAttacking() == false && targetFaction != null && npcMgr.defenseManager_NPC.IsDefending() == false)
            {
                //launch attack timer:
                if (launchAttackTimer > 0)
                    launchAttackTimer -= Time.deltaTime;
                else
                {
                    //reload timer:
                    launchAttackTimer = launchAttackReloadRange.getRandomValue();

                    //does the NPC faction has enough attacking power to launch attack?
                    if(factionMgr.GetCurrentAttackPower() > launchAttackPowerRange.getRandomValue())
                    {
                        //launch attack:
                        LaunchAttack();
                    }
                }
            }
        }

        //method to launch the attack:
        void LaunchAttack ()
        {
            //making sure there's a valid target faction:
            if (targetFaction == null)
                return;

            //mark as attacking:
            isAttacking = true;

            //get the units that will go to attack:
            int attackUnitsAmount = (int) (factionMgr.Army.Count * (1- npcMgr.defenseManager_NPC.defenseRatioRange.getRandomValue()) );
            //clear the current attack units list:
            currentAttackUnits.Clear();
            currentAttackUnits.AddRange(factionMgr.Army.GetRange(0, attackUnitsAmount)); //get the required units for this attack.

            //we'll be searching for the next building to attack starting from the last attack pos, initially set it as the capital building
            lastAttackPos = gameMgr.Factions[factionMgr.FactionID].CapitalBuilding.transform.position;

            currentTargetBuilding = null;
            //pick a target building:
            SetTargetBuilding();

            //start the attack order timer:
            attackOrderTimer = attackOrderReloadRange.getRandomValue();
        }

        //a method that picks a target building to attack:
        void SetTargetBuilding ()
        {
            //search the target faction's buildings and see if there's a match:
            int i = 0;
            float lastDistance = 0; //we wanna get the closest building to the capital:

            while (i < targetFaction.Buildings.Count)
            {
                //if the building is valid:
                if (targetFaction.Buildings[i] != null)
                {
                    //and the building's code matches.
                    if (targetBuildingCodes.Contains(targetFaction.Buildings[i].Code))
                    {
                        //get the closest building:
                        if (currentTargetBuilding == null || Vector3.Distance(currentTargetBuilding.transform.position, lastAttackPos) < lastDistance)
                        {
                            currentTargetBuilding = targetFaction.Buildings[i];
                            lastDistance = Vector3.Distance(targetFaction.Buildings[i].transform.position, lastAttackPos);
                        }
                    }
                }

                i++;
            }
        }

        //when the NPC faction is attacking:
        void AttackProgress ()
        {
            //if we're attacking and there's a valid target faction:
            if(isAttacking == true && targetFaction != null)
            {
                //attack order timer:
                if (attackOrderTimer > 0)
                    attackOrderTimer -= Time.deltaTime;
                else
                {
                    //reload attack order timer:
                    attackOrderTimer = attackOrderReloadRange.getRandomValue();

                    //did the current attack power hit the surrender attack power?
                    if(factionMgr.GetCurrentAttackPower() <= surrenderAttackPowerRange.getRandomValue())
                    {
                        CancelAttack();
                        return; //do not proceed.
                    }

                    //does the faction has a target building:
                    if(currentTargetBuilding != null)
                    {
                        //attack it:
                        AttackTargetBuilding();
                    }
                    else //if it doesn't have one yet,
                    {
                        SetTargetBuilding(); //pick one.
                    }
                }
            }
        }

        //attack the assigned target building.
        void AttackTargetBuilding ()
        {
            //making sure that the NPC faction in an attack in progress and that there's a valid target building:
            if (currentTargetBuilding == null || isAttacking == false)
                return;

            GameObject target = currentTargetBuilding.gameObject;
            MovementManager.AttackModes attackMode = MovementManager.AttackModes.None;

            //if the current target building is being constructed:
            if(currentTargetBuilding.WorkerMgr.CurrentWorkers > 0)
            {
                //attack the workers first: go through the workers positions
                for(int i = 0; i < currentTargetBuilding.WorkerMgr.WorkerPositions.Length; i++)
                {
                    //find worker:
                    if(currentTargetBuilding.WorkerMgr.WorkerPositions[i].CurrentUnit != null)
                    {
                        //is the worker actually at the building constructing it:
                        if (currentTargetBuilding.WorkerMgr.WorkerPositions[i].CurrentUnit.BuilderMgr.IsBuilding == true)
                        {
                            //assign it as target.
                            target = currentTargetBuilding.WorkerMgr.WorkerPositions[i].CurrentUnit.gameObject;
                            //force attack units to attack it:
                            attackMode = MovementManager.AttackModes.Change;
                        }
                    }
                }
            }

            //launch the actual attack:
            mvtMgr.LaunchAttack(currentAttackUnits, target, attackMode);
        }

        //a method that checks if a unit is part of the attacking army or not:
        public bool IsUnitDeployed (Unit unit)
        {
            return currentAttackUnits.Contains(unit);
        }

        //a method to cancel the attack:
        public void CancelAttack ()
        {
            //send back units:
            npcMgr.defenseManager_NPC.SendBackUnits(currentAttackUnits);

            //clear the current attack units:
            currentAttackUnits.Clear();

            currentTargetBuilding = null; //reset the target building.

            targetFaction = null;

            //stop attacking:
            isAttacking = false;
        }
    }
}
