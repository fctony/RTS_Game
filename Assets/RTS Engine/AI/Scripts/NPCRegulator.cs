using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* NPC Regulator script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public abstract class NPCRegulator<T> : ScriptableObject
    {
        //holds a prefab list of the items that will be regulated by this instance:
        //the prefabs MUST share the same code, each time, a random one will be chosen.
        public List<T> prefabs = new List<T>();
        //the item code that the above item prefabs.
        protected string code;

        //Buildings only: both minimum amount and maximum amount are per faction center building (a center building is a building that has the border component). 
        //minimum amount of item type (NPC would try to have this minimum amount of the item type created urgently).
        public IntRange minAmountRange = new IntRange(1, 2);
        protected int minAmount;

        public int GetMinAmount() { return minAmount; }
        public void IncMinAmount() { minAmount++; }

        //maximum amount of item type (can not have more than the amount below).
        public IntRange maxAmountRange = new IntRange(10, 15);
        protected int maxAmount;

        public int GetMaxAmount() { return maxAmount; }

        public int maxPendingAmount = 1; //the amount of items that can be pending creation at the same time.
        protected int pendingAmount;

        //when another NPC component (excluding the main component that can create it) requests the creation of this item type, can it be created?
        public bool createOnDemand = true;

        //current amount will be stored here:
        protected int amount = 0;
        protected List<T> currentInstances = new List<T>(); //the list of spawned items from the defined type(s) in this component.
        public List<T> GetCurrentInstances()
        {
            return currentInstances;
        }

        public FloatRange startCreatingAfter = new FloatRange(10.0f, 15.0f); //delay time in seconds after which this component will start creating items.

        public FloatRange spawnReloadRange = new FloatRange(15.0f, 20.0f); //time needed between spawning two consecutive items.

        //faction manager that this regulator belongs to
        protected FactionManager factionMgr;
        protected NPCManager npcMgr;

        protected virtual void InitItem (NPCManager npcMgr)
        {
            this.npcMgr = npcMgr;
            factionMgr = npcMgr.FactionMgr;

            //if there are no unit prefabs:
            if (prefabs.Count < 0)
            {
                //stop here and destroy this instance.
                Destroy(this);
                return;
            }

            //pick the rest random settings from the given info.
            maxAmount = maxAmountRange.getRandomValue();
            minAmount = minAmountRange.getRandomValue();

            amount = 0;
        }

        //remove an iem from the amount:
        public virtual void RemoveItem(T item)
        {
            amount--; //decrease the amount of current items.
            //remove the item from the current items list:
            if (currentInstances.Remove(item) == false) //if the item wasn't on the list to begin with
                pendingAmount--; //decrease pending amount
            
        }

        //determine if we have reached the maximum amount or not on this regulator or if the item limits have been reached
        public bool HasReachedMaxAmount()
        {
            return amount >= maxAmount || factionMgr.HasReachedLimit(code) || pendingAmount >= maxPendingAmount;
        }

        //determine if we have reached the minimum amount or not
        public bool HasReachedMinAmount()
        {
            return amount >= minAmount;
        }
    }
}
