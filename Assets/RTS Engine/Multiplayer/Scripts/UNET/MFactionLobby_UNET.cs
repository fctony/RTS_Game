using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.UI;

/* Multiplayer Faction Lobby Info: script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
	public class MFactionLobby_UNET : NetworkLobbyPlayer {

		[HideInInspector]
        [SyncVar]
		public int FactionID = 0; //holds the faction ID associated to the player.
		[HideInInspector]
		public string FactionName = "New Faction"; //holds the player's faction name.
        [HideInInspector]
        public int FactionTypeID = 0; //the faction type's ID.
        [HideInInspector]
		public Color FactionColor = Color.blue; //player faction's color
		[HideInInspector]
		public int FactionColorID = 0;
        [HideInInspector]
        [SyncVar]
        public bool IsServer; //Is this the server/host?
        [SyncVar]
        string GameVersion; //hold's the server's game version

		//UI Info:
		public Image ColorImg; //showing the faction's color
		public InputField FactionNameInput; //input/show the faction's name
		public GameObject ReadyToBeginButton; //to announce that the player is to ready or not
		public Image ReadyImage; //the image to show when the player is ready
		public Dropdown FactionTypeMenu; //UI Dropdown used to display the list of possible faction types that can be used in the currently selected maps.
		public GameObject KickButton; //the button that the host can use to kick this player.

		[HideInInspector]
		public NetworkManager_UNET NetworkMgr;

		void Start ()
		{
            //get the lobby manager script
            NetworkMgr = NetworkManager_UNET.instance;

			//if it's a local player:
			if (isLocalPlayer)
			{
                //if this is the server:
                if (isServer)
                {
                    GameVersion = NetworkMgr.GameVersion; //set the game version
                    IsServer = true;
                    KickButton.SetActive(false);
                }

                //set the local faction
                SetupLocalFaction();
                CmdAddPlayerToList (FactionID);
            }
			else
			{
				//if it's not the local player:
				SetupOtherFaction();
			}

			SetObjParent (); //set the lobby info object's parent.

			//Show the kick buttons for the server only:
			if (!isServer) {
				KickButton.SetActive (false);
			}

            if (IsServer == true) //if this component belongs to the host
            { //checking if this is the host's faction
              //make sure that the kick button is not activated and that the host can't announce he's ready or not, only him can launch the game when all other players are ready.
                ReadyToBeginButton.SetActive(false);

                //if this player's game version is different from the host's game version
                if (NetworkMgr.GameVersion != GameVersion)
                {
                    //get kicked for not having the same game version
                    CmdKickPlayer(true);
                }

            }
        }

        //method called by the server to sync factions stats for new players who recently joined the room
		[Command]
		public void CmdAddPlayerToList (int NewFactionID)
		{
            //the server will share all the lobby settings for the new player
            foreach (NetworkLobbyPlayer NetworkPlayer in NetworkMgr.lobbySlots) //go through all the lobby slots
            {
                if (NetworkPlayer != null)
                {
                    MFactionLobby_UNET LobbyPlayer = NetworkPlayer.gameObject.GetComponent<MFactionLobby_UNET>();
                    //remove any slots for players who disconnected and are not anymore present in the lobby
                    if (LobbyPlayer != null)
                    {
                        if (LobbyPlayer.FactionID != NewFactionID) //making sure that this is not the new player that just joined
                        {
                            LobbyPlayer.CmdServerUpdateMap(); //ask server to share the current map ID
                            LobbyPlayer.CmdUpdateFactionColor(LobbyPlayer.FactionColorID);
                            LobbyPlayer.CmdUpdateFactionName(LobbyPlayer.FactionName);
                            LobbyPlayer.CmdUpdateFactionType(LobbyPlayer.FactionTypeID);
                            LobbyPlayer.CmdUpdateReadyStatus();
                        }
                    }
                }
            }
		}

		//setting up the local faction:
		public void SetupLocalFaction ()
		{
			FactionNameInput.interactable = true; //only the player can change his faction's name
			FactionTypeMenu.interactable = true; //only the player can pick his faction type.
			//Hide the multiplayer main menu:
			NetworkMgr.LoadingMenu.gameObject.SetActive(false);
			NetworkMgr.MainMPMenu.gameObject.SetActive (false);
			NetworkMgr.MatchMakingMenu.gameObject.SetActive (false);
			NetworkMgr.HostMapMenu.gameObject.SetActive (true);
            //Show the lobby menu:
            NetworkMgr.LobbyMenu.gameObject.SetActive(true);

			NetworkMgr.LocalFactionLobbyInfo = this; //link the lobby manager with the local players

			NetworkMgr.MapDropDownMenu.value = NetworkMgr.CurrentMapID; //set the map on the map drop down menu
			NetworkMgr.UpdateMapUIInfo (); //and update the map's UI.

            FactionName = "New Faction";
			FactionNameInput.text = FactionName;
            FactionColorID = 0;
			ColorImg.color = NetworkMgr.AllowedColors[FactionColorID]; //set its color.

			SetMapInfo ();
			ResetFactionTypes ();
		}


		//when the faction does not belong to the local player:
		public void SetupOtherFaction ()
		{
			FactionNameInput.interactable = false; //can't change its name
			FactionTypeMenu.interactable = false; //can't change the faction type.
			FactionNameInput.text = FactionName;
			ColorImg.color = NetworkMgr.AllowedColors[FactionColorID]; //set its color.
			ReadyImage.gameObject.SetActive (readyToBegin); //check if it's ready or not
			ResetFactionTypes ();
		}

		//setting the map info:
		public void SetMapInfo ()
		{
			if (isServer) { //if this is the server:
				NetworkMgr.MapDropDownMenu.interactable = true; //then make the player able to pick the map
				NetworkMgr.StartGameButton.SetActive (true); //activate the start game button because only the host can launch a game

				//initial map settings:
				NetworkMgr.CurrentMapID = 0;
				NetworkMgr.MapDropDownMenu.value = NetworkMgr.CurrentMapID;

			} else {
				//if this is not the server, then the player can't start the game or change the map:
				NetworkMgr.MapDropDownMenu.interactable = false;
				NetworkMgr.StartGameButton.SetActive (false);
			}
		}

		//this method sets the lobby info object parent to one chosen in the lobby manager:
		public void SetObjParent ()
		{
			if (NetworkMgr.LobbyPlayerParent != null) {
				transform.SetParent (NetworkMgr.LobbyPlayerParent);
				gameObject.GetComponent<RectTransform> ().localScale = new Vector3 (1.0f, 1.0f, 1.0f);
			}
		}

		//updating the map info:
		public void UpdateMapInfo()
		{
			CmdServerUpdateMap (); //ask the server
		}

		[Command]
		public void CmdServerUpdateMap ()
		{
			RpcUpdateMapInfo (NetworkMgr.MapDropDownMenu.value); //update the map info for all players
		}

		[ClientRpc]
		public void RpcUpdateMapInfo (int ID)
		{
            NetworkMgr.MapDropDownMenu.value = ID; //set the new map ID

            NetworkMgr.UpdateMapUIInfo (); //update the map info from lobby manager

            //the server will share all the lobby settings for the new player
            foreach (NetworkLobbyPlayer NetworkPlayer in NetworkMgr.lobbySlots) //go through all the lobby slots
            {
                if (NetworkPlayer != null)
                {
                    MFactionLobby_UNET LobbyPlayer = NetworkPlayer.gameObject.GetComponent<MFactionLobby_UNET>();
                    //do not consider any slots for players who disconnected and are not anymore present in the lobby
                    if (LobbyPlayer != null)
                    {
                        LobbyPlayer.ResetFactionTypes(); //reset factions
                    }
                }
            }
		}

		public void ResetFactionTypes ()
		{
			List<string> FactionTypes = new List<string> ();
			if (NetworkMgr.Maps [NetworkMgr.CurrentMapID].FactionTypes.Length > 0) { //if there are actually faction types to choose from:
				for(int i = 0; i < NetworkMgr.Maps [NetworkMgr.CurrentMapID].FactionTypes.Length ; i++) //create a list with the names with all possible faction types:
				{
					FactionTypes.Add (NetworkMgr.Maps [NetworkMgr.CurrentMapID].FactionTypes [i].Name);
				}
			}

			FactionTypeMenu.ClearOptions (); //clear all the faction type options.
			if (FactionTypes.Count > 0) {
				//Add the faction types' names as options:
				FactionTypeMenu.AddOptions(FactionTypes);
				FactionTypeMenu.value = 0;
                FactionTypeID = 0;
			}
        }

		//Toggle the ready status of the player:
		public void ToggleReadyStatus()
		{
			if (isLocalPlayer && !isServer) { //if this is not the server and this belongs to a local player:
				if (ReadyImage.gameObject.activeInHierarchy == false) { //if the ready image is not active then make the player ready
					SendReadyToBeginMessage ();
				} else {
					SendNotReadyToBeginMessage (); //and vice versa
				}
				CmdUpdateReadyStatus (); //ask the server to update the ready status of this faction to all players:
			}
		}

		[Command]
		public void CmdUpdateReadyStatus ()
		{
			//update the ready status for all players
			RpcUpdateReadyStatus ();
		}

		[ClientRpc]
		public void RpcUpdateReadyStatus ()
		{
			ReadyImage.gameObject.SetActive (readyToBegin); //Show the ready image  or hide it depending on the ready status of the player.
		}

		//Updating the faction name:
		public void OnFactionNameChange ()
		{
			if (isLocalPlayer) { //only if it's the local player:
				if (FactionNameInput.text != "") { //and the new name is valid
                    CmdUpdateFactionName (FactionNameInput.text); //ask the server to update it
				}
			}
		}
		//update the faction's name to all players:
		[ClientRpc]
		public void RpcUpdateFactionName (string Value)
		{
			FactionNameInput.text = Value;
            FactionName = Value;
        }

        [Command]
		public void CmdUpdateFactionName(string Value)
		{
			RpcUpdateFactionName (Value);
		}

		//Updating the faction name:
		public void OnFactionTypeChange ()
		{
			if (isLocalPlayer) { //only if it's the local player:
				if (FactionTypeMenu.value < NetworkMgr.Maps[NetworkMgr.CurrentMapID].FactionTypes.Length) { //if the new type is valid
					//update it:
					CmdUpdateFactionType(FactionTypeMenu.value);
				}
			}
		}

		//update the faction's type to all players:
		[ClientRpc]
		public void RpcUpdateFactionType (int ID)
		{
			FactionTypeMenu.value = ID;
            FactionTypeID = ID;
        }

		[Command]
		public void CmdUpdateFactionType (int ID)
		{
            RpcUpdateFactionType (ID);
		}

		//Updating the faction color:
		public void OnFactionColorChange ()
		{
			if (isLocalPlayer) { //only if it's the local player

				//change the faction's color ID
				if (NetworkMgr.AllowedColors.Length - 1 > FactionColorID) {
					FactionColorID++;
				} else {
					FactionColorID = 0;
				}

				//ask the server to update the color for all players:
				CmdUpdateFactionColor (FactionColorID);
			}
		}

		[ClientRpc]
		public void RpcUpdateFactionColor (int ColorID)
		{
            FactionColor = NetworkMgr.AllowedColors[ColorID];
            FactionColorID = ColorID;
            ColorImg.color = FactionColor; //update the color
		}

		[Command]
		public void CmdUpdateFactionColor(int ColorID)
		{
			//send a message to all players to update this faction's color:
			RpcUpdateFactionColor (ColorID);
		}

		//player kick:
        [Command]
		public void CmdKickPlayer (bool IsGameVersion) //this method allows the host to kick other players:
		{
            //kick the player:
            RpcOnKicked(IsGameVersion);
		}

        [ClientRpc]
        public void RpcOnKicked(bool IsGameVersion)
        {
            if (isLocalPlayer == false) //only to the target faction ID
                return;

            //register the disconnection reason
            NetworkMgr.LastDisconnectionType = (IsGameVersion == true) ? NetworkManager_UNET.DisconnectionTypes.GameVersion : NetworkManager_UNET.DisconnectionTypes.Kicked;
            //make player leave lobby:
            NetworkMgr.LeaveLobby();
        }
    }
}