using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/* Selection Obj script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */
namespace RTSEngine
{
	public class SelectionObj : MonoBehaviour {

		//This script gets added to an empty object that only have a collider and a rigidbody, the collider represents the boundaries of the object (building or resource) that can be selected by the player.
		//The main collider of the object will only be used for placement.

        public enum ObjTypes { Unit, Building, Resource}
        public ObjTypes ObjType;

        public bool CanSelect = true; //if this is set to false then the unit/building/resource will not be selectable
        public bool SelectOwnerOnly = false; //if this is set to true then only the local player can select the object associated to this.

        public float MinimapIconSize = 0.5f;

		[HideInInspector]
		public GameObject MainObj; //The object that we're actually selecting.
        //main object components:
        [HideInInspector]
        public Unit UnitComp;
        [HideInInspector]
        public Building BuildingComp;
        [HideInInspector]
        public Resource ResourceComp;

        //the minimap icon here:
        [HideInInspector]
        public GameObject MinimapIcon;

        //other scripts
        GameManager GameMgr;
        SelectionManager SelectionMgr;
        UIManager UIMgr;

        void Start()
        {
            //get the components below
            GameMgr = GameManager.Instance;
            SelectionMgr = GameMgr.SelectionMgr;
            UIMgr = GameMgr.UIMgr;

            //get the main object main component depending on the object type:
            switch(ObjType)
            {
                case ObjTypes.Unit:
                    UnitComp = MainObj.GetComponent<Unit>();
                    break;
                case ObjTypes.Building:
                    BuildingComp = MainObj.GetComponent<Building>();
                    break;
                case ObjTypes.Resource:
                    ResourceComp = MainObj.GetComponent<Resource>();
                    break;
                default:
                    break;
            }

            //ask the minimap icon manager to create the icons for the main obj:
            MinimapIconManager.Instance.AssignIcon(this);

            gameObject.layer = 0; //Setting it to the default layer because raycasting ignores building and resource layers.

			//In order for collision detection to work, we must assign these settings to the collider and rigidbody.
			GetComponent<Collider> ().isTrigger = true;
            GetComponent<Collider>().enabled = true; //enable it

            if (GetComponent<Rigidbody> () == null) {
				gameObject.AddComponent<Rigidbody> ();
			}
			GetComponent<Rigidbody> ().isKinematic = true;
			GetComponent<Rigidbody> ().useGravity = false;
		}

