using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/* Map Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
	public class SinglePlayerManager : MonoBehaviour {

        public static SinglePlayerManager instance = null;

        [Header("General:")]

        public string MainMenuScene; //the name of the scene that you want the player to get back to from the the single player menu:

		public int MinPopulation = 5; //The minimum amount of population to start with.

		//Maps' info:
		[System.Serializable]
		public class MapsVars
		{
			public string MapScene = ""; //Name of the scene that includes the map.
			public string MapName = "Map"; //Name of the map to show in the UI menu.
			public string MapDescription = "Description"; //When a map is selected, this description is displayed.
			public int MaxFactions = 4; //Maximum amount of factions that this map supports.
			public FactionTypeInfo[] FactionTypes; //The available types of factions that can play in this map.
		}
        [Header("Maps & Factions:")]
        public MapsVars[] Maps;
		[HideInInspector]
		public int CurrentMapID = -1; //holds the currently selected map ID.

		//an array that holds the faction's info:
		[System.Serializable]
		public class FactionVars
		{
			public FactionUIInfo FactionUI; //The faction UI slot associated to this faction.
			public FactionTypeInfo TypeInfo;
			public string FactionName; //holds the faction's name.
			public Color FactionColor = Color.blue; //holds the faction's color.
			public int FactionColorID = 0; //holds the faction's color ID.
			public bool playerControlled = false; //is the faction controlled by the player or not?
			public int InitialPopulation = 10; //the initial amount of the faction's population slots.
			public NPCManager npcMgr; //holds the NPC manager
		}
		[HideInInspector]
		public List<FactionVars> Factions;

		public Color[] AllowedColors; //Array that holds all the allowed colors for the factions

        //array that holds the NPC Manager prefabs that can be selected for NPC factions from this single player menu.
		public NPCManager[] npcManagers;

        [Header("UI:")]
        //Map info UI:
        public Text MapDescription; //A UI Text that shows the selected map's description.
        public Text MapMaxFactions; //A UI Text that shows the selected map's maximum allowed amount of factions.
        public Dropdown MapDropDownMenu; //A UI object that has a Dropdown component allowing the player to pick the map.

        public Transform FactionUIParent; //UI object parent of all the objects holding the "FactionUIInfo.cs" script. Each child object represents a faction slot.
        public FactionUIInfo FactionUISample; //A child object of the "FactionUIParent", that includs the "FactionUIInfo.cs" component. This represents a faction slot and holds all the information of the faction (color, name, population, etc).
        //This object will be used a sample as it will be duplicated to the number of the faction that the player chooses.

        void Awake () {

            //singleton:
            if (instance == null)
                instance = this;
            else if (instance != null)
                Destroy(instance);

            DontDestroyOnLoad (this.gameObject); //we want this object to be passed to the map's scene.

			int i = 0;

			//go through all the maps:
			if (Maps.Length > 0) {
				CurrentMapID = 0;
				List<string> MapNames = new List<string>();

				for (i = 0; i < Maps.Length; i++) {
					//If we forgot to put the map's scene or we have less than 2 as max players, then show an error:
					if (Maps [i].MapScene == null) {
						Debug.LogError ("[Single Player Manager]: Map ID " + i.ToString () + " is invalid.");
					}
					if (Maps [i].MaxFactions < 2) {
						Debug.LogError ("[Single Player Manager]: Map ID " + i.ToString () + " max factions (" + Maps [i].MaxFactions.ToString () + ") is lower than 2.");
					} 
					MapNames.Add (Maps [i].MapName);
				}

				//set the map drop down menu options to the names of the maps.
				if (MapDropDownMenu != null) {
					MapDropDownMenu.ClearOptions ();
					MapDropDownMenu.AddOptions (MapNames);
				} else {
					Debug.LogError ("[Single Player Manager]: You must add a drop down menu to pick the maps.");
				}
			} else {
				//At least one map must be included:
				Debug.LogError ("[Single Player Manager]: You must include at least one map.");
			}

			//go through all the npc managers:
			if (npcManagers.Length > 0) {
				for (i = 0; i < npcManagers.Length; i++) {
					//Show an error if one of the npc manager components has not been assigned:
					if (npcManagers[i] == null) {
						Debug.LogError ("[Single Player Manager]: NPC Manager ID " + i.ToString () + " has not been defined.");
					}
				}
			} else {
				//We need at least one npc manager:
				Debug.LogError ("[Single Player Manager]: You must include at least one NPC Manager.");
			}


			Factions = new List<FactionVars>(); //Initialize the factions list.
            //add the default two factions:
            for(i = 0; i < 2; i++)
                AddFaction();

			UpdateMap (); //update the map's settings
		}

		//a method that adds a new faction:
		public void AddFaction () {
			//if we haven't reached the maximum allowed amount of factions
			if (Factions.Count < Maps [CurrentMapID].MaxFactions) {
				//create a new faction
				FactionVars NewFaction = new FactionVars ();
				NewFaction.FactionName = "Faction " + Factions.Count.ToString ();
				NewFaction.InitialPopulation = MinPopulation;

                //Creating a UI panel for each faction:
                if (Factions.Count == 0) //first faction to be created -> player faction
                {
                    NewFaction.playerControlled = true;

                    NewFaction.FactionUI = FactionUISample;

                    //add the NPC Managers from the list in this component as options in the npc manager menu:
                    //first put the names of the npc managers inside a list of strings:
                    List<string> npcMgrOptions = new List<string>();
                    foreach (NPCManager mgr in npcManagers)
                        npcMgrOptions.Add(mgr.Name);
                    //add the above list as the options for the npc manager menu:
                    NewFaction.FactionUI.npcManagerMenu.ClearOptions();
                    NewFaction.FactionUI.npcManagerMenu.AddOptions(npcMgrOptions);
                    //we only need to set the npc manager menu once as it will be copied from other factions later.

                    NewFaction.FactionUI.npcManagerMenu.gameObject.SetActive(false); //hide the npc manager menu for the first faction (as it is always player controlled).
                }
                else
                {
                    NewFaction.playerControlled = false; //not player controlled.

                    //taking the FactionUISample as base to create all the rest:
                    GameObject FactionUIClone = (GameObject)Instantiate(FactionUISample.gameObject, Vector3.zero, Quaternion.identity);
                    FactionUIClone.transform.SetParent(FactionUIParent); //make sure to set the FactionUI objects to the same parent.
                    NewFaction.FactionUI = FactionUIClone.GetComponent<FactionUIInfo>();
                    FactionUIClone.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);

                    NewFaction.FactionUI.npcManagerMenu.gameObject.SetActive(true); //activate the npc manager menu for NPC factions.
                }

                //set the faction's UI info:
                //if these are the first two factions, then they can not be removed, if not they can be removed:
                NewFaction.FactionUI.RemoveFactionButton.gameObject.SetActive((Factions.Count < 2) ? false : true);

                NewFaction.FactionUI.PopulationInput.text = NewFaction.InitialPopulation.ToString (); //population
				NewFaction.FactionUI.FactionNameInput.text = NewFaction.FactionName.ToString (); //name
				NewFaction.FactionUI.ColorImg.color = NewFaction.FactionColor; //color
                NewFaction.npcMgr = npcManagers[0];

                //the faction type:
                NewFaction.FactionUI.FactionTypeMenu.ClearOptions (); //clear the default faction type menu options.
                //if this is not the player controlled faction/first faction:
                if (Factions.Count > 0)
                {
                    //simply copy the faction type menu options from the first created faction.
                    if (Factions[0].FactionUI.FactionTypeMenu.options.Count > 0)
                    {
                        NewFaction.FactionUI.FactionTypeMenu.AddOptions(Factions[0].FactionUI.FactionTypeMenu.options);
                    }
                }
				NewFaction.FactionUI.FactionTypeMenu.value = 0; //pick the first type in the list by default.
				NewFaction.TypeInfo = Maps [CurrentMapID].FactionTypes [0];

				NewFaction.FactionUI.Mgr = this;

				Factions.Add (NewFaction); 
				NewFaction.FactionUI.Faction = Factions [Factions.Count - 1]; //to link the FactionUI object with the map manager.

			} else {
                Debug.LogError("[Single Player Manager]: Map maximum factions amount has been reached.");
            }
		}

		//a method that removes the faction from the list:
		public void RemoveFaction (int ID) {
			//We can't remove the faction controlled by the player and we must at least keep two factions:
			if (ID == 0 || Factions.Count == 2) {
				return;
			}

			if (ID < Factions.Count) {
				//destroy the faction's UI object and remove it from the list.
				Destroy (Factions [ID].FactionUI.gameObject);
				Factions.RemoveAt (ID);
			}
		}

		//a method that updates the map:
		public void UpdateMap ()
		{
			if (MapDropDownMenu != null) {
				int MapID = MapDropDownMenu.value;
				//Make sure the map ID is valid and defined
				if (MapID < Maps.Length) {
					CurrentMapID = MapID; //Set the new map ID

					//check if the amount of factions does surpass the max amount of factions allowed for this map:
					if (Factions.Count > Maps [CurrentMapID].MaxFactions) {
						//Remove factions until the max amount factions is reached.
						for (int i = (Factions.Count - Maps [CurrentMapID].MaxFactions); i > 0; i--) {
							RemoveFaction (Factions.Count-1);
						}
					}

					UpdateMapUI ();
				}
			}
		}

		//update the map's UI by:
		public void UpdateMapUI ()
		{
			if (MapDescription != null) {
				MapDescription.text = Maps [CurrentMapID].MapDescription; //showing the selected map's description.
			}
			if (MapMaxFactions != null) {
				MapMaxFactions.text = Maps [CurrentMapID].MaxFactions.ToString (); //showing the selected map's maximum amount of 
			}

			List<string> FactionTypes = new List<string> ();
			if (Maps [CurrentMapID].FactionTypes.Length > 0) { //if there are actually faction types to choose from:
				for(int i = 0; i < Maps [CurrentMapID].FactionTypes.Length ; i++) //create a list with the names with all possible faction types:
				{
					FactionTypes.Add (Maps [CurrentMapID].FactionTypes [i].Name);
				}
			}

			for(int i = 0; i < Factions.Count; i++) //for each present faction:
			{
				Factions [i].FactionUI.FactionTypeMenu.ClearOptions (); //clear all the faction type options.
				if (FactionTypes.Count > 0) {
					//Add the faction types' names as options:
					Factions [i].FactionUI.FactionTypeMenu.AddOptions(FactionTypes);
					Factions [i].FactionUI.FactionTypeMenu.value = 0;
					Factions [i].TypeInfo = Maps [CurrentMapID].FactionTypes [0];
				}
			}
		}

		//start the game and loads the map's scene
		public void StartGame ()
		{
			SceneManager.LoadScene (Maps [CurrentMapID].MapScene);
		}

		//go back to the main menu:
		public void MainMenu ()
		{
			SceneManager.LoadScene (MainMenuScene);
			Destroy (this.gameObject);
		}
	}
}