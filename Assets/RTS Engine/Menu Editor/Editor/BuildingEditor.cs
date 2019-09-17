using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using RTSEngine;

/* Building Editor script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

[CustomEditor(typeof(Building))]
[CanEditMultipleObjects]
public class BuildingEditor : Editor {

	public SerializedProperty BuildingStates;
	public SerializedProperty BuildingResources;
    public SerializedProperty BonusResources;
	public SerializedProperty DestroyAward;
	public SerializedProperty DropOffResourceList;
	public SerializedProperty FactionColors;
	public SerializedProperty ConstructionStates;
	public SerializedProperty UpgradeBuildingResources;
	public SerializedProperty UpgradeRequiredBuildings;
    public SerializedProperty RequiredBuildings;


	private ReorderableList TestList;

	public override void OnInspectorGUI ()
	{
		Building Target = (Building)target;

		GUIStyle TitleGUIStyle = new GUIStyle ();
		TitleGUIStyle.fontSize = 20;
		TitleGUIStyle.alignment = TextAnchor.MiddleCenter;
		TitleGUIStyle.fontStyle = FontStyle.Bold;

		EditorGUILayout.LabelField ("Building:", TitleGUIStyle);
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		TitleGUIStyle.fontSize = 15;
		EditorGUILayout.LabelField ("General Building Settings:", TitleGUIStyle);
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		Target.Name = EditorGUILayout.TextField ("Building Name: ", Target.Name);
		Target.Code = EditorGUILayout.TextField ("Building Code: ", Target.Code);
		Target.Category = EditorGUILayout.TextField ("Building Category: ", Target.Category);
		Target.Description = EditorGUILayout.TextField ("Building Description: ", Target.Description);
		EditorGUILayout.LabelField ("Building Icon:");
		Target.Icon = EditorGUILayout.ObjectField (Target.Icon, typeof(Sprite), true) as Sprite;
		Target.FreeBuilding = EditorGUILayout.Toggle ("Free Building? (Belongs to no faction)", Target.FreeBuilding);
		Target.CanBeAttacked = EditorGUILayout.Toggle ("Can Be Attacked? ", Target.CanBeAttacked);
		Target.TaskPanelCategory = EditorGUILayout.IntField ("Task Panel Category: ", Target.TaskPanelCategory);
	    Target.Radius = EditorGUILayout.FloatField ("Building Radius: ", Target.Radius);
		Target.FactionID = EditorGUILayout.IntField ("Faction ID: ", Target.FactionID);
        Target.AddPopulation = EditorGUILayout.IntField ("Population Slots To Add: ", Target.AddPopulation);

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Building Placement Settings:", TitleGUIStyle);
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        Target.PlacedByDefault = EditorGUILayout.Toggle("Placed by default?", Target.PlacedByDefault);
        Target.PlaceOutsideBorder = EditorGUILayout.Toggle("Place Outside Border?", Target.PlaceOutsideBorder);
        serializedObject.Update();
        RequiredBuildings = serializedObject.FindProperty("RequiredBuildings");
        EditorGUILayout.PropertyField(RequiredBuildings, true);
        serializedObject.ApplyModifiedProperties();
        Target.PlaceNearResource = EditorGUILayout.Toggle("Place Near Resource?", Target.PlaceNearResource);
        if (Target.PlaceNearResource == true)
        {
            Target.ResourceName = EditorGUILayout.TextField("Resource Name:", Target.ResourceName);
            Target.ResourceRange = EditorGUILayout.FloatField("Resource Range:", Target.ResourceRange);
        }

        EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		EditorGUILayout.LabelField ("Building Resource Settings:", TitleGUIStyle);
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		BuildingResources = serializedObject.FindProperty("BuildingResources");
		EditorGUILayout.PropertyField (BuildingResources, true);
		serializedObject.ApplyModifiedProperties();

		BonusResources = serializedObject.FindProperty("BonusResources");
		EditorGUILayout.PropertyField (BonusResources, true);
		serializedObject.ApplyModifiedProperties();

		Target.ResourceDropOff = EditorGUILayout.Toggle ("Is Resource Drop Off?", Target.ResourceDropOff);
		if (Target.ResourceDropOff == true) {
            EditorGUILayout.LabelField("Drop Off Position:");
            Target.DropOffPos = EditorGUILayout.ObjectField(Target.DropOffPos, typeof(Transform), true) as Transform;
			Target.AcceptAllResources = EditorGUILayout.Toggle ("Accept All Resources?", Target.AcceptAllResources);
			if (Target.AcceptAllResources == false) {
				DropOffResourceList = serializedObject.FindProperty("AcceptedResources");
				EditorGUILayout.PropertyField (DropOffResourceList, true);
				serializedObject.ApplyModifiedProperties();
			}
		}

		EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		EditorGUILayout.LabelField ("Building Health Settings:", TitleGUIStyle);
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		Target.MaxHealth = EditorGUILayout.FloatField ("Maximum Building Health: ", Target.MaxHealth);
        Target.HoverHealthBarY = EditorGUILayout.FloatField("Hover Health Bar Height: ", Target.HoverHealthBarY);
        Target.DestroyObj = EditorGUILayout.Toggle("Destroy Object?", Target.DestroyObj);
        Target.DestroyObjTime = EditorGUILayout.FloatField("Destroy Object Time: ", Target.DestroyObjTime);

        BuildingStates = serializedObject.FindProperty("BuildingStates");
		EditorGUILayout.LabelField ("Building States Parent Obj:");
		Target.BuildingStatesParent = EditorGUILayout.ObjectField (Target.BuildingStatesParent, typeof(GameObject), true) as GameObject;
		EditorGUILayout.PropertyField (BuildingStates, true);
		serializedObject.ApplyModifiedProperties();

		EditorGUILayout.LabelField ("Destruction Audio Clip:");
		Target.DestructionAudio = EditorGUILayout.ObjectField (Target.DestructionAudio, typeof(AudioClip), true) as AudioClip;
		EditorGUILayout.LabelField ("Destruction Effect Obj:");
		Target.DestructionEffect = EditorGUILayout.ObjectField (Target.DestructionEffect, typeof(EffectObj), true) as EffectObj;
		DestroyAward = serializedObject.FindProperty("DestroyAward");
		EditorGUILayout.PropertyField (DestroyAward, true);
		serializedObject.ApplyModifiedProperties();

		EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		EditorGUILayout.LabelField ("Building Upgrade Settings:", TitleGUIStyle);
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		Target.DirectUpgrade = EditorGUILayout.Toggle ("Upgrade Building Directly?", Target.DirectUpgrade);
		EditorGUILayout.LabelField ("Upgrade Building:");
		Target.UpgradeBuilding = EditorGUILayout.ObjectField (Target.UpgradeBuilding, typeof(Building), true) as Building;
		UpgradeBuildingResources = serializedObject.FindProperty("BuildingUpgradeResources");
		EditorGUILayout.PropertyField (UpgradeBuildingResources, true);
		serializedObject.ApplyModifiedProperties();
		UpgradeRequiredBuildings = serializedObject.FindProperty("UpgradeRequiredBuildings");
		EditorGUILayout.PropertyField (UpgradeRequiredBuildings, true);
		serializedObject.ApplyModifiedProperties();
		Target.BuildingUpgradeReload = EditorGUILayout.FloatField ("Building Upgrade Duration: ", Target.BuildingUpgradeReload);
		Target.UpgradeAllBuildings = EditorGUILayout.Toggle ("Upgrade All Buildings?", Target.UpgradeAllBuildings);

		EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		EditorGUILayout.LabelField ("Building Components:", TitleGUIStyle);
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

        EditorGUILayout.LabelField("Building Model:");
        Target.BuildingModel = EditorGUILayout.ObjectField(Target.BuildingModel, typeof(GameObject), true) as GameObject;
        EditorGUILayout.LabelField ("Building Plane:");
		Target.BuildingPlane = EditorGUILayout.ObjectField (Target.BuildingPlane, typeof(GameObject), true) as GameObject;
		EditorGUILayout.LabelField ("Building Selection Component:");
		Target.PlayerSelection = EditorGUILayout.ObjectField (Target.PlayerSelection, typeof(SelectionObj), true) as SelectionObj;
		EditorGUILayout.LabelField ("Construction Object:");
		Target.ConstructionObj = EditorGUILayout.ObjectField (Target.ConstructionObj, typeof(GameObject), true) as GameObject;
		ConstructionStates = serializedObject.FindProperty("ConstructionStates");
		EditorGUILayout.PropertyField (ConstructionStates, true);
		serializedObject.ApplyModifiedProperties();
		EditorGUILayout.LabelField ("Building Damage Effect:");
		Target.DamageEffect = EditorGUILayout.ObjectField (Target.DamageEffect, typeof(EffectObj), true) as EffectObj;
		EditorGUILayout.LabelField ("Units Spawn Position:");
		Target.SpawnPosition = EditorGUILayout.ObjectField (Target.SpawnPosition, typeof(Transform), true) as Transform;
		EditorGUILayout.LabelField ("Units Goto Position (right after spawning):");
		Target.GotoPosition = EditorGUILayout.ObjectField (Target.GotoPosition, typeof(Transform), true) as Transform;
		FactionColors = serializedObject.FindProperty("FactionColors");
		EditorGUILayout.PropertyField (FactionColors, true);
		serializedObject.ApplyModifiedProperties();

		EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		EditorGUILayout.LabelField ("Audio Clips:", TitleGUIStyle);
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		EditorGUILayout.LabelField ("Selection Sound Effect:");
		Target.SelectionAudio = EditorGUILayout.ObjectField (Target.SelectionAudio, typeof(AudioClip), true) as AudioClip;
        EditorGUILayout.LabelField("Upgrade Launch Sound Effect:");
        Target.UpgradeLaunchedAudio = EditorGUILayout.ObjectField(Target.UpgradeLaunchedAudio, typeof(AudioClip), true) as AudioClip;
        EditorGUILayout.LabelField("Upgrade Completed Sound Effect:");
        Target.UpgradeCompletedAudio = EditorGUILayout.ObjectField(Target.UpgradeCompletedAudio, typeof(AudioClip), true) as AudioClip;

        EditorUtility.SetDirty (Target);
	}
}
