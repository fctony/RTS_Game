using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager instance = null;

        void Awake ()
        {
            //only one building manager component in the map
            if (instance == null)
                instance = this;
            else if (instance != this)
                Destroy(this);
        }

        //creates an instance of a building that is instantly placed:
        public static Building CreatePlacedInstance(Building buildingPrefab, Vector3 placementPosition, Border buildingCenter, int factionID, bool placedByDefault = false)
        {
            Building buildingInstance = Instantiate(buildingPrefab.gameObject, placementPosition, buildingPrefab.transform.rotation).GetComponent<Building>(); //create instance
            buildingInstance.CurrentCenter = buildingCenter; //set building cenetr.

            if (placedByDefault == false) //if it's this placed by default
                buildingInstance.PlaceBuilding(factionID);
            else
            {
                buildingInstance.FactionID = factionID;
                buildingInstance.PlacedByDefault = true;
            }

            return buildingInstance;
        }

        //filter a building list depending on a certain code
        public static List<Building> FilterBuildingList(List<Building> buildingList, string code)
        {
            //result list:
            List<Building> filteredBuildingList = new List<Building>();
            //go through the input building list:
            foreach (Building b in buildingList)
            {
                if (b.Code == code) //if it has the code we need
                    filteredBuildingList.Add(b); //add it
            }

            return filteredBuildingList;
        }

        //get the closest building of a certain type out of a list to a given position
        public static Building GetClosestBuilding (Vector3 pos, List<string> codes, List<Building> buildings)
        {
            Building resultBuilding = null;
            float lastDistance = 0;

            //go through the buildings to search
            foreach(Building b in buildings)
            {
                //if the building has a valid code:
                if(codes.Contains(b.Code))
                {
                    //get the closest building:
                    if(resultBuilding == null || Vector3.Distance(b.transform.position, pos) < lastDistance)
                    {
                        resultBuilding = b;
                        lastDistance = Vector3.Distance(b.transform.position, pos);
                    }
                }
            }

            return resultBuilding;
        }

        //get the closest building of out of a list to a given position
        public static Building GetClosestBuilding(Vector3 pos, List<Building> buildings)
        {
            Building resultBuilding = null;
            float lastDistance = 0;

            //go through the buildings to search
            foreach (Building b in buildings)
            {
                //get the closest building:
                if (resultBuilding == null || Vector3.Distance(b.transform.position, pos) < lastDistance)
                {
                    resultBuilding = b;
                    lastDistance = Vector3.Distance(b.transform.position, pos);
                }
            }

            return resultBuilding;
        }
    }
}
