using UnityEngine;
using System.Collections;
using UnityEngine.Networking.Match;
using UnityEngine.UI;

/* Internet Match Info: script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
	public class InternetMatch_UNET : MonoBehaviour {

        //this script handles joining internet matches
		[HideInInspector]
		public int ID; //the ID of the internet match is stored here.

		public Text MatchName; //name of the match
		public Text MatchSize; //max amount of players in the game

        NetworkManager_UNET NetworkMgr;

		void Start () {
            NetworkMgr = NetworkManager_UNET.instance;
		}

        //method to launch an internet match
		public void JoinInternetMatch ()
		{
			NetworkMgr.JoinInternetMatch (ID);
		}
	}
}