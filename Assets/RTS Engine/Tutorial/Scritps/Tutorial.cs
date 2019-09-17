using UnityEngine;
using System.Collections;

public class Tutorial : MonoBehaviour {

	public GameObject TutorialObject;
	public GameObject MoveForward;
	public GameObject MoveBackward;

	public GameObject[] TutorialMsg;
	public int TutorialID;

	public void ToggleTutorialMenu ()
	{
		TutorialObject.SetActive (!TutorialObject.activeInHierarchy);
		if (TutorialObject.activeInHierarchy == true) {
			MoveTutorial (0);
		}
	}
	public void MoveTutorial (int ID)
	{
		if (TutorialID + ID >= 0 && TutorialID+ ID < TutorialMsg.Length) {
			TutorialMsg [TutorialID].SetActive (false);
			TutorialID += ID;
			TutorialMsg [TutorialID].SetActive (true);

			if (TutorialID == TutorialMsg.Length - 1) {
				MoveForward.SetActive (false);
			} else {
				MoveForward.SetActive (true);
			}
			if (TutorialID == 0) {
				MoveBackward.SetActive (false);
			} else {
				MoveBackward.SetActive (true);
			}
		}
	}
}