		// Update is called once per frame
		public void SelectObj () {

            if (CanSelect == false) //if this can't be selected:
                return; //do not advance

			if (MainObj != null) { //Making sure we have linked an object or a resource object to this script:
				if (!EventSystem.current.IsPointerOverGameObject () && BuildingPlacement.IsBuilding == false) { //Make sure that the mouse is not over any UI element
                    switch (ObjType)
                    {
                        case ObjTypes.Unit: //in case it's a unit
                            //if this can only be selected by its owner:
                            if (SelectOwnerOnly == true && UnitComp.FactionID != GameManager.PlayerFactionID)
                                return; //don't allow player to select.

                            UnitComp.SelectUnit();

                            if (UnitComp.FactionID == GameManager.PlayerFactionID && UnitComp.Dead == false)
                            {
                                AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, UnitComp.SelectionAudio, false);
                            }
                            break;
                        case ObjTypes.Building: //If the object to select is a building:
                            //if this can only be selected by its owner:
                            if (SelectOwnerOnly == true && BuildingComp.FactionID != GameManager.PlayerFactionID)
                                return; //don't allow player to select.

                            if (BuildingComp.Placed == true)
                            {
                                //Only select the building when it's already placed and when we are not attempting to place any building on the map:
                                BuildingComp.FlashTime = 0.0f;
                                BuildingComp.CancelInvoke("SelectionFlash");

                                SelectionMgr.SelectBuilding(BuildingComp);

                                if (BuildingComp.Destroyed == false)
                                {
                                    AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, BuildingComp.SelectionAudio, false);
                                    if (BuildingComp.PortalMgr)
                                    { //if this is a portal building:
                                      //trigger a mouse click:
                                        BuildingComp.PortalMgr.TriggerMouseClick();
                                    }
                                }
                            }
                            break;
                        case ObjTypes.Resource: //in case it's a resource
                            ResourceComp.FlashTime = 0.0f;
                            ResourceComp.CancelInvoke("SelectionFlash");

                            SelectionMgr.SelectResource(ResourceComp);

                            AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, GameMgr.ResourceMgr.ResourcesInfo[ResourceComp.ResourceID].TypeInfo.SelectionAudio, false);
                            break;
                        default:
                            break;
                    }
				}
			}
		}

        //Hover health bar:

        //when the mouse is over this object
        void OnMouseEnter()
        {
            //if this is related to a resource
            if (ObjType == ObjTypes.Resource)
            {
                //we have no business here
                return;
            }
            //if the hover health bar feature is enabled
            if (UIMgr.EnableHoverHealthBar == true)
            {
                if (MainObj != null) //the main object is valid
                {
                    //Making sure we have linked an object or a resource object to this script:
                    if (!EventSystem.current.IsPointerOverGameObject() && BuildingPlacement.IsBuilding == false)
                    { //Make sure that the mouse is not over any UI  and we're not placing any building
                        float Health = 0.0f;
                        float MaxHealth = 0.0f;
                        float PosY = 0.0f;

                        int FactionID = -1;

                        switch (ObjType)
                        {
                            case ObjTypes.Unit: //in case it's a unit
                                if (UnitComp.enabled == true)
                                { //make sure the unit is enabled
                                  //get the health stats from the unit:
                                    Health = UnitComp.Health;
                                    MaxHealth = UnitComp.MaxHealth;
                                    PosY = UnitComp.HoverHealthBarY;
                                    FactionID = UnitComp.FactionID;
                                }
                                break;
                            case ObjTypes.Building: //If the object to select is a building:
                                if (BuildingComp.Placed == true && BuildingComp.enabled == true)
                                { //if the building component is enabled and the actual building is placed
                                  //get the health stats from the building:
                                    Health = BuildingComp.Health;
                                    MaxHealth = BuildingComp.MaxHealth;
                                    PosY = BuildingComp.HoverHealthBarY;
                                    FactionID = BuildingComp.FactionID;
                                }
                                break;
                            default:
                                break;
                        }

                        //if this is not the player faction ID and we're only allowed to show hover health bars for friendly units/buildings:
                        if (FactionID != GameManager.PlayerFactionID && UIMgr.PlayerFactionOnly == true)
                        {
                            return; //STOP BEFORE WE GO FURTHER!
                        }

                        //enable the hover health bar:
                        UIMgr.TriggerHoverHealthBar(true, this, PosY);
                        //if the hover health bar was successfully enabled:
                        if (UIMgr.IsHoverSource(this))
                        {
                            //update the health:
                            UIMgr.UpdateHoverHealthBar(Health, MaxHealth);
                        }
                    }
                }
            }
        }

        //if the mouse leaves this object
        void OnMouseExit()
        {
            //if the hover health bar feature is enabled
            if (UIMgr.EnableHoverHealthBar == true)
            {
                //disable the hover health bar:
                UIMgr.TriggerHoverHealthBar(false, this, 0.0f);
            }
        }

        //called when a trigger collider enters in this unit:
        void OnTriggerEnter (Collider other)
        {
            //if the object has a selection obj
            if(other.gameObject.GetComponent<SelectionObj>() && GameMgr != null)
            {
                //trigger custom event for selection obj collision:
                if(GameMgr.Events != null)
                    GameMgr.Events.OnSelectionObjEnter(this, other.gameObject.GetComponent<SelectionObj>());
            }
        }

    }
}