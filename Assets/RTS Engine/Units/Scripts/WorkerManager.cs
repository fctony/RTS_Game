using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Worker Manager: script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class WorkerManager : MonoBehaviour {
        
        [System.Serializable]
        public class WorkerPosVars
        {
            public Transform Pos; //a transform from which we get the position the unit will be collecting resources/constructing a building
            [HideInInspector]
            public Unit CurrentUnit; //the Unit that is occupying this working position
        }
        public WorkerPosVars[] WorkerPositions; //all possible working positions to gather resources/construct a building are stored here.

        [HideInInspector]
        public int CurrentWorkers = 0; //the current amount of workers.

        //the main components that the worker manager handles:
        Building BuildingMgr;
        Resource ResourceMgr;

        void Awake ()
        {
            CurrentWorkers = 0;

            BuildingMgr = gameObject.GetComponent<Building>();
            ResourceMgr = gameObject.GetComponent<Resource>();
        }

        public Vector3 AddWorker(Unit Worker)
        {
            int ID = -1; //will hold the ID of the nearest working position to the unit
            float Distance = 0.0f; //holds the distance between the working position and the unit

            //go through all worker positions and look for a free one
            for (int i = 0; i < WorkerPositions.Length; i++)
            {
                if (WorkerPositions[i].CurrentUnit == null) //if there's no unit occupying this
                {
                    //if there's no worker position then return this ID instantely 
                    if (WorkerPositions[i].Pos == null)
                    {
                        ID = i;
                        break;
                    }
                    else
                    {
                        if (ID == -1) //if we haven't found a valid working pos yet
                        {
                            ID = i; //assign this one
                            Distance = Vector3.Distance(Worker.transform.position, WorkerPositions[i].Pos.position); //register the distance
                        }
                        else if (Vector3.Distance(Worker.transform.position, WorkerPositions[i].Pos.position) < Distance) //if this is closer to the unit than the saved working pos
                        {
                            ID = i; //assign this one
                            Distance = Vector3.Distance(Worker.transform.position, WorkerPositions[i].Pos.position); //register the distance
                        }
                    }
                }
            }

            if(ID != -1) //if we found a valid working pos for the unit
            {
                //assign the unit here
                WorkerPositions[ID].CurrentUnit = Worker;
                CurrentWorkers++;
                Worker.LastWorkerPosID = ID;
                //if we're using legacy mvt then return the object's position as the result, if not return the registered pos
                return (WorkerPositions[ID].Pos == null) ? transform.position : WorkerPositions[ID].Pos.position;
            }
            else //no valid working pos was found
            {
                //Cancel the unit movement and cancel building & resource gathering
                Worker.StopMvt();
                if(BuildingMgr)
                    Worker.CancelBuilding();
                if(ResourceMgr)
                    Worker.CancelCollecting();

                return Vector3.zero;
            }
        }

        //a method to remove one worker
        public void RemoveWorker (Unit Worker)
        {
            //go through all worker positions and look for the worker
            for (int i = 0; i < WorkerPositions.Length; i++)
            {
                if (WorkerPositions[i].CurrentUnit == Worker) //if this is the unit we're looking for
                {
                    WorkerPositions[i].CurrentUnit = null;
                    CurrentWorkers--;
                }
            }
        }

        //method that gets a worker from the list
        public Unit GetWorker ()
        {
            //go through all worker positions and look for the first worker
            for (int i = 0; i < WorkerPositions.Length; i++)
            {
                if (WorkerPositions[i].CurrentUnit != null) //if this is a valid unit
                {
                    return WorkerPositions[i].CurrentUnit;
                }
            }

            return null;
        }

        //get the work position using it ID:
        public Vector3 GetWorkerPos (int ID)
        {
            if(ID >= 0 && ID < WorkerPositions.Length)
            {
                //if there's a valid worker position with the given ID return it back, if not then give the resource's position
                return (WorkerPositions[ID].Pos != null) ? WorkerPositions[ID].Pos.position : transform.position;
            }
            else
            {
                return transform.position;
            }
        }
    }
}
