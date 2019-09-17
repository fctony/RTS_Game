using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

/* Attack script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class Attack : MonoBehaviour
    {
        public int AttackPower = 10; //a value that represents the attack power of this component:

        public bool IsActive = true; //Is this attack type active? 

        public string AttackCode; //unique code for each attack type
        public Sprite AttackIcon; //an icon to represent this attack type in the task panel.

        public bool BasicAttack = true; //In case the player only has multiple attacks, this is the attack that will be usable directly by the right mouse button
        public bool DealDamage = true; //If set to false, then no damage will be dealt to the target but a custom event will be triggered in place

        [HideInInspector]
        public int AttackID = -1; //in case there are a lot of other attack components on this object.

        public bool DirectAttack = false; //If set to true, the unit will affect damage when in range with the target, if not the damage will be affected within an object released by the unit (like a particle effect).
        public bool MoveOnAttack = false; //Is the unit allowed to move while attacking?

        //Attack type:
        public bool AttackAllTypes = true;
        public bool AttackUnits = true;
        public bool AttackBuildings = true;
        public bool AttackOnlyInList = false; //Be able to attack the units/buildings in the below list only? or be able to attack all units/buidlings beside the ones in the list below.
        public List<string> CodesList = new List<string>(); //A list that defines what units/buildings can (not) this attack

        public bool UseReload = true; //Use reload time?
        public float AttackReload = 2.0f; //Time between two successive attacks
        float AttackTimer;

        //unit range types:
        public string RangeType = "shortrange";
        [HideInInspector]
        public int RangeTypeID;

        [HideInInspector]
        public float LastBuildingDistance;
        //Attack range:
        public bool AttackOnAssign = true; //can attack when the player assigns a target?
        public bool AttackWhenAttacked = false; //is the unit allowed to defend itself when attacked? 
        public bool AttackOnce = false; //Cancel the attack as soon as an attack is performed.

        //Attack friendly units? for panic mode!
        public bool AttackFriendlyUnits = false;

        //AI related settings;
        [HideInInspector]
        public Building AttackRangeCenter; //which building center the units should protect?
        [HideInInspector]
        public bool AttackRangeFromCenter = false; //should the unit protect the building center:

        public bool AttackInRange = false; //when an enemy unit enter in range of this unit, can the unit attack it automatically?
        public float SearchRange = 10.0f; //searching for enemies range
        public float SearchReload = 1.0f; //Search for enemy units every ..
        float SearchTimer;
        //Target:
        public bool RequireAttackTarget = true; //if set to false then the player can attack anywhere in the map (suits areal attacks).
        [HideInInspector]
        public Vector3 AttackPosition;

        [HideInInspector]
        public GameObject AttackTarget; //the target (unit or building) that the unit is attacking.
        public Unit TargetUnit; //target unit
        public Building TargetBuilding; //target building
        [HideInInspector]
        public float FollowRange = 15.0f; //If the target leaves this range then the unit will stop following/attacking it.
        bool WasInTargetRange = false;

        //Attack target effect:
        public EffectObj AttackEffect; //this is the prefab of the attack effect that will be shown on the attak target's body when the attack is launched
        public float AttackEffectTime; //how long will the attack effect be shown for?

        public Vector3 LastTargetPos;

        //Attack damage:
        [System.Serializable]
        public class DamageVars
        {
            public string Code; //the code of the unit/building that will get the below damage
            public float Damage; //the amount of damage specifically for the building/unit with the above code.
        }
        public DamageVars[] CustomDamage; //if the target unit/building code is in the list then it will be given the matching damage, if not then the default damage

        //Attack type:
        public float UnitDamage = 10.0f; //damage points when this unit attacks another unit.

        public float BuildingDamage = 10.0f; //damage points when this unit attacks a building.

        //Area attack:
        public bool AreaDamage = false;
        [System.Serializable]
        public class AttackRangesVars
        {
            public float Range = 10.0f;
            public float UnitDamage = 5.0f;
            public float BuildingDamage = 4.0f;
        }
        public AttackRangesVars[] AttackRanges;

        //Attack Delay:
        public float DelayTime = 0.0f; //how much time before the actual attack is launched?
        float DelayTimer;
        public bool UseDelayTrigger = false; //if we want to use another component to trigger the attack, then enable this.
        [HideInInspector]
        public bool AttackTriggered = false;
        public bool PlayAnimInDelay = false; //should the character play the animation while waiting for the delay? 

        //DoT:
        [System.Serializable]
        public class DoTVars
        {
            public bool Enabled = false; //enable Damage over time?
            [HideInInspector]
            public float Damage; //How much damage will be dealt to the target? In the attack component, this is chosen from the other damage settings
            public bool Infinite = false; //if set to true, the DoT won't stop until the target is dead or another component requires that.
            public float Duration = 20.0f; //the duration of the DoT attack.
            public float Cycle = 4.0f; //When will damage be applied.
            public GameObject Source; //the attacker who launched this attack
        }
        public DoTVars DoT;

        //Cooldown:
        public bool EnableCoolDown = false;
        [HideInInspector]
        public bool InCoolDownMode = false;
        public float CoolDown = 10.0f;
        float CoolDownTimer;

        public enum AttackTypes { Random, InOrder }; //in the case of having a lot of attack sources, there are two mods, the first is to choose  and the second is attacking from all sources in order
        public AttackTypes AttackType = AttackTypes.Random & AttackTypes.InOrder;
        [System.Serializable]
        public class AttackSourceVars
        {
            //Delay:
            public float DelayTime = 0.2f; //Delay time for the attack object before it gets sent/created.
            public bool CreateOnDelay = false; //delay means that the attack object will be created but won't be actually sent to the target till the delay is done
            public bool DamageInDelay = true; //If the above field is true and this one as well then the attack object will be able to do damage even if delay mode.
            public Transform DelayParentObj; //Allows the attack object to stay as child of during the delay

            public EffectObj AttackObj; //attack object prefab (must have both the EffectObj and AttackObject components).
            public float AttackObjDestroyTime = 3.0f; //life duration of the attack object
            public Transform AttackObjSource; //Where will the attack object be sent from?
            public float AttackObjSpeed = 10.0f; //how fast is the attack object moving
            public bool DamageOnce = true; //should the attack object do damage once it hits a building/unit and then do no more damage.
            public bool DestroyAttackObjOnDamage = true; //should the attack object get destroyed after it has caused damage.

            public bool ReloadAttackAnim; //play the attack animation each time after this attack? 
        }
        public AttackSourceVars[] AttackSources;
        [HideInInspector]
        public Vector3 MvtVector; //The attack object movement direction
        public float AttackStepTimer;
        public int AttackStep;

        public GameObject WeaponObj; //When assigned, this object will be rotated depending on the target's position.
        public bool FreezeRotX = false;
        public bool FreezeRotY = false;
        public bool FreezeRotZ = false;
        public bool SmoothRotation = true; //allow smooth rotation?
        public float RotationDamping = 2.0f; //rotation damping (when smooth rotation is enabled)
        public bool ForceIdleRotation = true; //when the attacker is in idle mode and this is true, the weapon obj will rotate back to its idle rotation 
        public Vector3 WeaponIdleAngles;
        Quaternion WeaponIdleRotation;

        //Can the unit attack only if the target is in line of sight? 
        public bool EnableLOS = false;
        bool InLOS = false; //is the attacker in LOS of the target?
        public bool WeaponObjInLOS = false; //use the weapon object instead of the unit's main object as the reference for the line of sight
        public float LOSAngle = 40.0f; //between 0.0f and 1.0f, the close this value to 1.0f, the closer is the attacker looking at its target
        //Ignore one or more axis while considering LOS?
        public bool IgnoreLOSX = false;
        public bool IgnoreLOSY = false;
        public bool IgnoreLOSZ = false;

        //Attacking anim timer:
        bool CanPlayAnim = false; //is the unit playing the attack animation?

        //Damage dealt:
        float DamageDealt; //Amount of damage dealt to target
        public bool ReloadDamageDealt = false; //Each time the attacker aquires a new target the damage dealt count resets to 0 

        //is the attacker a building or a unit?
        public enum AttackerTypes { Unit, Building }
        public AttackerTypes AttackerType;

        //attack power:

        [HideInInspector]
        public Unit UnitMgr;
        [HideInInspector]
        public Building BuildingMgr;
        MovementManager MvtMgr;
        AttackManager AttackMgr;

        GameManager GameMgr;

        //Other scripts:
        EffectObjPool ObjPool;
        MultipleAttacks MultipleAttacksMgr;

        //Army Unit ID:
        [HideInInspector]
        public int ArmyUnitID = -1;

        //Audio:
        public AudioClip AttackOrderSound; //played when the unit is ordered to attack
        public AudioClip AttackSound; //played each time the unit attacks.

        void Awake()
        {
            CheckAttacker(); //check if the attacker setting matches the object where this component is at.

            AttackID = -1; //reset the attack ID (the multiple attack, if existant will set it later in Start () )

            //Set the DoT source if we're ever going to use that:
            DoT.Source = gameObject;

            //Get the multiple attack manager:
            MultipleAttacksMgr = gameObject.GetComponent<MultipleAttacks>();

            InCoolDownMode = false; //per default, not in cooldown mode
            IsActive = true; //per default, the attack type is active

            AttackFriendlyUnits = false; //don't attack friendly units per default
        }

        //check if the attacker setting matches the object where this component is at.
        void CheckAttacker()
        {
            //attempt to get the unit and building components
            UnitMgr = gameObject.GetComponent<Unit>();
            BuildingMgr = gameObject.GetComponent<Building>();

            //check if they are valid for the chosen attacker setting
            if (UnitMgr == null && AttackerType == AttackerTypes.Unit)
            {
                Debug.LogError("Attack is set to 'Unit' but there's no 'Unit.cs' component.");
            }
            else if (BuildingMgr == null && AttackerType == AttackerTypes.Building)
            {
                Debug.LogError("Attack is set to 'Building' but there's no 'Building.cs' component.");
            }
        }

        //determines if the attacker is dead or not
        bool CanAttack()
        {
            if (InCoolDownMode == true)
                return false;

            if (AttackerType == AttackerTypes.Unit) //if the attacker is a unit
            {
                if (UnitMgr.Dead == true) //and it's dead
                {
                    return false; //nope
                }
            }
            else if (AttackerType == AttackerTypes.Building) //if the attacker is a building
            {
                if (BuildingMgr.Health <= 0.0f || BuildingMgr.IsBuilt == false) //if the building's health is null (destroyed or getting destroyed) or the building is not built
                {
                    return false; //nope
                }
            }

            //if we reach this stage then
            return true; //yeah
        }

        //cancels the attack:
        void CancelAttack()
        {
            if (AttackerType == AttackerTypes.Unit) //if the attacker is a unit
            {
                UnitMgr.CancelAttack();
                if (UnitMgr.Moving == true) //if the unit is moving
                {
                    UnitMgr.StopMvt(); //stop
                }
            }

            //for both:
            AttackTarget = null; //set the target to null.
        }

        //is the attacker idle (when searching for an attack target, we need to check if the attacker is not doing something else):
        bool IsIdle()
        {
            if (AttackerType == AttackerTypes.Unit) //concerns units only
            {
                return UnitMgr.IsIdle();
            }

            return true;
        }

        //gets the attacker's faction ID
        public int AttackerFactionID()
        {
            if (AttackerType == AttackerTypes.Unit) //if the attacker is a unit
            {
                return UnitMgr.FactionID;
            }
            else if (AttackerType == AttackerTypes.Building) //if the attacker is a building
            {
                return BuildingMgr.FactionID;
            }

            return -1;
        }

        //Check if the unit can attack a specific faction:
        public bool CanAttackFaction(int FactionID)
        {
            if ((AttackerFactionID() != FactionID && AttackFriendlyUnits == false) || (AttackerFactionID() == FactionID && AttackFriendlyUnits == true))
                return true;

            return false;
        }

        //Determine if the target object is in the attacker's range or not
        public bool IsTargetInRange ()
        {
            if(AttackTarget != null)
            {
                float Range = (TargetUnit != null) ? AttackManager.Instance.RangeTypes[RangeTypeID].UnitStoppingDistance : AttackManager.Instance.RangeTypes[RangeTypeID].BuildingStoppingDistance;
                return Vector3.Distance(this.transform.position, AttackTarget.transform.position) <= Range + ((MoveOnAttack == true) ? AttackManager.Instance.RangeTypes[RangeTypeID].MoveOnAttackOffset : 0);
            }
            return false;
        }

        void Start()
        {
            //get the ame manager script
            GameMgr = GameManager.Instance;
            MvtMgr = MovementManager.Instance;
            AttackMgr = AttackManager.Instance;

            //default values for the timers:
            AttackTimer = 0.0f;
            SearchTimer = 0.0f;

            ObjPool = EffectObjPool.Instance;

            //if there's a weapon object:
            if (WeaponObj)
            {
                WeaponIdleRotation.eulerAngles = WeaponIdleAngles;
            }

            //get the range type ID from the attack manager
            RangeTypeID = AttackMgr.GetRangeTypeID(RangeType);
        }

        //method to handle cool down timer
        void UpdateAttackCooldown()
        {
            if (InCoolDownMode)
            {
                //Cooldown timer:
                if (CoolDownTimer > 0)
                {
                    CoolDownTimer -= Time.deltaTime;
                }
                else
                {
                    InCoolDownMode = false;
                    if (AttackerType == AttackerTypes.Unit)
                    {
                        //If the attacker is selected:
                        if (GameMgr.SelectionMgr.SelectedUnits.Contains(UnitMgr))
                        {
                            //reselect him:
                            GameMgr.UIMgr.UpdateUnitUI(UnitMgr);
                        }
                    }
                    else
                    {
                        if (GameMgr.SelectionMgr.SelectedBuilding == BuildingMgr)
                        {
                            //update building UI:
                            GameMgr.UIMgr.UpdateBuildingUI(BuildingMgr);
                        }
                    }
                }
            }
        }

        //method to search for a target
        bool SearchForTargetToAttack()
        {
            //Search if there are enemy units in range:
            bool Found = false;

            float Size = SearchRange;
            Vector3 SearchFrom = transform.position;

            //only for NPC factions:

            //if there's no city center to protect:
            if (AttackRangeCenter == null)
            {
                AttackRangeFromCenter = false; //we're not defending any city center then:
            }
            //if there's a city center to protect
            if (AttackRangeFromCenter == true && AttackRangeCenter != null)
            {
                SearchFrom = AttackRangeCenter.transform.position; //the search pos is the city center
                Size = AttackRangeCenter.GetComponent<Border>().Size; //and the search size is the whole city border size:
            }

            Collider[] ObjsInRange = Physics.OverlapSphere(SearchFrom, Size);
            int i = 0; //counter
            while (i < ObjsInRange.Length && Found == false)
            {
                Unit UnitInRange = ObjsInRange[i].gameObject.GetComponent<Unit>();
                if (UnitInRange && UnitInRange != UnitMgr) //make sure that there's a unit in range and that unit is not this one.
                { //If it's a unit object 
                    if (UnitInRange.enabled == true)
                    { //if the unit comp is enabled
                      //If this unit and the target have different teams and make sure it's not dead.
                        if (CanAttackFaction(UnitInRange.FactionID) && UnitInRange.Dead == false)
                        {
                            //if the unit is visible:
                            if (UnitInRange.IsInvisible == false)
                            {
                                if (AttackTarget == null)
                                {
                                    if (AttackMgr.CanAttackTarget(this, ObjsInRange[i].gameObject))
                                    { //if the unit can attack the target.
                                      //Set this unit as the target
                                      //if this is a unit:
                                        if (AttackerType == AttackerTypes.Unit)
                                        {
                                            MvtMgr.LaunchAttack(UnitMgr, ObjsInRange[i].gameObject, MovementManager.AttackModes.None);
                                        }
                                        else //if this is a building
                                        {
                                            SetAttackTarget(ObjsInRange[i].gameObject);
                                        }
                                        Found = true;
                                    }
                                }
                            }
                        }
                    }
                }

                i++;
            }

            return Found;
        }

        void Update()
        {
            UpdateAttackCooldown();

            //Attack timer:
            if (AttackTimer > 0 && UseReload == true)
            {
                AttackTimer -= Time.deltaTime;
            }

            //if the attack is not active then don't proceed
            if (IsActive == false)
            {
                return;
            }

            //if the attack is not active then don't proceed
            if (IsActive && CanAttack() && !GameMgr.InPeaceTime())
            {
                if (AttackTarget == null)
                {
                    if (WeaponObj != null && ForceIdleRotation == true) //if there's a weapon object and we force the idle rotation
                    {
                        if (SmoothRotation == false)
                        { //if the rotation is automatically changed
                            WeaponObj.transform.localRotation = WeaponIdleRotation;
                        }
                        else
                        {
                            //smooth rotation here:
                            WeaponObj.transform.localRotation = Quaternion.Slerp(WeaponObj.transform.localRotation, WeaponIdleRotation, Time.deltaTime * RotationDamping);
                        }
                    }

                    if (GameManager.MultiplayerGame == false || (GameManager.MultiplayerGame == true && GameMgr.IsLocalPlayer(gameObject) == true))
                    { //if this is an offline game or online but this is the local player
                        if (AttackInRange == true && IsIdle() == true) //if the attacker can attack in range and it's also in idle state
                        {
                            //if the faction is a NPC or a local player and having a target is required.
                            if (GameManager.PlayerFactionID != AttackerFactionID() || (GameManager.PlayerFactionID == AttackerFactionID() && RequireAttackTarget == true))
                            {
                                if (SearchTimer > 0)
                                {
                                    //search timer
                                    SearchTimer -= Time.deltaTime;
                                }
                                else
                                {
                                    if (SearchForTargetToAttack() == false)
                                    {
                                        SearchTimer = SearchReload; //No enemy units found? search again.
                                    }
                                }
                            }
                        }

                    }
                }
                else
                {
                    //if the target went invisible:
                    //Checking whether the target is dead or not (if it's a unit or a building.
                    bool Dead = false;

                    if (TargetUnit)
                    {
                        //if the target went invisible:
                        if (TargetUnit.IsInvisible == true)
                        {
                            //stop attacking it:
                            CancelAttack();
                            return;
                        }
                        else 
                        Dead = TargetUnit.Dead;
                    }
                    else if (TargetBuilding)
                    {
                        if (!CanAttackFaction(TargetBuilding.FactionID))
                        {
                            CancelAttack();
                            return;
                        }
                    }

                    if (Dead == true)
                    {
                        //if the target unit is dead, cancel everything
                        CancelAttack();
                    }
                    else
                    {
                        //If the current unit has a target,
                        if (Vector3.Distance(this.transform.position, AttackTarget.transform.position) > FollowRange && AttackRangeFromCenter == false && TargetUnit != null && WasInTargetRange == true)
                        { //This means that the target has left the follow range of the unit.
                            CancelAttack();
                            //This unit doesn't have a target anymore.
                        }
                        else
                        {
                            if (AttackerType == AttackerTypes.Unit) //if the attacker is a unit
                            {
                                //if destination is reached but the unit is not in the correct range
                                if (Vector3.Distance(LastTargetPos, AttackTarget.transform.position) >= AttackManager.Instance.RangeTypes[RangeTypeID].UpdateMvtDistance)
                                {
                                    UnitMgr.DestinationReached = false; //Destination is not reached
                                    //Update movement by asking the unit to attack again
                                    MvtMgr.LaunchAttackLocal(UnitMgr, AttackTarget.gameObject, MovementManager.AttackModes.Change);
                                }

                                if (UnitMgr.DestinationReached == false && UnitMgr.Moving == false)
                                { //If the unit didn't reach its target and it looks like it's not moving:
                                  //Follow the target:
                                    MvtMgr.LaunchAttackLocal(UnitMgr, AttackTarget.gameObject, MovementManager.AttackModes.Change);
                                }

                                //if the attacker is in the correct range of his target
                                if (UnitMgr.DestinationReached == true)
                                {
                                    if (MoveOnAttack == false && UnitMgr.Moving == true) //if move on attack is disabled and the unit is still moving
                                    {
                                        return; //stop
                                    }
                                }
                                else
                                {
                                    if (MoveOnAttack == true && IsTargetInRange() == false || MoveOnAttack == false) //if we can move on attack but the target is still not in range
                                    {
                                        return; //do not proceed
                                    }
                                }

                            }

                            //reaching this stage means that the attacker is in range of the target and it's all good to go:

                            //attacker in target's range custom event
                            if (WasInTargetRange == false && GameMgr.Events)
                                GameMgr.Events.OnAttackerInRange(this, AttackTarget);

                            WasInTargetRange = true;
                            //Make the attack object look at the target:
                            if (WeaponObj != null)
                            {
                                Vector3 LookAt = AttackTarget.transform.position - WeaponObj.transform.position;
                                //which axis should not be rotated? 
                                if (FreezeRotX == true)
                                    LookAt.x = 0.0f;
                                if (FreezeRotY == true)
                                    LookAt.y = 0.0f;
                                if (FreezeRotZ == true)
                                    LookAt.z = 0.0f;
                                Quaternion TargetRotation = Quaternion.LookRotation(LookAt);
                                if (SmoothRotation == false)
                                { //if the rotation is automatically changed
                                    WeaponObj.transform.rotation = TargetRotation;
                                }
                                else
                                {
                                    //smooth rotation here:
                                    WeaponObj.transform.rotation = Quaternion.Slerp(WeaponObj.transform.rotation, TargetRotation, Time.deltaTime * RotationDamping);
                                }

                            }

                            if (AttackTimer <= 0)
                            { //if the attack timer is ready:
                              //Delay timer here: 
                                if (DelayTimer > 0)
                                {
                                    DelayTimer -= Time.deltaTime;
                                }

                                //Only if the animation is not already playing
                                if (AttackerType == AttackerTypes.Unit && CanPlayAnim == true && UnitMgr.AnimMgr)
                                {
                                    //Playing animation:
                                    if (PlayAnimInDelay == true || (PlayAnimInDelay == false && DelayTimer <= 0.0f && AttackTriggered == true))
                                    {
                                        PlayAttackAnim();
                                    }
                                }

                                //If the attack delay is over, launch the attack
                                if (DelayTimer <= 0.0f && AttackTriggered == true)
                                {
                                    //should the attacker look at the target first? 
                                    if(EnableLOS == true && InLOS == false)
                                    {
                                        //What are we going to use a reference to see if the attacker is actually in line of sight of the target?
                                        //The attacker's main object?
                                        Transform AttackerTransRef = transform;
                                        //Or are we using the weapon's object (if there's one)
                                        if(WeaponObj != null && WeaponObjInLOS == true)
                                        {
                                            AttackerTransRef = WeaponObj.transform;
                                        }

                                        Vector3 ToVector = AttackTarget.transform.position - AttackerTransRef.position;

                                        //Which axis to ignore when checking for LOS?
                                        if(IgnoreLOSX == true)
                                        {
                                            ToVector.x = 0.0f;
                                        }
                                        if (IgnoreLOSY == true)
                                        {
                                            ToVector.y = 0.0f;
                                        }
                                        if (IgnoreLOSZ == true)
                                        {
                                            ToVector.z = 0.0f;
                                        }

                                        //if the angle is below the allowed LOS Angle
                                        if (Vector3.Angle(AttackerTransRef.forward, ToVector) <= LOSAngle)
                                        {
                                            InLOS = true; //we are seeing the target
                                        }
                                    }

                                    //Are we requiring the unit to be in LOS before attacking or not?
                                    if ((InLOS == true && EnableLOS == true) || EnableLOS == false)
                                    {

                                        //Is this a direct attack (no use of attack objects)?
                                        if (DirectAttack == true)
                                        {
                                            LaunchDirectAttack();
                                        }
                                        else
                                        { //If the unit can launch attack objs towards the target unit
                                            if (AttackStep < AttackSources.Length && AttackSources.Length > 0)
                                            { //if we haven't already launched attacks from all sources

                                                if (AttackStepTimer > 0)
                                                {
                                                    AttackStepTimer -= Time.deltaTime;
                                                }
                                                if (AttackStepTimer <= 0)
                                                {
                                                    LaunchAttackObj();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        //a method to laucnh a direct attack
        void LaunchDirectAttack ()
        {
            if (AreaDamage == true)
            {
                //launch area damage and provide all arguments:
                AttackMgr.LaunchAreaDamage(AttackTarget.transform.position, this);
            }
            else
            {
                //Custom event:
                if (GameMgr.Events)
                    GameMgr.Events.OnAttackPerformed(this, AttackTarget);

                if (DealDamage == true) //Can this attack deal damage directly?
                {
                    if (DoT.Enabled == true)
                    {
                        //only for units currently?
                        if (TargetUnit)
                        {
                            ConfigureTargetDoT(TargetUnit, AttackManager.GetDamage(AttackTarget, CustomDamage, UnitDamage));
                        }
                    }
                    else
                    {
                        //if this is no areal damage and no DoT
                        //deal damage to unit/building:
                        if (TargetUnit)
                        {
                            TargetUnit.AddHealth(-AttackManager.GetDamage(AttackTarget, CustomDamage, UnitDamage), this.gameObject);
                            //Spawning the damage effect object:
                            AttackMgr.SpawnEffectObj(TargetUnit.DamageEffect, AttackTarget, AttackTarget.transform.position, 0.0f, true, true);
                            //spawn attack effect object: units only currently
                            AttackMgr.SpawnEffectObj(AttackEffect, AttackTarget, AttackTarget.transform.position, AttackEffectTime, false, true);
                        }
                        else if (TargetBuilding)
                        {
                            TargetBuilding.AddHealth(-AttackManager.GetDamage(AttackTarget, CustomDamage, BuildingDamage), this.gameObject);
                            //Spawning the damage effect object:
                            AttackMgr.SpawnEffectObj(TargetBuilding.DamageEffect, AttackTarget, AttackTarget.transform.position, 0.0f, true, true);
                        }
                    }
                }
            }

            //Play the attack audio:
            AudioManager.PlayAudio(gameObject, AttackSound, false);

            FinishAttack();
        }

        void FinishAttack ()
        {
            AttackTimer = AttackReload;
            CanPlayAnim = true;

            //If this not the basic attack then revert back to the basic attack after done
            if (BasicAttack == false && MultipleAttacksMgr != null)
            {
                //If this feature is actually enabled:
                if (MultipleAttacksMgr.SwitchBackToBasic == true)
                {
                    MultipleAttacksMgr.EnableAttackType(MultipleAttacksMgr.BasicAttackID);
                }
            }

            //Cooldown:
            StartCoolDown();

            //Attack once? cancel attack to prevent source from attacking again
            if (AttackOnce == true)
            {
                CancelAttack();
            }

            ReloadAttackDelay();
        }

        void ReloadAttackDelay()
        {
            //set the attack delay options:
            DelayTimer = DelayTime;
            //do we have an attack trigger option?
            if (UseDelayTrigger == true)
            {
                AttackTriggered = false;
            }
            else
            {
                AttackTriggered = true;
            }
        }

        //a method that launches the attack object (indirect attack):
        public void LaunchAttackObj()
        {
            AttackObject AttackObj = ObjPool.GetEffectObj(EffectObjPool.EffectObjTypes.AttackObj, AttackSources[AttackStep].AttackObj).GetComponent<AttackObject>();
            AttackObj.transform.position = AttackSources[AttackStep].AttackObjSource.transform.position; //Set the attack object's position:
            AttackObj.gameObject.SetActive(true); //Activate the attack object

            AttackObj.DefaultUnitDamage = UnitDamage;
            AttackObj.DefaultBuildingDamage = BuildingDamage;
            AttackObj.CustomDamage = CustomDamage;

            Vector3 TargetPos = AttackTarget.transform.position;

            if (TargetUnit)
            {
                AttackObj.TargetFactionID = TargetUnit.FactionID;
                TargetPos = TargetUnit.PlayerSelection.transform.position;
            }
            else if (TargetBuilding)
            {
                AttackObj.TargetFactionID = TargetBuilding.FactionID;
                TargetPos = TargetBuilding.PlayerSelection.transform.position;
            }

            AttackObj.DamageOnce = AttackSources[AttackStep].DamageOnce;
            AttackObj.DestroyOnDamage = AttackSources[AttackStep].DestroyAttackObjOnDamage;
            AttackObj.DealDamage = DealDamage;

            AttackObj.Source = this;
            AttackObj.SourceFactionID = AttackerFactionID();

            if (AreaDamage == false)
            {
                AttackObj.DidDamage = false;
                AttackObj.DoDamage = !DirectAttack;
                AttackObj.AreaDamage = false;
            }
            else
            {
                AttackObj.DamageOnce = true;
                AttackObj.DidDamage = false;
                AttackObj.AreaDamage = true;
                AttackObj.AttackRanges = AttackRanges;
            }

            //Damage over time:
            AttackObj.DoT = DoT;

            //Attack object movement:
            AttackObj.MvtVector = (TargetPos - AttackSources[AttackStep].AttackObjSource.transform.position) / Vector3.Distance(AttackTarget.transform.position, AttackSources[AttackStep].AttackObjSource.transform.position);
            AttackObj.Speed = AttackSources[AttackStep].AttackObjSpeed;

            //Set the attack obj's rotation so that it looks at the target:
            AttackObj.transform.rotation = Quaternion.LookRotation(TargetPos - AttackObj.transform.position);

            //pass attack effect:
            AttackObj.AttackEffect = AttackEffect;
            AttackObj.AttackEffectTime = AttackEffectTime;

            //Hide the attack object after some time:
            AttackObj.gameObject.GetComponent<EffectObj>().Timer = AttackSources[AttackStep].AttackObjDestroyTime;
            AttackObj.gameObject.GetComponent<EffectObj>().EnableLifeTime = true;

            AttackObj.ShowAttackObjEffect();

            //Attack object delay only if the attack object is supposed to be created on delay.
            AttackObj.DelayTime = (AttackSources[AttackStep].CreateOnDelay == true) ? AttackSources[AttackStep].DelayTime : 0.0f;
            AttackObj.DamageInDelay = AttackSources[AttackStep].DamageInDelay; //do damage while in delay time (if the attack object is created in delay mode)?

            //if there's a delay for the attack object and there's a parent object for delay:
            if (AttackSources[AttackStep].DelayTime > 0.0f && AttackSources[AttackStep].DelayParentObj != null)
            {
                //set delay parent object:
                AttackObj.transform.SetParent(AttackSources[AttackStep].DelayParentObj, true);
            }

            //Play the attack audio:
            AudioManager.PlayAudio(gameObject, AttackSound, false);

            //-----------------------------------------------------------------------------------------------

            //search for the next attack object:
            if (AttackType == AttackTypes.InOrder)
            { //if the attack types is in order
                AttackStep++;
            }

            if (AttackStep >= AttackSources.Length || AttackType == AttackTypes.Random)
            { //end of attack round:
              //Reload the attack timer:
                AttackStep = 0;
                //Only consider the delay time here if the attack object will not be created while the delay is going (if that's the case, the attack object itself will handle the delay).
                AttackStepTimer = (AttackSources[AttackStep].CreateOnDelay == false) ? AttackSources[AttackStep].DelayTime : 0.0f;

                FinishAttack();
            }
            else
            {
                AttackStepTimer = AttackSources[AttackStep].DelayTime;

                //can we reload the attack animation?
                if(AttackSources[AttackStep].ReloadAttackAnim == true)
                {
                    CanPlayAnim = true;
                }
            }
        }

        public void SetAttackTarget(GameObject Obj)
        {
            if (GameManager.MultiplayerGame == false)
            {
                SetAttackTargetLocal(Obj); //attack target settings
            }
            else
            {
                if (GameMgr.IsLocalPlayer(gameObject) == true && AttackerType == AttackerTypes.Building)
                {
                    //send input action to the input manager
                    InputVars NewInputAction = new InputVars();
                    //mode:
                    NewInputAction.SourceMode = (byte)InputSourceMode.Building;

                    NewInputAction.TargetMode = (byte)InputTargetMode.Attack; //initially, this is set to none

                    //source
                    NewInputAction.Source = gameObject;

                    //set the target:
                    NewInputAction.Target = Obj;

                    //sent input
                    InputManager.SendInput(NewInputAction);
                }
            }
        }

        //Set attack target:
        public void SetAttackTargetLocal(GameObject Obj)
        {
            //if the attacker is a unit:
            if (AttackerType == AttackerTypes.Unit)
            {
                UnitMgr.DestinationReached = false; //to make the unit move to the target.
            }

            LastTargetPos = Obj.transform.position; //mark the last position of the target

            InLOS = false; //Initially mark the attacker as not inside the LOS of the target

            //If this is a different target than the last assigned:
            if (Obj != AttackTarget)
            {
                //Allow to play animation:
                CanPlayAnim = true;

                AttackTarget = Obj;
                TargetUnit = AttackTarget.GetComponent<Unit>();
                TargetBuilding = AttackTarget.GetComponent<Building>();

                if (DirectAttack == false)
                {
                    //other settings here:
                    if (AttackType == AttackTypes.Random)
                    { //if the attack type is random
                        AttackStep = Random.Range(0, AttackSources.Length); //pick a random source
                    }
                    else if (AttackType == AttackTypes.InOrder)
                    { //if it's in order
                        AttackStep = 0; //start with the first attack source:
                    }

                    AttackStepTimer = AttackSources[AttackStep].DelayTime;
                }

                WasInTargetRange = false;

                //new target, check to reload damage dealt;
                if (ReloadDamageDealt == true)
                {
                    DamageDealt = 0.0f;
                }

                //Custom event:
                if (GameMgr.Events)
                    GameMgr.Events.OnAttackTargetLocked(this, AttackTarget);
            }
            
            ReloadAttackDelay(); //Reload the attack delay
        }

        public void ResetAttack() //reset the values of the attack:
        {
            AttackStep = 0;
            AttackStepTimer = 0.0f;
            AttackTimer = 0.0f;
            AttackTarget = null;
            TargetUnit = null;
            TargetBuilding = null;
            WasInTargetRange = false;
        }

        //Damage Dealt:
        public void AddDamageDealt(float Value)
        {
            DamageDealt += Value;
        }

        //get the current get damage dealt:
        public float GetDamageDealt()
        {
            return DamageDealt;
        }

        //Set DoT configs:
        public void ConfigureTargetDoT (Unit Target, float DoTDamage)
        {
            //DoT settings:
            Target.DoT = new DoTVars();
            Target.DoT.Enabled = true;
            Target.DoT.Cycle = DoT.Cycle;
            Target.DoT.Duration = DoT.Duration;
            Target.DoT.Infinite = DoT.Infinite;
            Target.DoT.Source = gameObject;
            Target.DoT.Damage = DoTDamage;
        }

        //Cooldown:
        public void StartCoolDown()
        {
            if (EnableCoolDown == true)
            {
                CoolDownTimer = CoolDown;
                InCoolDownMode = true;
            }
        }

        //Attack animation:
        public void PlayAttackAnim()
        {
            UnitMgr.SetAnimState(UnitAnimState.Attacking);

            CanPlayAnim = false;
        }

    }
}