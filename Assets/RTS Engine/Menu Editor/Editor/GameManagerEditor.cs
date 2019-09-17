using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RTSEngine;

/* Game Manager Editor script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor {

	int FactionID; //current faction that the user is configuring.

	public override void OnInspectorGUI ()
	{
		GameManager Target = (GameManager)target;

		GUIStyle TitleGUIStyle = new GUIStyle ();
		TitleGUIStyle.fontSize = 20;
		TitleGUIStyle.alignment = TextAnchor.MiddleCenter;
		TitleGUIStyle.fontStyle = FontStyle.Bold;

		EditorGUILayout.LabelField ("Factions:", TitleGUIStyle);
		EditorGUILayout.Space ();

		if (GUILayout.Button ("Add Faction (Faction Count: " + Target.Factions.Count + ")")) {
			GameManager.FactionInfo NewFaction = new GameManager.FactionInfo ();
			Target.Factions.Add (NewFaction);

			FactionID = Target.Factions.Count - 1;
		}


		EditorGUILayout.Space ();
		EditorGUILayout.HelpBox ("Make sure to create the maximum amount that this map can handle. When fewer factions play on the map, the rest will be automatically removed.", MessageType.Info);

		EditorGUILayout.Space ();
		if (GUILayout.Button (">>")) {
			ChangeFactionID (1, Target.Factions.Count);
		}
		if (GUILayout.Button ("<<")) {
			ChangeFactionID (-1, Target.Factions.Count);
		}

		EditorGUILayout.Space ();
		TitleGUIStyle.fontSize = 15;
		EditorGUILayout.LabelField ("Faction ID " + FactionID.ToString (), TitleGUIStyle);
		EditorGUILayout.Space ();


		Target.Factions [FactionID].Name = EditorGUILayout.TextField ("Faction Name", Target.Factions [FactionID].Name);
        Target.Factions[FactionID].TypeInfo = EditorGUILayout.ObjectField("Faction Type", Target.Factions[FactionID].TypeInfo, typeof(FactionTypeInfo), false) as FactionTypeInfo;

        Target.Factions [FactionID].FactionColor = EditorGUILayout.ColorField ("Faction Color", Target.Factions [FactionID].FactionColor);

        Target.Factions[FactionID].FactionMgr = EditorGUILayout.ObjectField("HA3", Target.Factions[FactionID].FactionMgr, typeof(FactionManager), true) as FactionManager;

        Target.Factions [FactionID].playerControlled = EditorGUILayout.Toggle ("Player Controlled", Target.Factions [FactionID].playerControlled);
		EditorGUILayout.HelpBox ("Make sure that only one team is controlled the player.", MessageType.Info);

		Target.Factions [FactionID].maxPopulation = EditorGUILayout.IntField ("Initial Maximum Population", Target.Factions [FactionID].maxPopulation);
		Target.Factions [FactionID].CapitalBuilding = EditorGUILayout.ObjectField ("Capital Building", Target.Factions [FactionID].CapitalBuilding, typeof(Building), true) as Building;
        Target.Factions[FactionID].npcMgr = EditorGUILayout.ObjectField("NPC Manager",Target.Factions[FactionID].npcMgr, typeof(NPCManager), true) as NPCManager;

        EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		if (GUILayout.Button ("Remove Faction")) {
			if (Target.Factions.Count > 2) {
                Target.Factions.RemoveAt(FactionID);
                ChangeFactionID(-1, Target.Factions.Count);
            } else {
				Debug.LogError ("The minimum amount of factions to have in one map is: 2!");
			}
		}

		EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		EditorGUILayout.Space (); 
		TitleGUIStyle.fontSize = 20;
		EditorGUILayout.LabelField ("General Settings:", TitleGUIStyle);
		EditorGUILayout.Space ();

		Target.randomPlayerFaction = EditorGUILayout.Toggle ("Random Player Faction", Target.randomPlayerFaction);
		Target.MainMenuScene = EditorGUILayout.TextField ("Main Menu Scene", Target.MainMenuScene);
		Target.PeaceTime = EditorGUILayout.FloatField ("Peace Time (seconds)", Target.PeaceTime);
		EditorGUILayout.LabelField ("General Audio Source");
		Target.GeneralAudioSource = EditorGUILayout.ObjectField (Target.GeneralAudioSource, typeof(AudioSource), true) as AudioSource;

		EditorUtility.SetDirty (Target);
	}

	public void ChangeFactionID (int Value, int Max)
	{
		int ProjectedID = FactionID + Value;
		if (ProjectedID < Max && ProjectedID >= 0) {
			FactionID = ProjectedID;
		}
	}
}
