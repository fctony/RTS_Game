using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* NPC Task Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public enum NPCTaskTypes {constructBuilding, collectResource};

    public class NPCTaskManager : NPCComponent
    {
        public int queuesAmount = 5; //the amount of different priority queues available.
        
        public class NPCTaskInfo
        {
            public NPCTaskTypes taskType; //the task type

            //attributes for different task types:

            //construct building:
            public Building refBuilding;

            //collect reosurce:
            public Resource refResource;
        }

        public class NPCQueueInfo
        {
            public List<NPCTaskInfo> tasks = new List<NPCTaskInfo>(); //the tasks that belong to this queue.
        }

        private NPCQueueInfo[] queues = new NPCQueueInfo[0];

        //moving between different queues settings:
        public float clockLength = 1.0f;
        public int promotionClockAmount = 10; //each 10 clock signals, tasks will be moved one queue further.
        private int currentClockAmount; //counts the clock amount until the promotion amount is hit.

        private void Start()
        {
            //queues amount must be at least 1:
            if (queuesAmount < 1)
            {
                Debug.LogError("[NPC Task Manager]: Queues amount must be at least 1!");
                queuesAmount = 1;
            }

            //create the queues:
            queues = new NPCQueueInfo[queuesAmount];
            for(int i = 0; i < queuesAmount; i++)
            {
                queues[i] = new NPCQueueInfo(); //initialize the queue.
                queues[i].tasks = new List<NPCTaskInfo>(); //initiliaze the tasks list
            }

            currentClockAmount = 0;
            //On Clock Update is called each clock length.
            InvokeRepeating("OnClockUpdate", 0.0f, clockLength);
        }

        void OnClockUpdate ()
        {
            currentClockAmount++; //increment current clock amount.
            
            if(currentClockAmount >= promotionClockAmount) //if the current clock amount surpasses the promotion clock amount
            {
                //reset current clock amount:
                currentClockAmount = 0;

                //if there's more than one queue:
                if (queues.Length > 1)
                {
                    //go through the queues (excluding the first one):
                    for (int i = 1; i < queues.Length; i++)
                    {
                        //move tasks to +1 higher priority level queues:
                        queues[i - 1].tasks.AddRange(queues[i].tasks);
                        //free tasks in current queue priority level:
                        queues[i].tasks.Clear();
                    }
                }
            }

            //if a task is in queue 0 -> this means that task must be executed ASAP:
            //go through the queue 0 tasks:
            for (int i = 0; i < queues[0].tasks.Count; i++)
            {
                NPCTaskInfo task = queues[0].tasks[i];

                //task type?
                switch (task.taskType)
                {
                    case NPCTaskTypes.constructBuilding:
                        //construct a building?
                        //making sure that the building is still under construction
                        if (npcMgr.buildingConstructor_NPC.IsBuildingUnderConstruction(task.refBuilding) == true)
                        {
                            //request the building constructor and force it to construct building.
                            npcMgr.buildingConstructor_NPC.OnBuildingConstructionRequest(task.refBuilding, false, true);
                            i++;
                        }
                        else
                        {
                            //remove task:
                            queues[0].tasks.RemoveAt(i);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        //add a new task:
        public void AddTask (NPCTaskTypes taskType, GameObject refObject, int queueID)
        {
            //if an invalid queue has been given
            if(queueID >= queues.Length)
            {
                //pick the last available queue:
                queueID = queues.Length - 1;
            }

            switch (taskType)
            {
                case NPCTaskTypes.constructBuilding:
                    //construct a building?
                    //add task and ref building:
                    queues[queueID].tasks.Add(new NPCTaskInfo { taskType = taskType, refBuilding = refObject.GetComponent<Building>() });
                    break;
                default:
                    break;
            }
        }
    }
}
