using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/* Custom Network Discovery: script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    //Extension of the Network Discovery component that allows rooms to be discovered across a local network.
	public class NetDiscovery_UNET : NetworkDiscovery {
		public NetworkManager_UNET NetworkMgr; //The network manager goes here.
		public bool Connected = false; //is the user connected to a local game or not?

        //when the player is looking for a game to join in a local network, this method will be called...
        //...when the player receives a broadcast from another address (annoucing that there's a game hosted in that address).
		public override void OnReceivedBroadcast (string fromAddress, string data)
		{
			if (Connected == false) { //if the player is not connected to a local room yet
                //connect to the local room via the network manager
				NetworkMgr.JoinLocalGame(fromAddress);
				Connected = true; //player is now marked as connected.
			}
		}
	}
}