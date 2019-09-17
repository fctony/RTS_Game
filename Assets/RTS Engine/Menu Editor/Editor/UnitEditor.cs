using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RTSEngine;

/* Unit Editor script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

[CustomEditor(typeof(Unit))]
[CanEditMultipleObjects]
public class UnitEditor : Editor {

	public SerializedProperty UnitColors1;
	public SerializedProperty UnitColors2;
	public SerializedProperty DestroyAward;

	public override void OnInspectorGUI ()
	{
		Unit Target = (Unit)target;

		GUIStyle TitleGUIStyle = new GUIStyle ();
		TitleGUIStyle.fontSize = 20;
		TitleGUIStyle.alignment = TextAnchor.MiddleCenter;
		TitleGUIStyle.fontStyle = FontStyle.Bold;

		EditorGUILayout.LabelField ("Unit:", TitleGUIStyle);
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		TitleGUIStyle.fontSize = 15;
		EditorGUILayout.LabelField ("General Unit Settings:", TitleGUIStyle);
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		Target.Name = EditorGUILayout.TextField ("Unit Name: ", Target.Name);
		Target.Code = EditorGUILayout.TextField ("Unit Code: ", Target.Code);
		Target.Category = EditorGUILayout.TextField ("Unit Category: ", Target.Category);
		Target.Description = EditorGUILayout.TextField ("Unit Description: ", Target.Description);
		EditorGUILayout.LabelField ("Unit Icon:");
		Target.Icon = EditorGUILayout.ObjectField (Target.Icon, typeof(Sprite), true) as Sprite;
        Target.CanBeConverted = EditorGUILayout.Toggle("Can the unit be converted?", Target.CanBeConverted);
        Target.FreeUnit = EditorGUILayout.Toggle ("Free Unit? (Belongs to no faction)", Target.FreeUnit);
		Target.FactionID = EditorGUILayout.IntField ("Faction ID: ", Target.FactionID);
		Target.UnitHeight = EditorGUILayout.FloatField ("Unit Height: ", Target.UnitHeight);
		Target.FlyingUnit = EditorGUILayout.Toggle ("Air Unit:", Target.FlyingUnit);

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Unit Health:", TitleGUIStyle);
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        Target.MaxHealth = EditorGUILayout.FloatField ("Maximum Unit Health: ", Target.MaxHealth);
        Target.HoverHealthBarY = EditorGUILayout.FloatField("Hover Health Bar Height: ", Target.HoverHealthBarY);
        Target.DestroyObj = EditorGUILayout.Toggle("Destroy Object?", Target.DestroyObj);
        Target.DestroyObjTime = EditorGUILayout.FloatField ("Destroy Object Time: ", Target.DestroyObjTime);
		DestroyAward = serializedObject.FindProperty("DestroyAward");
		EditorGUILayout.PropertyField (DestroyAward, true);
		serializedObject.ApplyModifiedProperties();
        EditorGUILayout.LabelField("Destruction Audio Clip:");
        Target.DestructionAudio = EditorGUILayout.ObjectField(Target.DestructionAudio, typeof(AudioClip), true) as AudioClip;
        EditorGUILayout.LabelField("Destruction Effect Obj:");
        Target.DestructionEffect = EditorGUILayout.ObjectField(Target.DestructionEffect, typeof(EffectObj), true) as EffectObj;

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Taking Damage:", TitleGUIStyle);
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        Target.TakeDamage = EditorGUILayout.Toggle("Take Damage?", Target.TakeDamage);
        Target.StopMvtOnTakeDamage = EditorGUILayout.Toggle("Stop Mvt On Take Damage?", Target.StopMvtOnTakeDamage);
        Target.EnableTakeDamageAnim = EditorGUILayout.Toggle("Play Take Damage Animation?", Target.EnableTakeDamageAnim);
        Target.TakeDamageDuration = EditorGUILayout.FloatField("Take Damage Duration:", Target.TakeDamageDuration);

        EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		EditorGUILayout.LabelField ("Unit Movement Settings:", TitleGUIStyle);
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

        Target.CanBeMoved = EditorGUILayout.Toggle("Can the unit be moved?", Target.CanBeMoved);
        Target.Speed = EditorGUILayout.FloatField ("Movement Speed: ", Target.Speed);
        Target.CanRotate = EditorGUILayout.Toggle("Rotate while idle?", Target.CanRotate);
        Target.RotationDamping = EditorGUILayout.FloatField ("Rotation Damping: ", Target.RotationDamping);

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Wandering:", TitleGUIStyle);
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        Target.CanWander = EditorGUILayout.Toggle("Can Wander?", Target.CanWander);
        if (Target.CanWander == true)
        {
            Target.WanderByDefault = EditorGUILayout.Toggle("Wander By Default?", Target.WanderByDefault);
            Target.FixedWanderCenter = EditorGUILayout.Toggle("Fixed Wander Center?", Target.FixedWanderCenter);
            Target.WanderRange = EditorGUILayout.Vector2Field("Wander Range: ", Target.WanderRange);
            Target.WanderReloadRange = EditorGUILayout.Vector2Field("Wander Reload Range: ", Target.WanderReloadRange);
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Escape On Attack:", TitleGUIStyle);
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        Target.EscapeOnAttack = EditorGUILayout.Toggle("Escape On Attack?", Target.EscapeOnAttack);
        if (Target.EscapeOnAttack == true)
        {
            Target.EscapeRange = EditorGUILayout.Vector2Field("Escape Range: ", Target.EscapeRange);
            Target.EscapeSpeed = EditorGUILayout.FloatField("Escape Speed: ", Target.EscapeSpeed);
            EditorGUILayout.LabelField("Escape Animator Controller:");
            Target.EscapeAnimOverride = EditorGUILayout.ObjectField(Target.EscapeAnimOverride, typeof(AnimatorOverrideController), true) as AnimatorOverrideController;
        }

        EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		EditorGUILayout.LabelField ("Unit Components:", TitleGUIStyle);
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

        EditorGUILayout.LabelField("Unit Model:");
        Target.UnitModel = EditorGUILayout.ObjectField(Target.UnitModel, typeof(GameObject), true) as GameObject;
        EditorGUILayout.LabelField ("Unit Animator:");
		Target.AnimMgr = EditorGUILayout.ObjectField (Target.AnimMgr, typeof(Animator), true) as Animator;
		EditorGUILayout.LabelField ("Unit Plane:");
		Target.UnitPlane = EditorGUILayout.ObjectField (Target.UnitPlane, typeof(GameObject), true) as GameObject;
		EditorGUILayout.LabelField ("Unit Selection Component:");
		Target.PlayerSelection = EditorGUILayout.ObjectField (Target.PlayerSelection, typeof(SelectionObj), true) as SelectionObj;
		EditorGUILayout.LabelField ("Default Animator Override Controller:");
		Target.AnimOverrideController = EditorGUILayout.ObjectField (Target.AnimOverrideController, typeof(AnimatorOverrideController), true) as AnimatorOverrideController;
		EditorGUILayout.LabelField ("Unit Damage Effect:");
		Target.DamageEffect = EditorGUILayout.ObjectField (Target.DamageEffect, typeof(EffectObj), true) as EffectObj;
        EditorGUILayout.LabelField("Target Position Collider:");
        Target.TargetPosColl = EditorGUILayout.ObjectField(Target.TargetPosColl, typeof(Collider), true) as Collider;
        EditorGUILayout.LabelField ("Color Objects (Skinned Mesh Renderers only):");
		UnitColors1 = serializedObject.FindProperty("FactionColorObjs");
		EditorGUILayout.PropertyField (UnitColors1, true);
		serializedObject.ApplyModifiedProperties();
		EditorGUILayout.LabelField ("Color Objects (Mesh Renderers only):");
		UnitColors2 = serializedObject.FindProperty("FactionColorObjs2");
		EditorGUILayout.PropertyField (UnitColors2, true);
		serializedObject.ApplyModifiedProperties();

		EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		EditorGUILayout.LabelField ("Audio Clips:", TitleGUIStyle);
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		EditorGUILayout.LabelField ("Selection Sound Effect:");
		Target.SelectionAudio = EditorGUILayout.ObjectField (Target.SelectionAudio, typeof(AudioClip), true) as AudioClip;
		EditorGUILayout.LabelField ("Movement Order Sound Effect:");
		Target.MvtOrderAudio = EditorGUILayout.ObjectField (Target.MvtOrderAudio, typeof(AudioClip), true) as AudioClip;
        EditorGUILayout.LabelField("Movement Sound Effect:");
        Target.MvtAudio = EditorGUILayout.ObjectField(Target.MvtAudio, typeof(AudioClip), true) as AudioClip;
        EditorGUILayout.LabelField ("Invalid Movement Path Sound Effect:");
		Target.InvalidMvtPathAudio = EditorGUILayout.ObjectField (Target.InvalidMvtPathAudio, typeof(AudioClip), true) as AudioClip;

        EditorUtility.SetDirty (Target);
	}
}
