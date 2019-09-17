using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Minimap Icon Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    //all units/buildings/resources icons in the minimap will be handled by this component
    public class MinimapIconManager : MonoBehaviour
    {
        public static MinimapIconManager Instance;

        public GameObject IconPrefab; //the minimap's icon prefab
        public float MinimapIconHeight = 20.0f; //height of the minimap icon

        //a list where all created minimap icons are, this list will be used for object pooling
        List<GameObject> UnusedMinimapIcons = new List<GameObject>();

        //this is the color that the free units/buildings will have.
        public Color FreeFactionIconColor = Color.white;

        void Awake()
        {
            //set the instance:
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }

            if (IconPrefab == null)
            {
                //if there's no icon prefab then disable this component.
                Debug.LogError("No minimap icon prefab has been assigned in the Minimap Icon Manager.");
                enabled = false;
            }
        }

        //Assign a minimap icon:
        public void AssignIcon (SelectionObj Selection)
        {
            //assign the minimap icon to the selection obj component
            Selection.MinimapIcon = GetNewMinimapIcon(Selection.MainObj.transform, Selection.MinimapIconSize);

            AssignIconColor(Selection);
        }

        //method to assign the correct color to the minimap icon:
        public void AssignIconColor (SelectionObj Selection)
        {
            MeshRenderer IconRenderer = Selection.MinimapIcon.gameObject.GetComponent<MeshRenderer>();

            //set the color of the minimap icon depending on the object type
            switch (Selection.ObjType)
            {
                case SelectionObj.ObjTypes.Unit: //in case it's a unit
                    if (Selection.UnitComp.FreeUnit == false) //if the unit belongs to a faction
                    {
                        //set the faction color
                        IconRenderer.material.color = GameManager.Instance.Factions[Selection.UnitComp.FactionID].FactionColor;
                    }
                    else //if it's a free unit
                    {
                        //set the free faction color
                        IconRenderer.material.color = FreeFactionIconColor;
                    }
                    break;
                case SelectionObj.ObjTypes.Building: //If the object to select is a building:
                    //set the color as the building's faction color:
                    if (Selection.BuildingComp.FreeBuilding == false) //building belongs to a faction
                    {
                        //set as the faction color
                        IconRenderer.material.color = GameManager.Instance.Factions[Selection.BuildingComp.FactionID].FactionColor;
                    }
                    else //free building
                    {
                        //set as the free faction color
                        IconRenderer.material.color = FreeFactionIconColor;
                    }
                    break;
                case SelectionObj.ObjTypes.Resource: //in case it's a resource
                    //set the color depending on the resource type
                    IconRenderer.material.color = GameManager.Instance.ResourceMgr.ResourcesInfo[Selection.ResourceComp.ResourceID].TypeInfo.MinimapIconColor;
                    break;
                default:
                    break;
            }
        }

        //method to get a minimap icon either from the inactive list or create one
        GameObject GetNewMinimapIcon(Transform Parent, float Size)
        {
            GameObject NewMinimapIcon = null;

            if (UnusedMinimapIcons.Count > 0) //if there are any unused minimap icons
            {
                //get one and that's it
                NewMinimapIcon = UnusedMinimapIcons[0];
                UnusedMinimapIcons.RemoveAt(0);
            }
            else //if we don't have an unused one we need to create one
            {
                //create one from the prefab
                NewMinimapIcon = Instantiate(IconPrefab, Parent.position, Quaternion.identity);
            }

            //set its size;
            NewMinimapIcon.transform.localScale = new Vector3(Size, Size, Size);

            //set it as child of the parent object
            NewMinimapIcon.transform.SetParent(Parent, true);

            NewMinimapIcon.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f); //set its position
            NewMinimapIcon.transform.position = new Vector3(NewMinimapIcon.transform.position.x, MinimapIconHeight, NewMinimapIcon.transform.position.z); //set its position

            return NewMinimapIcon;
        }

        public void RemoveMinimapIcon (SelectionObj Selection)
        {
            //only proceed if there's a valid selection
            if (Selection.MinimapIcon == null)
                return;

            //remove it and add it to the unused list:
            Selection.MinimapIcon.SetActive(false);
            Selection.MinimapIcon.transform.SetParent(null, true);
            UnusedMinimapIcons.Add(Selection.MinimapIcon);
            Selection.MinimapIcon = null;
        }
    }
}