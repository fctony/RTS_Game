using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* NPC Building Placer script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class NPCBuildingPlacer : NPCComponent
    {
        //everything from the building object & placement info will be held in his structure and will be used to place the building
        public struct PendingBuilding
        {
            public Building buildingPrefab; //prefab of the building that is being placed
            public Building buildingInstance; //the actual building that will be placed.
            public Vector3 buildAroundPos; //building will be placed around this position.
            public Building buildingCenter; //the building center that the building instance will belong to.
            public float buildAroundDistance; //how close does the building is to its center?
        }
        private List<PendingBuilding> pendingBuildings = new List<PendingBuilding>(); //list that holds all pending building infos.

        //placement settings:
        public FloatRange placementDelayRange = new FloatRange(7.0f, 20.0f); //actual placement will be only considered after this time.
        float placementDelay;
        public float rotationSpeed = 50.0f; //how fast will the building rotate around its build around position
        public float moveTimer = 10.0f; //whenever this timer is through, building will be moved away from build around position but keeps rotating
        float timer;
        public float timerInc = 2.0f; //this will be added to the move timer each time the building moves.
        int incVal = 0;
        public float moveDistance = 1.0f; //this the distance that the building will move at.

        ResourceManager resourceMgr;

        void Start()
        {
            resourceMgr = GameManager.Instance.ResourceMgr;
        }

        //a method that other NPC components use to request to place a building.
        public void OnBuildingPlacementRequest (Building buildingPrefab, GameObject buildAround, Building buildingCenter, float buildAroundDistance)
        {
            //if the building center or the build around object hasn't been specified:
            if(buildAround == null || buildingCenter == null)
            {
                Debug.LogError("Build Around object or Building Center hasn't been specified in the Building Placement Request!");
                return;
            }

            //take resources to place building.
            resourceMgr.TakeResources(buildingPrefab.BuildingResources, factionMgr.FactionID);

            //pick the building's spawn pos:
            Vector3 buildAroundPos = new Vector3(buildAround.transform.position.x, GameManager.Instance.TerrainMgr.SampleHeight(buildAround.transform.position) + BuildingPlacement.instance.BuildingYOffset, buildAround.transform.position.z);
            Vector3 buildingSpawnPos = buildAroundPos;
            buildingSpawnPos.x += buildAroundDistance;

            //create new instance of building and add it to the pending buildings list:
            PendingBuilding newPendingBuilding = new PendingBuilding
            {
                buildingPrefab = buildingPrefab,
                buildingInstance = Instantiate(buildingPrefab.gameObject, buildingSpawnPos, buildingPrefab.transform.rotation).GetComponent<Building>(),
                buildAroundPos = buildAroundPos,
                buildingCenter = buildingCenter,
                buildAroundDistance = buildAroundDistance
            };
            //initialize the building instance for placement:
            newPendingBuilding.buildingInstance.InitPlacementInstance(factionMgr.FactionID, buildingCenter.BorderMgr);

            //we need to hide the building initially, when its turn comes to be placed, appropriate settings will be applied.
            newPendingBuilding.buildingInstance.gameObject.SetActive(false);
            //Hide the building's model:
            newPendingBuilding.buildingInstance.BuildingModel.SetActive(false);
            newPendingBuilding.buildingInstance.BuildingPlane.SetActive(false); //hide the building's selection texture

            //Call the start building placement custom event:
            if (GameManager.Instance.Events)
                GameManager.Instance.Events.OnBuildingStartPlacement(newPendingBuilding.buildingInstance);

            //add the new pending building to the list:
            pendingBuildings.Add(newPendingBuilding);

            if (pendingBuildings.Count == 1) //if the queue was empty before adding the new pending building
                StartPlacingNextBuilding(); //immediately start placing it.
        }

        //place buildings from the pending building list: First In, First Out
        void StartPlacingNextBuilding ()
        {
            //if there's no pending building:
            if (pendingBuildings.Count == 0)
                return; //stop.

            //simply activate the first pending building in the list:
            pendingBuildings[0].buildingInstance.gameObject.SetActive(true);

            //reset building rotation/movement timer:
            timer = -1; //this will move the building from its initial position in the beginning of the placement process.
            incVal = 0;
            placementDelay = placementDelayRange.getRandomValue(); //start the placement delay timer.
        }

        void Update ()
        {
            if(pendingBuildings.Count > 0) //if that are pending buildings to be placed:
            {
                float centerDistance = Vector3.Distance(pendingBuildings[0].buildingInstance.transform.position, pendingBuildings[0].buildingCenter.transform.position); 
                //if building center of the current pending building is destroyed while building is getting placed:
                //or if the building is too far away or too close from the center
                if (pendingBuildings[0].buildingCenter == null || centerDistance > pendingBuildings[0].buildingCenter.BorderMgr.Size)
                {
                    StopPlacingBuilding(); //Stop placing building.
                    return;
                }

                //placement delay timer:
                if (placementDelay > 0)
                {
                    placementDelay -= Time.deltaTime;
                }
                else //if the placement delay is through, NPC faction is now allowed to place faction:
                {
                    //Check if the building is in a valid position or not:
                    pendingBuildings[0].buildingInstance.CheckBuildingPos();

                    //can we place the building:
                    if (pendingBuildings[0].buildingInstance.CanPlace == true)
                    {
                        PlaceBuilding();
                        return;
                    }
                }

                //building movement timer:
                if (timer > 0)
                {
                    timer -= Time.deltaTime;
                }
                else
                {
                    //reset timer:
                    timer = moveTimer + (timerInc * incVal);
                    incVal++;

                    //move building away from build around pos.
                    Vector3 mvtDir = (pendingBuildings[0].buildingInstance.transform.position - pendingBuildings[0].buildAroundPos).normalized;
                    mvtDir.y = 0.0f;
                    if (mvtDir == Vector3.zero)
                    {
                        mvtDir = new Vector3(1.0f, 0.0f, 0.0f);
                    }
                    pendingBuildings[0].buildingInstance.transform.position += mvtDir * moveDistance;
                }

                //move the building around its build around position:
                Quaternion buildingRotation = pendingBuildings[0].buildingInstance.transform.rotation; //save building rotation
                //this will move the building around the build around pos which what we want but it will also affect the build rotation..
                pendingBuildings[0].buildingInstance.transform.RotateAround(pendingBuildings[0].buildAroundPos, Vector3.up, rotationSpeed * Time.deltaTime);
                pendingBuildings[0].buildingInstance.transform.rotation = buildingRotation; //therefore we'll reset it each time.
            }
        }

        //method that places a building.
        void PlaceBuilding ()
        {
            //place the first building in the pending buildings list:

            //destroy the building instance that was supposed to be placed:
            Destroy(pendingBuildings[0].buildingInstance.gameObject);

            //ask the building manager to create a new placed building:
            BuildingManager.CreatePlacedInstance(pendingBuildings[0].buildingPrefab, pendingBuildings[0].buildingInstance.transform.position, pendingBuildings[0].buildingCenter.BorderMgr, factionMgr.FactionID);

            //remove the first item in pending buildings list:
            pendingBuildings.RemoveAt(0);

            StartPlacingNextBuilding(); //start placing next building
        }

        //a method that stops placing a building
        void StopPlacingBuilding ()
        {
            //Call the stop building placement custom event:
            if (GameManager.Instance.Events)
                GameManager.Instance.Events.OnBuildingStopPlacement(pendingBuildings[0].buildingInstance);

            //Give back resources:
            resourceMgr.GiveBackResources(pendingBuildings[0].buildingInstance.BuildingResources, factionMgr.FactionID);

            //destroy the building instance that was supposed to be placed:
            Destroy(pendingBuildings[0].buildingInstance);

            //remove the first item in pending buildings list:
            pendingBuildings.RemoveAt(0);

            StartPlacingNextBuilding(); //start placing next building.
        }
    }
}
