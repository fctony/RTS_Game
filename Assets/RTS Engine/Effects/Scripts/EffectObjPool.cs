using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;

/* Effect Obj Pooling script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class EffectObjPool : MonoBehaviour
    {

        //Make sure there's one instance of this component:
        public static EffectObjPool Instance;

        // Because instantiating and destroying objects uses a lot of memory and we will be spawning and hiding a lot objects during the game.
        // It's better not to destroy them but keep them inactive and re-use when needed.

        //Effect objects types:
        public enum EffectObjTypes {UnitDamageEffect, BuildingDamageEffect, AttackObjEffect, AttackObj};

        //Lists that include all the created (active and inactive) effect objects:
        List<GameObject> UnitDamageEffects = new List<GameObject>();
        List<GameObject> BuildingDamageEffects = new List<GameObject>();
        List<GameObject> AttackObjEffects = new List<GameObject>();
        List<GameObject> AttackObjs = new List<GameObject>();

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(this);
            }
        }

        //This method searches for a hidden effect object with a certain code so that it can be used again.
        public GameObject GetEffectObj(EffectObjTypes Type, EffectObj Prefab)
        {
            Assert.IsTrue(Prefab != null);

            //Determine which list of objects the script is going to search depending on the given type:
            List<GameObject> SearchList = new List<GameObject>();
            switch (Type)
            {
                case EffectObjTypes.AttackObjEffect:
                    SearchList = AttackObjEffects;
                    break;
                case EffectObjTypes.BuildingDamageEffect:
                    SearchList = BuildingDamageEffects;
                    break;
                case EffectObjTypes.UnitDamageEffect:
                    SearchList = UnitDamageEffects;
                    break;
                case EffectObjTypes.AttackObj:
                    SearchList = AttackObjs;
                    break;
                default:
                    Debug.LogError("Effect object type not found!");
                    break;
            }

            GameObject Result = null;
            //Loop through all the spawned objects in the target list:
            if (SearchList.Count > 0)
            {
                int i = 0;

                string Code = Prefab.Code; //save the prefab's code here

                while (Result == null && i < SearchList.Count)
                {
                    if (SearchList[i] != null)
                    {
                        //If the current object's code mathes the one we're looking for:
                        if (SearchList[i].gameObject.GetComponent<EffectObj>().Code == Code)
                        {
                            //We can re-use non active objects, so we'll check for that as well:
                            if (SearchList[i].gameObject.activeInHierarchy == false)
                            {
                                //This matches all what we're looking for so make it the result;
                                Result = SearchList[i];
                            }
                        }
                        i++;
                    }
                    else
                    {
                        SearchList.RemoveAt(i); //if there's nothing here, remove the empty list field
                    }
                }


            }

            //if we still haven't found the free effect object we're looking for:
            //Create one:
            if (Result == null)
            {
                Result = Instantiate(Prefab.gameObject, Vector3.zero, Prefab.transform.rotation);
                //add it to the list:
                SearchList.Add(Result);
            }


            //return the result:
            return Result;
        }
    }
}