using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    public class AttackManager : MonoBehaviour
    {
        public static AttackManager Instance;

        //Range type:

        [System.Serializable]
        public class UnitRanges
        {
            public string Code;
            public float UnitStoppingDistance; //stopping distance for target unit
            public float BuildingStoppingDistance; //stopping distance for target buildings
            public float MoveOnAttackOffset = 4.0f; //when move on attack is enabled for attackers, the range of the move attack would be...
            //...one of the stopping distance values above + the offset value provided in this field
            public float UpdateMvtDistance = 2.0f; //whenever the attack target moves this distance when it has been locked as a target, attacker mvt will be recalculated
        }
        public UnitRanges[] RangeTypes;

        public int GetRangeTypeID(string Code)
        {
            for(int i = 0; i < RangeTypes.Length; i++)
            {
                if(RangeTypes[i].Code == Code)
                {
                    return i;
                }
            }

            return -1;
        }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        //Attack Units/Buildings:

        //checks whether the attack source can attack the target object
        public bool CanAttackTarget(Attack Source, GameObject Target)
        {
            //if the source can attack all types then instantly return true
            if (Source.AttackAllTypes == true)
                return true;

            string Code = ""; //get the target's code
            if (Target.GetComponent<Unit>())
            { //if it's a unit
              //if the source can't attack units:
                if (Source.AttackUnits == false)
                    return false;
                Code = Target.GetComponent<Unit>().Code;
            }
            else if (Target.GetComponent<Building>())
            { //if it's a 
                if (Source.AttackBuildings == false)
                    return false;
                Code = Target.GetComponent<Building>().Code;
            }

            //if the target's code is on the list, then no the source can't attack it

            if (Source.CodesList.Contains(Code) == Source.AttackOnlyInList)
                return true;
            else
                return false;
        }

        //a function that determines how much damage will the attacker give to a unit/building
        public static float GetDamage(GameObject Victim, Attack.DamageVars[] DamageList, float DefaultDamage)
        {
            if (DamageList.Length > 0)
            { //if there's custom damage
                if (Victim.gameObject.GetComponent<Unit>())
                { //if the victim is a unit
                  //search for the right damage
                    return GetDamageBasedOnCode(Victim.gameObject.GetComponent<Unit>().Code, DamageList, DefaultDamage);
                }
                if (Victim.gameObject.GetComponent<Building>())
                { //if the victim is a building
                  //search for the right damage
                    return GetDamageBasedOnCode(Victim.gameObject.GetComponent<Building>().Code, DamageList, DefaultDamage);
                }
            }

            return DefaultDamage;
        }

        //based a code (from a unit/building) get the damage:
        public static float GetDamageBasedOnCode(string Code, Attack.DamageVars[] DamageList, float DefaultDamage)
        {
            for (int i = 0; i < DamageList.Length; i++)
            { //go through the list
                if (DamageList[i].Code == Code)
                { //if the code matches
                    return DamageList[i].Damage; //give the damage.
                }
            }

            return DefaultDamage;
        }

        //Handle area damage attacks here:
        public void LaunchAreaDamage(Vector3 AreaCenter, Attack Source)
        {
            if (Source.AttackRanges.Length > 0) //if the area ranges have been defined
            {
                //try to get all objects in the chosen range (biggest range must always be the last one)
                Collider[] ObjsInRange = Physics.OverlapSphere(AreaCenter, Source.AttackRanges[Source.AttackRanges.Length - 1].Range);
                int i = 0;
                //go through all of them
                while (i < ObjsInRange.Length)
                {
                    //if we find a selection obj
                    if (ObjsInRange[i].gameObject.GetComponent<SelectionObj>())
                    {
                        //it can be one for a unit or a building
                        Unit Unit = ObjsInRange[i].gameObject.GetComponent<SelectionObj>().MainObj.GetComponent<Unit>();
                        Building Building = ObjsInRange[i].gameObject.GetComponent<SelectionObj>().MainObj.GetComponent<Building>();

                        //if it's a unit
                        if (Unit)
                        {
                            //and it's in the same faction as the source 
                            if (Unit.FactionID == Source.AttackerFactionID() || !CanAttackTarget(Source, Unit.gameObject))
                                Unit = null; //cancel 
                        }
                        //if it's a building
                        if (Building)
                        {
                            //and it's in the same faction as the source
                            if (Building.FactionID == Source.AttackerFactionID() || !CanAttackTarget(Source, Building.gameObject))
                                Building = null; //cancel
                        }

                        //if we have a unit or a building
                        if (Unit != null || Building != null)
                        {
                            //Custom event:
                            if (GameManager.Instance.Events)
                                GameManager.Instance.Events.OnAttackPerformed(Source, ObjsInRange[i].gameObject.GetComponent<SelectionObj>().MainObj);

                            //Only if we can deal damage:
                            if (Source.DealDamage)
                            {
                                //check the distance of the object to the area center
                                float Distance = Vector3.Distance(ObjsInRange[i].transform.position, AreaCenter);
                                int j = 0;
                                bool Found = false;
                                //go through all ranges
                                while (j < Source.AttackRanges.Length && Found == false)
                                {
                                    //if we find a suitable range
                                    if (Distance < Source.AttackRanges[j].Range)
                                    {
                                        //apply damage:
                                        Found = true;
                                        if (Unit)
                                        {
                                            if (Source.DoT.Enabled)
                                            {
                                                //DoT settings:
                                                Source.ConfigureTargetDoT(Unit, Source.AttackRanges[j].UnitDamage);

                                                //also there are still no effect objects for DoT, make their own?
                                            }
                                            else
                                            {
                                                Unit.AddHealth(-Source.AttackRanges[j].UnitDamage, Source.gameObject);
                                                //Spawning the damage effect object:
                                                SpawnEffectObj(Unit.DamageEffect, ObjsInRange[i].gameObject, ObjsInRange[i].gameObject.transform.position, 0.0f, true, true);
                                                //currently there's attack effect objects for units only:
                                                SpawnEffectObj(Source.AttackEffect, ObjsInRange[i].gameObject, ObjsInRange[i].gameObject.transform.position, Source.AttackEffectTime, false, true);
                                            }
                                        }
                                        else if (Building)
                                        {
                                            //no DoT for buildings, just normal attacks
                                            Building.AddHealth(-Source.AttackRanges[j].BuildingDamage, Source.gameObject);
                                            //Spawning the damage effect object:
                                            SpawnEffectObj(Building.DamageEffect, ObjsInRange[i].gameObject, ObjsInRange[i].gameObject.transform.position, 0.0f, true, true);
                                        }

                                    }
                                    j++;
                                }
                            }
                        }
                    }
                    i++;
                }
            }
        }

        //Spawn attack effect object and damage effect objects here:
        public void SpawnEffectObj(EffectObj Prefab, GameObject Target, Vector3 SpawnPostion, float LifeTime, bool AutoLifeTime, bool ChildObj)
        {
            //First check if the effect object is valid
            if (Prefab != null)
            {
                //if the effect object has an auto life time then assign it:
                if (AutoLifeTime == true)
                    LifeTime = Prefab.LifeTime;

                //get the effect type (unit or building):
                EffectObjPool.EffectObjTypes EffectType;
                if (Target.GetComponent<Unit>())
                {
                    EffectType = EffectObjPool.EffectObjTypes.UnitDamageEffect;
                }
                else
                {
                    EffectType = EffectObjPool.EffectObjTypes.BuildingDamageEffect;
                }

                //get the attack effect (either create it or get one tht is inactive):
                GameObject Effect = EffectObjPool.Instance.GetEffectObj(EffectType, Prefab);

                //settings:
                Effect.SetActive(true);
                //set the position and rotation of the damage object:
                Effect.transform.position = SpawnPostion;
                Effect.transform.rotation = Prefab.gameObject.transform.rotation;
                //if we need the effect object to be a child obj of the affected object:
                if (ChildObj == true)
                {
                    //set the attack target as the parent of the damage effect
                    Effect.transform.SetParent(Target.transform, true);
                }
                //default life time:
                Effect.GetComponent<EffectObj>().Timer = LifeTime;
            }
        }
    }

}