using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

/* Border script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class Border : MonoBehaviour
    {

        [HideInInspector]
        public bool IsActive = false; //is the border active or not?

        [Header("Border Object:")]
        //spawn the border object?
        public bool SpawnBorderObj = true;
        public GameObject BorderObj; //Use an object that is only visible on the terrain to avoid drawing borders outside the terrain.
        public float BorderHeight = 20.0f; //The height of the border object here
        [Range(0.0f, 1.0f)]
        public float BorderColorTransparency = 1.0f; //transparency of the border's object color
        public float Size = 10.0f; //The size of the border around this building:
        public float BorderSizeMultiplier = 2.0f; //To control the relation of the border obj's actual size and the border's map. Using different textures for the border objects will require using 

        //If the border belongs to an NPC player, you can require the NPC to build all the objects in the array below.
        //If the border belongs to the player, then this array will only represent the maximum amounts for each building
        //and if a building is not in this array, then the player is free to build as many as he wishes to build.
        [System.Serializable]
        public class BuildingsInsideBorderVars
        {
            public Building Prefab; //prefab of the building to be placed inside the border
            public string FactionCode; //Leave empty if you want this building to be considered by all factions
            [HideInInspector]
            public int CurrentAmount = 0; //current amount of the building type inside this border
            public int MaxAmount = 1; //maximum allowed amount of this building type inside this border

        }
        [Header("Border Buildings:")]
        public List<BuildingsInsideBorderVars> BuildingsInsideBorder;
        [HideInInspector]
        public List<Building> BuildingsInRange = new List<Building>();

        //components:
        [HideInInspector]
        public Building MainBuilding;

        //The list of resources belonging inside this border:
        [HideInInspector]
        public List<Resource> ResourcesInRange = new List<Resource>();

        GameManager GameMgr;
        ResourceManager ResourceMgr;
        [HideInInspector]
        public FactionManager FactionMgr = null;

        void Awake()
        {
            MainBuilding = gameObject.GetComponent<Building>();
        }

        //called to activate the border
        public void ActivateBorder()
        {
            //make sure to get the game manager and resource manager components
            GameMgr = GameManager.Instance;
            ResourceMgr = GameMgr.ResourceMgr;

            //if the border is not active yet
            if (IsActive == false)
            {
                //shuffle building defs list:
                RTSHelper.ShuffleList<BuildingsInsideBorderVars>(BuildingsInsideBorder);

                //if we're allowed to spawn the border object
                if (SpawnBorderObj == true)
                {
                    //create and spawn it
                    BorderObj = (GameObject)Instantiate(BorderObj, new Vector3(transform.position.x, BorderHeight, transform.position.z), Quaternion.identity);
                    BorderObj.transform.localScale = new Vector3(Size * BorderSizeMultiplier, BorderObj.transform.localScale.y, Size * BorderSizeMultiplier);
                    BorderObj.transform.SetParent(transform, true);

                    //Set the border's color to the faction it belongs to:
                    Color FactionColor = GameMgr.Factions[MainBuilding.FactionID].FactionColor;
                    BorderObj.GetComponent<MeshRenderer>().material.color = new Color(FactionColor.r, FactionColor.g, FactionColor.b, BorderColorTransparency);
                    //Set the border's sorting order:
                    BorderObj.GetComponent<MeshRenderer>().sortingOrder = GameMgr.LastBorderSortingOrder;
                    GameMgr.LastBorderSortingOrder--;
                }


                //Add the border to all borders list:
                GameMgr.AllBorders.Add(this);

                CheckBorderResources(); //check the resources around the border

                IsActive = true; //mark the border as active

                //Custom Event:
                if (GameMgr.Events)
                    GameMgr.Events.OnBorderActivated(this);
            }

            //set the faction manager
            FactionMgr = GameMgr.Factions[MainBuilding.FactionID].FactionMgr;

            //check the buildings in the border
            CheckBuildingsInBorder();
        }

        //called to check the resources inside the range of the border
        public void CheckBorderResources()
        {
            //We'll check the resources inside this border:
            if (ResourceMgr.AllResources.Count > 0)
            {
                for (int j = 0; j < ResourceMgr.AllResources.Count; j++)
                {
                    if (ResourceMgr.AllResources[j] != null) //if the resource is valid
                    {
                        if (ResourcesInRange.Contains(ResourceMgr.AllResources[j]) == false && ResourceMgr.AllResources[j].FactionID == -1)
                        {
                            //Making sure that it doesn't already exist before adding it.
                            if (Vector3.Distance(ResourceMgr.AllResources[j].transform.position, transform.position) < Size)
                            {
                                ResourcesInRange.Add(ResourceMgr.AllResources[j]);
                                ResourceMgr.AllResources[j].FactionID = MainBuilding.FactionID;
                            }
                        }
                    }
                }
            }
        }

        //returns the ID of a building inside the border
        public int GetBuildingIDInBorder(string Code)
        {
            if (BuildingsInsideBorder.Count > 0)
            {
                //Loop through all the buildings inside the array:
                for (int i = 0; i < BuildingsInsideBorder.Count; i++)
                {
                    //When we find the building in the border's list, return it.
                    if (BuildingsInsideBorder[i].Prefab.Code == Code)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        //add the building to a building's list
        public void RegisterBuildingInBorder(Building Building)
        {
            //add the building to the list:
            BuildingsInRange.Add(Building);
            //First check if the building exists inside the border:
            int i = GetBuildingIDInBorder(Building.Code);
            if (i != -1)
            {
                //If we reach the maximum allowed amount for this item then add it:
                BuildingsInsideBorder[i].CurrentAmount++;
            }
        }

        public void UnegisterBuildingInBorder(Building Building)
        {
            //remove the building from the list:
            BuildingsInRange.Remove(Building);
            //First check if the building exists inside the border:
            int i = GetBuildingIDInBorder(Building.Code);
            if (i != -1)
            {
                //If we reach the maximum allowed amount for this item then add it:
                BuildingsInsideBorder[i].CurrentAmount--;
            }
        }

        public bool AllowBuildingInBorder(string Code)
        {
            //This determines if we're still able to construct a building inside the borders:
            //Loop through all the buildings inside the array:
            if (BuildingsInsideBorder.Count > 0)
            {
                int i = GetBuildingIDInBorder(Code);
                if (i != -1)
                {
                    return BuildingsInsideBorder[i].CurrentAmount <= BuildingsInsideBorder[i].MaxAmount;
                }
            }

            //If the building doesn't belong the buildings allowed in border, then we're free to place it without any limitations:
            return true;
        }

        //check all buildings placed in the range of this border
        public void CheckBuildingsInBorder()
        {
            //if this is a single player game or a multiplayer game and this is the local player
            if (GameManager.MultiplayerGame == false || (GameManager.MultiplayerGame == true && GameManager.PlayerFactionID == MainBuilding.FactionID))
            {
                //This checks if there are buildings that are faction type specific and remove/keep them based on the faction code:
                //Loop through all the buildings inside the array:
                if (BuildingsInsideBorder.Count > 0)
                {
                    int i = 0;
                    while (i < BuildingsInsideBorder.Count)
                    {
                        //When a building has 
                        if (BuildingsInsideBorder[i].FactionCode != "")
                        {
                            if (GameMgr.Factions[MainBuilding.FactionID].TypeInfo.Code != BuildingsInsideBorder[i].FactionCode)
                            { //if the faction code is different
                                BuildingsInsideBorder.RemoveAt(i);
                            }
                            else
                            {
                                i++;
                            }
                        }
                        else
                        {
                            i++;
                        }
                    }
                }
            }
        }
    }
}