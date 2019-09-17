using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RTSEngine;

/* Attack Editor script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

[CustomEditor(typeof(Attack))]
public class AttackEditor : Editor
{
    public SerializedProperty CodesList;
    public SerializedProperty AttackSources;
    public SerializedProperty AreaDamage;
    public SerializedProperty CustomDamage;

    public override void OnInspectorGUI()
    {
        Attack Target = (Attack)target;

        GUIStyle TitleGUIStyle = new GUIStyle();
        TitleGUIStyle.fontSize = 20;
        TitleGUIStyle.alignment = TextAnchor.MiddleCenter;
        TitleGUIStyle.fontStyle = FontStyle.Bold;

        EditorGUILayout.LabelField("Attack Component:", TitleGUIStyle);
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        TitleGUIStyle.fontSize = 15;
        EditorGUILayout.LabelField("General Attack Settings:", TitleGUIStyle);
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        Target.AttackerType = (Attack.AttackerTypes)EditorGUILayout.EnumPopup("Attacker:", Target.AttackerType);
        Target.AttackCode = EditorGUILayout.TextField("Attack Code: ", Target.AttackCode);
        Target.AttackPower = EditorGUILayout.IntField("Attack Power:", Target.AttackPower);
        EditorGUILayout.LabelField("Attack Icon:");
        Target.AttackIcon = EditorGUILayout.ObjectField(Target.AttackIcon, typeof(Sprite), true) as Sprite;
        Target.BasicAttack = EditorGUILayout.Toggle("Basic Attack?", Target.BasicAttack);
        Target.AttackReload = EditorGUILayout.FloatField("Attack Reload", Target.AttackReload);
        Target.AttackAllTypes = EditorGUILayout.Toggle("Attack All Types?", Target.AttackAllTypes);
        if (Target.AttackAllTypes == false)
        {
            Target.AttackUnits = EditorGUILayout.Toggle("Attack Units?", Target.AttackUnits);
            Target.AttackBuildings = EditorGUILayout.Toggle("Attack Buildings?", Target.AttackBuildings);
            Target.AttackOnlyInList = EditorGUILayout.Toggle("Attack Only In (below) List: ", Target.AttackOnlyInList);
            CodesList = serializedObject.FindProperty("CodesList");
            EditorGUILayout.PropertyField(CodesList, true);
            serializedObject.ApplyModifiedProperties();
        }
        EditorGUILayout.Space();
        Target.RangeType = EditorGUILayout.TextField("Range Type Code:", Target.RangeType);

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Attack In Range Settings:", TitleGUIStyle);
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        Target.AttackInRange = EditorGUILayout.Toggle("Attack In Range?", Target.AttackInRange);
        if (Target.AttackInRange == true)
        {
            Target.SearchRange = EditorGUILayout.FloatField("Search Range:", Target.SearchRange);
            Target.SearchReload = EditorGUILayout.FloatField("Search Reload:", Target.SearchReload);
        }

        if (Target.AttackerType == Attack.AttackerTypes.Unit)
        {
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Attack Unit Settings:", TitleGUIStyle);
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            Target.AttackOnAssign = EditorGUILayout.Toggle("Attack On Assign?", Target.AttackOnAssign);
            Target.AttackWhenAttacked = EditorGUILayout.Toggle("Attack When Attacked?", Target.AttackWhenAttacked);
            Target.AttackOnce = EditorGUILayout.Toggle("Attack Once?", Target.AttackOnce);
            Target.MoveOnAttack = EditorGUILayout.Toggle("Move On Attack?", Target.MoveOnAttack);
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            Target.FollowRange = EditorGUILayout.FloatField("Follow Distance:", Target.FollowRange);
            EditorGUILayout.Space();
            Target.PlayAnimInDelay = EditorGUILayout.Toggle("Play Animation In Delay?", Target.PlayAnimInDelay);
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Attack Damage Settings:", TitleGUIStyle);
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        Target.DealDamage = EditorGUILayout.Toggle("Deal Damage?", Target.DealDamage);
        EditorGUILayout.Space();
        Target.AreaDamage = EditorGUILayout.Toggle("Area Damage?", Target.AreaDamage);
        if (Target.AreaDamage == true)
        {
            AreaDamage = serializedObject.FindProperty("AttackRanges");
            EditorGUILayout.PropertyField(AreaDamage, true);
            serializedObject.ApplyModifiedProperties();
        }
        else
        {
            Target.BuildingDamage = EditorGUILayout.FloatField("Building Damage:", Target.BuildingDamage);
            Target.UnitDamage = EditorGUILayout.FloatField("Unit Damage:", Target.UnitDamage);

            CustomDamage = serializedObject.FindProperty("CustomDamage");
            EditorGUILayout.PropertyField(CustomDamage, true);
            serializedObject.ApplyModifiedProperties();
        }
        EditorGUILayout.Space();
        //Damage over time:
        Target.DoT.Enabled = EditorGUILayout.Toggle("Damage Over Time?", Target.DoT.Enabled);
        if (Target.DoT.Enabled == true)
        {
            Target.DoT.Infinite = EditorGUILayout.Toggle("Infinite DoT?", Target.DoT.Infinite);
            Target.DoT.Duration = EditorGUILayout.FloatField("DoT Duration", Target.DoT.Duration);
            Target.DoT.Cycle = EditorGUILayout.FloatField("DoT Cycle", Target.DoT.Cycle);
        }
        EditorGUILayout.Space();
        Target.ReloadDamageDealt = EditorGUILayout.Toggle("Reload Dealt Damage:", Target.ReloadDamageDealt);

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Attack Delay/Cooldown:", TitleGUIStyle);
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        Target.DelayTime = EditorGUILayout.FloatField("Attack Delay Time:", Target.DelayTime);
        Target.UseDelayTrigger = EditorGUILayout.Toggle("Use Delay Trigger?", Target.UseDelayTrigger);

        EditorGUILayout.Space();

        //Attack cool down:
        Target.EnableCoolDown = EditorGUILayout.Toggle("Use cooldown?", Target.EnableCoolDown);
        if (Target.EnableCoolDown == true)
        {
            Target.CoolDown = EditorGUILayout.FloatField("Cooldown duration:", Target.CoolDown);
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Direct Attack/Attack Object:", TitleGUIStyle);
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        Target.DirectAttack = EditorGUILayout.Toggle("Direct Attack?", Target.DirectAttack);
        EditorGUILayout.Space();
        if (Target.DirectAttack == false)
        {
            Target.AttackType = (Attack.AttackTypes)EditorGUILayout.EnumPopup("Attack Type:", Target.AttackType);

            EditorGUILayout.Space();
      
            AttackSources = serializedObject.FindProperty("AttackSources");
            EditorGUILayout.PropertyField(AttackSources, true);
            serializedObject.ApplyModifiedProperties();
        }

        EditorGUILayout.LabelField("Attack Effect:");
        Target.AttackEffect = EditorGUILayout.ObjectField(Target.AttackEffect, typeof(EffectObj), true) as EffectObj;
        Target.AttackEffectTime = EditorGUILayout.FloatField("Attack Effect Duration:", Target.AttackEffectTime);

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Attack Weapon:", TitleGUIStyle);
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Weapon Object:");
        Target.WeaponObj = EditorGUILayout.ObjectField(Target.WeaponObj, typeof(GameObject), true) as GameObject;
        Target.FreezeRotX = EditorGUILayout.Toggle("Freeze Rotation on X?", Target.FreezeRotX);
        Target.FreezeRotY = EditorGUILayout.Toggle("Freeze Rotation on Y?", Target.FreezeRotY);
        Target.FreezeRotZ = EditorGUILayout.Toggle("Freeze Rotation on Z?", Target.FreezeRotZ);
        Target.SmoothRotation = EditorGUILayout.Toggle("Smooth Rotation?", Target.SmoothRotation);
        if (Target.SmoothRotation == true)
        {
            Target.RotationDamping = EditorGUILayout.FloatField("Rotation Damping: ", Target.RotationDamping);
        }
        Target.ForceIdleRotation = EditorGUILayout.Toggle("Force Idle Rotation?", Target.ForceIdleRotation);
        if (Target.ForceIdleRotation == true)
        {
            Target.WeaponIdleAngles = EditorGUILayout.Vector3Field("Weapon Idle Angle: ", Target.WeaponIdleAngles);
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Line Of Sight (LOS):", TitleGUIStyle);
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        Target.EnableLOS = EditorGUILayout.Toggle("Enable LOS?", Target.EnableLOS);
        if (Target.EnableLOS == true)
        {
            Target.WeaponObjInLOS = EditorGUILayout.Toggle("Use Weapon in LOS?", Target.WeaponObjInLOS);
            Target.LOSAngle = EditorGUILayout.FloatField("Allowed LOS Value:", Target.LOSAngle);
            Target.IgnoreLOSX = EditorGUILayout.Toggle("Ingore axis X in LOS?", Target.IgnoreLOSX);
            Target.IgnoreLOSY = EditorGUILayout.Toggle("Ingore axis Y in LOS?", Target.IgnoreLOSY);
            Target.IgnoreLOSZ = EditorGUILayout.Toggle("Ingore axis Z in LOS?", Target.IgnoreLOSZ);
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Attack Audio Settings:", TitleGUIStyle);
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Attack Order Audio:");
        Target.AttackOrderSound = EditorGUILayout.ObjectField(Target.AttackOrderSound, typeof(AudioClip), true) as AudioClip;
        EditorGUILayout.LabelField("Attack Audio:");
        Target.AttackSound = EditorGUILayout.ObjectField(Target.AttackSound, typeof(AudioClip), true) as AudioClip;

        EditorUtility.SetDirty(Target);
    }
}
