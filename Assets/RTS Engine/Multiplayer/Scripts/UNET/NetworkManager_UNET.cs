using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.Networking.Types;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/* Network Map Manager: script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
	public class NetworkManager_UNET : NetworkLobbyManager {

        public static NetworkManager_UNET instance; //to allow the network manager to be accessed by other components

        [Header("General:")]

        public string MainMenuScene; //the name of the scene that the player gets back to when leaving the multiplayer menu.
		public GameObject CanvasObj; //the main cnavas object which should be a child object of the object that has this component.

        public string GameVersion = "1.0.0"; //players who don't have the same game version can't be in the same lobby, host version required for all players
        public Text GameVersionText;

        [Header("Main MP Menu:")]

        public GameObject MainMPMenu; //The main multiplayer menu (shown when the scene starts).

        [Header("Matchmaking menu:")]

        public GameObject MatchMakingMenu; //The menu shown when searching for an internet match.

        //Matchmaking UI:
        public InputField InternetMatchName; //The name of the internet match to create.
        public InputField InternetMatchSize; //The size of the internet match to create.
        public int InternetMatchMaxSize = 4; //The maximum allowed size for an internet match to create.
        public GameObject MatchButtonPrefab; //The match button prefab (for each match this button will be spawned which will show the match's info and a button to let the player join).
        public Transform MatchesParent; //the matches list parent object.
        List<InternetMatch_UNET> MatchButtons = new List<InternetMatch_UNET>(); //holds  all the current match buttons.

        //or we in match making or local host?
        bool MatchMaking = false;

        [Header("Loading Menu:")]

        public GameObject LoadingMenu; //The menu shown when loading to access the lobby.

        //Info msg:
        public Text InfoMsg; //a message shown whenever there's an error/warning.
        float InfoMsgTimer; //how long will the message be shown for.
        public enum DisconnectionTypes { Left, Kicked, GameVersion, Timeout, Abort }
        [HideInInspector]
        public DisconnectionTypes LastDisconnectionType;

        [Header("Lobby Menu:")]

        public GameObject LobbyMenu; //The lobby menu (shown when the player joins a match).
        public Transform LobbyPlayerParent; //the parent object of all the Lobby Objects

        public Color[] AllowedColors; //the colors that a faction is allowed to take.

        GameObject FactionLobbyObj; //the local player's faction object.

        [Header("Map Menu (host only):")]

        public GameObject HostMapMenu; //The map settings menu (the one that has the map info).

        public Text RoomName; //The UI Text to show the room's name

        //Map info:
        [System.Serializable]
        //A list that holds all the maps that can be accessed in a multiplayer game:
        public class MapsVars
        {
            public string MapScene = ""; //The scene's name.
            public string MapName = "Map"; //The map's name to be displayed in the UI.
            public string MapDescription = "Description"; //The map's description.
            public int MaxFactions = 4; //The maximum amount of factions that this map supports
            public int InitialPopulation = 5; //the map's initial population.
            public FactionTypeInfo[] FactionTypes; //The available types of factions that can play in this map.
        }
        public MapsVars[] Maps;

        [HideInInspector]
		public int CurrentMapID = -1;

        public Dropdown MapDropDownMenu; //the menu that allows the host to select the map:

        //is this the room's host?
        bool IsHost = false;

        //Map info UI:
        public Text MapInitialPopulation; //The UI text to show the map's initial population.
		public Text MapDescription; //The UI text to show the selected map's description.
		public Text MapMaxFactions; //The UI text to show the map's maximum faction.

        //Start Game button:
        public GameObject StartGameButton;

        [Header("Network Discovery:")]

        //Local network related:
        //network discovery component: allows rooms to be discoverd across a local network.
        public NetDiscovery_UNET Discovery;
        public bool UseDiscovery = false; //enable/disable using the network discovery component.

		[HideInInspector]
		public MFactionLobby_UNET LocalFactionLobbyInfo;

        void Awake ()
		{
			Time.timeScale = 1.0f; //When the game ends, the time scale is set to 0.0f, we reset it now in case we're coming from a finished game

			//allow only one network manager in the game:
            if(instance == null)
            {
                instance = this;
            }
            else if(instance != this)
            {
                Destroy(gameObject);
            }

			//hide the info message at the beginning
			InfoMsg.gameObject.SetActive (false);
			InternetMatchSize.text = matchSize.ToString ();

            //If there's a text obj to display the game version:
            if (GameVersionText)
                GameVersionText.text = GameVersion; //display it

        }

		void Start () {

			//Set the maps in the map drop down menu:
			int i = 0;

			if (Maps.Length > 0) {
				CurrentMapID = 0;
				List<string> MapNames = new List<string>();

				for (i = 0; i < Maps.Length; i++) {
					//If we forgot to put the map's scene or we have less than 2 as max players, then show an error:
					if (Maps [i].MapScene == null) {
						Debug.LogError ("Map ID " + i.ToString () + " is invalid!");
					}
					if (Maps [i].MaxFactions < 2) {
						Debug.LogError ("Map ID " + i.ToString () + " max factions (" + Maps [i].MaxFactions.ToString () + ") is lower than 2!");
					} 
					if (Maps [i].InitialPopulation < 0) {
						Debug.LogError ("Map ID " + i.ToString () + " initial population (" + Maps [i].InitialPopulation.ToString () + ") must be > 0.");
					} 
					MapNames.Add (Maps [i].MapName);
				}

				//set the maps in the drop down menu
				if (MapDropDownMenu != null) {
					MapDropDownMenu.ClearOptions ();
					MapDropDownMenu.AddOptions (MapNames);
				} else {
					Debug.LogError ("You must add a drop down menu to pick the maps!");
				}
			} else {
				//At least one map must be included:
				Debug.LogError ("You must include at least one map in the map manager!");
			}

            //if we're using the network discovery component for local network:
            if (UseDiscovery == true)
            {
                Discovery.NetworkMgr = this;
            }
		}

        void Update()
        {
            //the info message timer:
            if (InfoMsgTimer > 0)
            {
                InfoMsgTimer -= Time.deltaTime;
            }
            if (InfoMsgTimer < 0)
            {
                InfoMsgTimer = 0;
                InfoMsg.gameObject.SetActive(false);
            }
        }

        //Map settings related:

        //a method that updates the selected map's info.
        public void UpdateMapUIInfo ()
		{
			//show the map name in the drop down menu
			CurrentMapID = MapDropDownMenu.value;

			//show the map's info: population, description and max factions:
			MapInitialPopulation.text = Maps [CurrentMapID].InitialPopulation.ToString();
			MapDescription.text = Maps [CurrentMapID].MapDescription;
			MapMaxFactions.text = Maps [CurrentMapID].MaxFactions.ToString ();

			playScene = Maps [CurrentMapID].MapScene; //set the Play scene to load when the game starts.

		}

		//this method updates the map settings for the local player:
		public void UpdateMapSettings ()
		{
            LocalFactionLobbyInfo.UpdateMapInfo ();
		}

        //when a player joins the room, this is called to create the lobby player object
        public override GameObject OnLobbyServerCreateLobbyPlayer(NetworkConnection conn, short playerControllerId)
		{
			FactionLobbyObj = Instantiate (lobbyPlayerPrefab.gameObject);

			//set the lobby player object's info: faction id, name and color
			FactionLobbyObj.GetComponent<MFactionLobby_UNET> ().FactionID = numPlayers;

			return FactionLobbyObj;
		}

		//when the game scene is loaded for the player:
		public override bool OnLobbyServerSceneLoadedForPlayer (GameObject lobbyPlayer, GameObject gamePlayer)
		{
			gamePlayer.GetComponent<MFactionManager_UNET>().FactionID = lobbyPlayer.GetComponent<MFactionLobby_UNET> ().FactionID; //sync the faction ID.
            return true;
		}

        //Localhost:

        //start the local host:
        public void StartLocalHost ()
		{
			networkPort = 7777; //currently using the port 7777 only.

			StartHost (); //start hosting the game

			IsHost = true; //mark this as the host
		}

		//stop the local host:
		public void StopLocalHost ()
		{
			StopHost ();
		}

		//connect to the local host:
		public void ConnectToLocalGame()
		{
			MatchMaking = false;
			if (UseDiscovery == true) { //if we're using discovery
                //then start the discovery component and start looking for a game
				Discovery.Initialize ();
				Discovery.StartAsClient (); //mark as client
			} else {
                //no discovery comp? then set the default settings for the IP and connect
				networkPort = 7777;
				networkAddress = "localhost";

				StartClient ();
			}

			MainMPMenu.gameObject.SetActive(false); //hide the main menu
			LoadingMenu.gameObject.SetActive(true); //show the loading menu

			ShowInfoMsg ("Joining LAN game lobby...", 2.0f); //send message
		}

        //this method allows to join a local game using the IP address of the host
		public void JoinLocalGame (string IP)
		{
			networkAddress = IP; //assign the IP address

			MatchMaking = false; //disable this
			MainMPMenu.gameObject.SetActive(false); //hide the main menu
			LoadingMenu.gameObject.SetActive(true); //show the loading menu

			StartClient (); //start the client
		}

        //called when the player starts hosting a game and the lobby is ready
		public override void OnLobbyStartHost()
		{
            //if we're using discovery
			if (UseDiscovery == true) {
				Discovery.Initialize ();
				Discovery.StartAsServer (); //mark as the host
			}
		}

		//Match making:

		//this enables the match making:
		public void EnableMatchMaking ()
		{
			//UseDiscovery = false;
			StartMatchMaker (); //enable match making
			InternetMatchName.text = "Game " + Random.Range (0, 100).ToString ();
			SetInternetMatchName ();

			//show the match making menu:
			MatchMakingMenu.gameObject.SetActive (true);
			MainMPMenu.gameObject.SetActive (false);

			RefreshInternetMatches (); //refresh the list of available online matches.
		}

		//disable match making
		public void DisableMatchMaking ()
		{
			StopMatchMaker (); //disable match making.

			//hide the match making menu
			MatchMakingMenu.gameObject.SetActive (false);
			MainMPMenu.gameObject.SetActive (true); //show the main menu
		}

		//set the internet match name:
		public void SetInternetMatchName ()
		{
			if (InternetMatchName.text != "") { //only if the match is valid.
				matchName = InternetMatchName.text;
			}
		}

		//set the internet match size:
		public void SetMatchSize ()
		{
			if (InternetMatchSize.text != "") {
				ushort Amount;
				//check if the match size is valid:
				bool Result = System.UInt16.TryParse (InternetMatchSize.text, out Amount);
				if (Result == true) {
					if (Amount > 1 && Amount <= InternetMatchMaxSize) {
						matchSize = Amount;
					} else {
						InternetMatchSize.text = matchSize.ToString ();
					}
				} else {
					InternetMatchSize.text = matchSize.ToString ();
				}
			}
		}

		//start the internet game:
		public void StartInternetMatch ()
		{
			matchMaker.CreateMatch(matchName, matchSize, true, "", "","",0,0, OnMatchCreate);
		}

		//callback when a match is started:
		public override void OnMatchCreate (bool Success, string ExtendedInfo, MatchInfo matchInfo)
		{
			base.OnMatchCreate (Success, ExtendedInfo, matchInfo);
			//show the loading menu:
			MatchMakingMenu.gameObject.SetActive(false);
			LoadingMenu.gameObject.SetActive(true);
			if (Success == true) {
				ShowInfoMsg ("Internet game lobby successfully created.", 2.0f);
				MatchMaking = true;
				IsHost = true;
			} else {
				ShowInfoMsg("Failed to create internet game lobby.", 2.0f);
			}
		}

		//callback when the player joins an internet match:
		public new void OnMatchJoined (bool Success, string ExtendedInfo, MatchInfo matchInfo)
		{
			base.OnMatchJoined (Success, ExtendedInfo, matchInfo);
			//show the loading's menu:
			MatchMakingMenu.gameObject.SetActive(false);
			LoadingMenu.gameObject.SetActive(true);
			if (Success == true) {
				ShowInfoMsg ("Joining Internet game lobby...", 15.0f);
				MatchMaking = true;
			} else {
				ShowInfoMsg("Failed to join Internet game lobby...", 2.0f);
			}
		}

		//refresh the internet match list.
		public void RefreshInternetMatches ()
		{
			matchMaker.ListMatches(0,20, "", false, 0,0, OnMatchList);
		}

		//callback when requestin to update the match list:
		public override void OnMatchList(bool Success, string ExtendedInfo, List<MatchInfoSnapshot> matchList)
		{
			base.OnMatchList (Success, ExtendedInfo, matchList);

			if (Success == true) {
				int i = 0;

				if (MatchButtons.Count > 0) {
					//hide all the match buttons' objects:
					for (i = 0; i < MatchButtons.Count; i++) {
						MatchButtons [i].gameObject.SetActive (false);
					}
				}

				if (matches.Count > 0) {
					for (i = 0; i < matches.Count; i++) {
						//show the matchs buttons' objects:
						if (MatchButtons.Count <= i) {
							//create a match button if there isn't one for this room:
							GameObject NewMatchButton = (GameObject)Instantiate (MatchButtonPrefab, new Vector3 (0f, 0f, 0f), Quaternion.identity);
							NewMatchButton.transform.SetParent (MatchesParent, false);
							MatchButtons.Add (NewMatchButton.GetComponent<InternetMatch_UNET> ());

							//set the match button's settings: name, and size.
							MatchButtons [i].ID = i;
							MatchButtons [i].MatchName.text = matches [i].name;
							MatchButtons [i].MatchSize.text = matches [i].currentSize.ToString () + "/" + matches [i].maxSize.ToString ();
						}

						MatchButtons [i].gameObject.SetActive (true);

					}
				} else {
					ShowInfoMsg ("No matches available.", 2.0f);
				}
			}
		}

		//allows the player to join an internet room.
		public void JoinInternetMatch (int ID)
		{
			if (matches [ID] != null) {
				matchMaker.JoinMatch (matches[ID].networkId, "", "","",0,0, OnMatchJoined);
			}
		}

        //callbacks for (dis)connection and errors:

		//when there's an error connecting
		public override void OnClientError(NetworkConnection NetConnection, int ErrorID)
		{
			base.OnClientError (NetConnection, ErrorID);
			ShowInfoMsg("Connection to game has failed, error ID: "+ErrorID.ToString(), 2.0f);
		}

		//when the client connects:
		public override void OnClientConnect (NetworkConnection NetConnection)
		{
			base.OnClientConnect (NetConnection);
			ShowInfoMsg("Connection to game is successful.", 1.0f);

			if (MatchMaking == true) {
				RoomName.text = matchName;
			} else {
				RoomName.text = "Local Game";
			}
		}

		//when the client leaves the game:
		public override void OnStopClient ()
		{
			base.OnStopClient ();
			if (UseDiscovery == true) { //if we're using discovery
				Discovery.StopBroadcast (); //stop broadcasting
				Discovery.Connected = false; //mark as disconnected
			}

            //if the player is leaving the game (not disconnecting due to timeout or an error).
            switch(LastDisconnectionType)
            {
                case DisconnectionTypes.Left:
                    ShowInfoMsg("You left the room/game.", 2.0f);
                    break;
                case DisconnectionTypes.Kicked:
                    ShowInfoMsg("You've been kicked from the game.", 2.0f);
                    break;
                case DisconnectionTypes.GameVersion:
                    ShowInfoMsg("You don't have the same game version as the lobby's host.", 2.0f);
                    break;
                case DisconnectionTypes.Abort:
                    ShowInfoMsg("Connection to lobby has been aborted.", 2.0f);
                    break;
                case DisconnectionTypes.Timeout:
                    ShowInfoMsg("Your connection timed out.", 2.0f);
                    break;
                default:
                    break;                  
            }

			ResetMPMenu (); //reset the multiplayer menu
		}

		//when the client disconnects:
		public override void OnClientDisconnect (NetworkConnection NetConnection)
		{
			base.OnClientDisconnect (NetConnection);
			ShowInfoMsg("The room/game is no longer available.", 2.0f);
			ResetMPMenu ();
		}

		//called on the server when a player disconnects
		public override void OnLobbyServerDisconnect (NetworkConnection conn)
		{

            //checked defeated factions if we are inside a game:
            GameManager GameMgr = FindObjectOfType(typeof(GameManager)) as GameManager;
            if (GameMgr != null)
            { //if we're already in a game

                int FactionID = -1; //holds the faction ID of the client that disconnected:
                int i = 0;

                while (i < GameMgr.Factions.Count && FactionID == -1) //go through all the factions if we still haven't found the disconnected faction ID yet
                {
                    //Check if this is client's faction by comparing the unique connection ID:
                    if(GameMgr.Factions[i].ConnID_UNET == conn.connectionId)
                    {
                        //this is the faction
                        FactionID = i;
                    }
                    i++;
                }

                if (FactionID != -1) //if a valid faction has been found
                {
                    //ask the server to announce that the faction has been defeated.
                    //send input action to the input manager
                    InputVars NewInputAction = new InputVars();

                    //mode:
                    NewInputAction.SourceMode = (byte)InputSourceMode.Destroy;
                    NewInputAction.TargetMode = (byte)InputTargetMode.Faction;

                    //Faction ID that disconnected:
                    NewInputAction.Value = FactionID;

                    InputManager.SendInput(NewInputAction);
                }
            }
            
            base.OnLobbyServerDisconnect (conn);
		}

		//Leave the lobby to main menu:
		public void LeaveLobby ()
		{
            if (IsHost == true) { //if the player was hosting a game
				StopHost (); //stop it
			} else { //if he was a normal client
				StopClient (); //stop it as well
			}
        }

		//leave the loading menu:
		public void LeaveLoadingMenu ()
		{
            if (MatchMaking == false) { //if we were trying to connect to a local game
				StopClient (); //abort
				LoadingMenu.gameObject.SetActive (false);
				MainMPMenu.gameObject.SetActive (true);
			} else {
				LeaveLobbyToMatchingMaking (); //if we were trying to connect to an internet match then go back to the match making lobby
			}

			MatchMaking = false;
		}

		//Leave the lobby to the match making menu:
		public void LeaveLobbyToMatchingMaking ()
		{
            StopHost ();
			EnableMatchMaking ();

			LoadingMenu.gameObject.SetActive(false);
			MatchMakingMenu.gameObject.SetActive (true);

		}

		//show an info message:
		public void ShowInfoMsg (string Msg, float Time)
		{
			InfoMsgTimer = Time;
			InfoMsg.text = Msg;
			InfoMsg.gameObject.SetActive (true);

		}

		//Start the game:
		public void StartGame ()
		{
			if (numPlayers > 0) { //make sure there's more than one player:
				int ReadyCount = 0;
				for (int i = 0; i < numPlayers; i++) { //check if all the players are ready but the host:
					if (lobbySlots [i].readyToBegin == true) {
						ReadyCount++;
					}
				}

                if (ReadyCount == numPlayers - 1) {
					lobbySlots [0].SendReadyToBeginMessage (); //send this ready message to start the game.
				} else {
					ShowInfoMsg ("Not all players are ready!", 1.0f);
				}
			} else {
				ShowInfoMsg ("Can't start the game with one player!", 1.0f);
			}
		}

        //reset the MP menu by hiding all the menus but the main one:
        public void ResetMPMenu()
        {
            if (MatchMakingMenu.activeInHierarchy == true)
            {
                DisableMatchMaking();
            }

            CanvasObj.gameObject.SetActive(true);
            LoadingMenu.gameObject.SetActive(false);
            MatchMakingMenu.gameObject.SetActive(false);
            MainMPMenu.gameObject.SetActive(true);
            LobbyMenu.gameObject.SetActive(false);
            HostMapMenu.gameObject.SetActive(false);

            IsHost = false;
        }

        //go bak to the main menu:
        public void MainMenu ()
		{
			if (MainMPMenu.gameObject.activeInHierarchy == true) {
				Destroy (this.gameObject);
				SceneManager.LoadScene (MainMenuScene);
			} else {
                if (LoadingMenu.gameObject.activeInHierarchy == true) {
                    LastDisconnectionType = DisconnectionTypes.Abort; //connection to lobby aborted
                    LeaveLoadingMenu ();
				}
				else {
                    LastDisconnectionType = DisconnectionTypes.Left; //player left the lobby
                    LeaveLobby ();
				}
			}
		}
	}
}