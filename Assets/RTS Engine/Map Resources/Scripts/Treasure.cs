using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
	public class Treasure : MonoBehaviour {

		public ResourceManager.Resources[] Resources; //the resources to give the faction when they claim this treasure.
		public AudioClip ClaimedAudio; //audio played when the reward is claimed by a unit;
		public GameObject ClaimedEffect; //effect spawned when the reward is claimed.
	}
}