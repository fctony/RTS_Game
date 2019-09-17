using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;


/* Resource Generator script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */
namespace RTSEngine
{
   public class ResourceGenerator : MonoBehaviour {

		[System.Serializable]
		public class ResourceGenInfo
		{
			public string Name; //name of the resource to collect
            public bool CanGenerate = true; //Can the resource generator produce this resource?
            public float CollectAmountPerSecond = 0.66f; //how much of this resource can a resource collector collect in each second.
            //[HideInInspector]
            public float CollectOneUnitTime = 1.0f; //To add 1 unit to the resource, this is the time required to do that.
			[HideInInspector]
			public float Timer;
			public int MaxAmount; //The maximum amount that this generator can store.
			[HideInInspector]
			public int Amount; //Current amount of the collected resource.
			[HideInInspector]
			public bool MaxAmountReached = false;
			public AudioClip CollectionAudioClip; //played when the resource is collected.
			public Sprite TaskIcon; //When the maximum amount is reached, then a task appears on the task panel to collect the gathered resource when the generator is selected. This is the task's icon.
		}
		public ResourceGenInfo[] Resources;
		[HideInInspector]
		public List<int> ReadyToCollect = new List<int> ();

        public int TaskPanelCategory = 0; //Task panel category at which the collection button will be shown in case Auto Collect is turned off
		public bool AutoCollect = false; //if true, then resources will automatically added to the faction. if false then the resource collection will be limited and player would have to manually gather them when they reach the max amount.

		//other scripts:
		ResourceManager ResourceMgr;
        GameManager GameMgr;

		Building RefBuilding;


		void Start ()
		{
            RefBuilding = gameObject.GetComponent<Building> ();

			if (RefBuilding != null) {
                GameMgr = GameManager.Instance;
                ResourceMgr = GameMgr.ResourceMgr;
            } else {
				Debug.LogError ("The Resource Generator script must be placed at the same object that has the Building script!");
				enabled = false;
			}

			if (Resources.Length > 0) {
				for (int i = 0; i < Resources.Length; i++) { //loop through all the resources to generate
					if (ResourceMgr.GetResourceID (Resources [i].Name) >= 0) { //if it's a valid resource
						if (Resources [i].CollectAmountPerSecond >= 0) {

                            SetCollectionTime(i);
						}
					} else {
                        Resources[i].Timer = 0.0f;

                    }
				}
			}

			//if it's a multiplayer game and this does not belong to the local player's faction:
			if (GameManager.MultiplayerGame == true && GameManager.PlayerFactionID != RefBuilding.FactionID) {
				enabled = false;
			}
		}

        public void SetCollectionTime (int ID)
        {
            Resources[ID].CollectOneUnitTime = 1 / Resources[ID].CollectAmountPerSecond;

            //start the collection timer:
            Resources[ID].Timer = Resources[ID].CollectOneUnitTime;
            Resources[ID].MaxAmountReached = false;
        }

		void Update ()
		{
			if (RefBuilding == null) {
				Debug.LogError ("The Resource Generator script must be placed at the same object that has the Building script!");
				enabled = false;
				return;
			}
			//if the building can produce resources:
			if (RefBuilding.IsBuilt == true) {
				if (Resources.Length > 0) {
                    UpdateResourceGen();
				}
			}
		}

        //resource generation update:
        void UpdateResourceGen()
        {
            for (int i = 0; i < Resources.Length; i++)
            { //loop through all the resources to generate and make sure that it's possible to generate resources at this time
                if (Resources[i].MaxAmountReached == false && Resources[i].CanGenerate == true)
                {
                    if (Resources[i].Timer > 0)
                    {
                        Resources[i].Timer -= Time.deltaTime;
                    }
                    else
                    {
                        if (AutoCollect == true)
                        {
                            //if resources are auto added to the faction:
                            ResourceMgr.AddResource(RefBuilding.FactionID, Resources[i].Name, 1);
                        }
                        else
                        {
                            //if resources are not auto added:
                            Resources[i].Amount += 1;
                            if (Resources[i].Amount >= Resources[i].MaxAmount)
                            {
                                if (GameManager.PlayerFactionID == RefBuilding.FactionID)
                                {
                                    //max amount reached, stop collecting.
                                    Resources[i].MaxAmountReached = true;
                                    ReadyToCollect.Add(i);

                                    //update the task panel if this building is selected:
                                    if (RefBuilding.SelectionMgr.SelectedBuilding == RefBuilding)
                                    {
                                        RefBuilding.UIMgr.UpdateBuildingUI(RefBuilding);
                                    }
                                }
                                else
                                {
                                    ResourceMgr.AddResource(RefBuilding.FactionID, Resources[i].Name, Resources[i].Amount);
                                    Resources[i].Amount = 0;
                                }
                            }
                        }

                        Resources[i].Timer = Resources[i].CollectOneUnitTime; //relaunch the timer.
                    }
                }
            }
        }

        //a method to collect resources.
        public void CollectResources (int ResourceID)
        {
            GameMgr.UIMgr.HideTooltip(); // we hide the tooltip because the task will be gone
            //we're collecting resources.

            ResourceMgr.AddResource(RefBuilding.FactionID, Resources[ResourceID].Name, Resources[ResourceID].Amount); //add the resource to the faction

            //reset the resource generator settings for this resource ID.
            Resources[ResourceID].Amount = 0;
            Resources[ResourceID].MaxAmountReached = false; //launch the timer again
            ReadyToCollect.RemoveAt(ResourceID);

            if (GameManager.PlayerFactionID == RefBuilding.FactionID) //if this is the local player:
            {
                //and the resource generator is selected.
                if (GameMgr.UIMgr.SelectionMgr.SelectedBuilding == RefBuilding)
                {
                    //update the UI:
                    GameMgr.UIMgr.UpdateInProgressTasksUI();
                    GameMgr.UIMgr.UpdateTaskPanel();
                }
                AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, Resources[ResourceID].CollectionAudioClip, false); //Launch task audio.
            }
        }
	}
}