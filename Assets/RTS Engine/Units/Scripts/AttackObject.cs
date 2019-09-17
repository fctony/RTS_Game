using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* Attack Object script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class AttackObject : MonoBehaviour
    {

        [HideInInspector]
        public Attack Source; //From the attack object was launched.
        GameObject SourceObj;
        [HideInInspector]
        public Vector3 MvtVector; //The attack object
        [HideInInspector]
        public float Speed = 10.0f; //attack object's speed:

        //Attack damage:
        [HideInInspector]
        public Attack.DamageVars[] CustomDamage; //if the target unit/building code is in the list then it will be given the matching damage, if not then the default damage
        [HideInInspector]
        public float DefaultUnitDamage = 10.0f; //damage points when this unit attacks another unit.
        [HideInInspector]
        public float DefaultBuildingDamage = 10.0f; //damage points when this unit attacks a building.

        [HideInInspector]
        public bool DamageInDelay = true; //Do damage while in delay mode?
        [HideInInspector]
        public bool DamageOnce = true; //do damage once? 
        [HideInInspector]
        public bool DoDamage = true; //do damage at all? 
        [HideInInspector]
        public bool DestroyOnDamage; //destroy on first given damage?
        [HideInInspector]
        public bool DealDamage = true;

        //Delay:
        [HideInInspector]
        public float DelayTime = 0.0f; //how long will the delay last.

        [HideInInspector]
        public int TargetFactionID; //target faction to attack
        [HideInInspector]
        public int SourceFactionID; //the unit's faction that this object came from:

        [HideInInspector]
        public bool DidDamage = false;
        [HideInInspector]
        public bool AreaDamage = false;

        //Area damage:
        [HideInInspector]
        public Attack.AttackRangesVars[] AttackRanges;
        [HideInInspector]
        public List<string> AttackExceptionList = new List<string>();

        //DoT:
        [HideInInspector]
        public Attack.DoTVars DoT;

        public EffectObj SpawnEffect; //the spawn effect that is instantied when this object is spawned


        //Attack target effect:
        [HideInInspector]
        public EffectObj AttackEffect;
        [HideInInspector]
        public float AttackEffectTime;

        //Scripts:
        EffectObjPool ObjPool;

        // Use this for initialization
        void Start()
        {
            DidDamage = false;

            //Settings in order to make OnCollisionEnter work:
            GetComponent<Collider>().isTrigger = false;
            GetComponent<Rigidbody>().isKinematic = false;
            GetComponent<Rigidbody>().useGravity = false;

            ObjPool = EffectObjPool.Instance;

            SourceObj = Source.gameObject; //store the source game object here in case it's destroyed before the attack object hits the enemy.
        }

        // Update is called once per frame
        void Update()
        {
            //if there's a delay do not move the attack object
            if(DelayTime > 0.0f)
            {
                DelayTime -= Time.deltaTime; //delay timer
                if(DelayTime <= 0.0f) //when done
                {
                    //free attack object:
                    transform.SetParent(null, true);
                }
                return;
            }

            //move the attack object towards its target:
            Vector3 moveDir = MvtVector.normalized;
            transform.position += moveDir * Speed * Time.deltaTime;

            //if we already done damage and this object gets destroyed after damage:
            if (DestroyOnDamage == true && DidDamage == true)
            {
                //actually hide the object and don't destroy it, hiding it will add it automatically to the pool allowing us to re-use it.
                gameObject.SetActive(false);
            }
        }


        //Show the attack object's spawn effect:
        public void ShowAttackObjEffect()
        {
            if (SpawnEffect != null && ObjPool != null)
            { //if we have a spawn effect object and there's an effect object pooling manager:
              //Get the spawn effect obj
                GameObject SpawnEffectObj = ObjPool.GetEffectObj(EffectObjPool.EffectObjTypes.AttackObjEffect, SpawnEffect);

                //settings for the spawn effect object:
                SpawnEffectObj.SetActive(true); //activate it
                SpawnEffectObj.transform.position = transform.position; //set its position
                SpawnEffectObj.transform.rotation = SpawnEffect.transform.rotation; //its rotation as well

                //and the life time (default):
                SpawnEffectObj.GetComponent<EffectObj>().Timer = SpawnEffectObj.GetComponent<EffectObj>().LifeTime;

            }
        }

        //Attack object collision effect:
        void OnTriggerEnter(Collider other)
        {
            //If the attack object is still in delay time then don't proceed:
            if (DelayTime > 0.0f && DamageInDelay == false)
            {
                return;
            }

            if ((DidDamage == false || DamageOnce == false) && DoDamage == true)
            { //Make sure that the attack obj either didn't do damage when the attack object is allowed to do damage once or if it can do damage multiple times.
                SelectionObj HitObj = other.gameObject.GetComponent<SelectionObj>();
                if (HitObj != null)
                {
                    Unit HitUnit = HitObj.MainObj.GetComponent<Unit>();
                    Building HitBuilding = HitObj.MainObj.GetComponent<Building>();

                    //If the damaged object is a unit:
                    if (HitUnit)
                    {
                        //Check if the unit belongs to the faction that this attack obj is targeted to and if the unit is actually not dead yet:
                        if (HitUnit.FactionID == TargetFactionID && HitUnit.Dead == false)
                        {
                            if (other != null)
                            {
                                DidDamage = true; //Inform the script that the damage has been done
                                if (AreaDamage)
                                {
                                    AttackManager.Instance.LaunchAreaDamage(transform.position, Source);
                                }
                                else
                                {
                                    //Custom event:
                                    if (GameManager.Instance.Events)
                                        GameManager.Instance.Events.OnAttackPerformed(Source, HitUnit.gameObject);

                                    if (DealDamage == true)
                                    {
                                        if (DoT.Enabled) //if it's DoT
                                        {
                                            //DoT settings:
                                            Source.ConfigureTargetDoT(HitUnit, AttackManager.GetDamage(HitUnit.gameObject, CustomDamage, DefaultUnitDamage));
                                        }
                                        else
                                        {
                                            //Remove health points from the unit:
                                            HitUnit.AddHealth(-AttackManager.GetDamage(HitUnit.gameObject, CustomDamage, DefaultUnitDamage), SourceObj);
                                            //Attack effect:
                                            AttackManager.Instance.SpawnEffectObj(AttackEffect, other.gameObject, other.transform.position, AttackEffectTime, false, true);
                                            //Spawning the damage effect object:
                                            AttackManager.Instance.SpawnEffectObj(HitUnit.DamageEffect, other.gameObject, other.transform.position, 0.0f, true, true);
                                        }
                                    }

                                }
                            }
                        }
                    }
                    //If the attack obj hit a building:
                    if (HitBuilding)
                    {
                        //Check if the building belongs to the faction that this attack obj is targeted to and if the building still has health:
                        if (HitBuilding.FactionID == TargetFactionID && HitBuilding.Health >= 0)
                        {
                            if (other != null)
                            {
                                DidDamage = true; //Inform the script that the damage has been done

                                if (AreaDamage)
                                {
                                    AttackManager.Instance.LaunchAreaDamage(transform.position, Source);
                                }
                                else
                                {
                                    //Custom event:
                                    if (GameManager.Instance.Events)
                                        GameManager.Instance.Events.OnAttackPerformed(Source, HitBuilding.gameObject);

                                    if (DealDamage == true) //only if we can deal damage directly
                                    {
                                        //Remove health points from the unit:
                                        HitBuilding.AddHealth(-AttackManager.GetDamage(HitBuilding.gameObject, CustomDamage, DefaultBuildingDamage), SourceObj);
                                        //Attack effect:
                                        AttackManager.Instance.SpawnEffectObj(AttackEffect, other.gameObject, other.transform.position, AttackEffectTime, false, true);
                                        //Spawning the damage effect object:
                                        AttackManager.Instance.SpawnEffectObj(HitBuilding.DamageEffect, other.gameObject, other.transform.position, 0.0f, true, true);
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