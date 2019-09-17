using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RTSEngine;

[CustomEditor(typeof(FactionTypeInfo))]
public class FactionTypeInfoEditor : Editor {

    FactionTypeInfo Target;
    SerializedObject SOTarget;

    //GUI Style:
    GUILayoutOption[] SmallButtonLayout;

    public void OnEnable()
    {
        SmallButtonLayout = new GUILayoutOption[] { GUILayout.Width(20.0f), GUILayout.Height(20.0f) };

        Target = (FactionTypeInfo)target;

        SOTarget = new SerializedObject(Target);
    }

    public override void OnInspectorGUI()
    {
        SOTarget.Update(); //Always update the Serialized Object.

        FactionSettings();

        SOTarget.ApplyModifiedProperties(); //Apply all modified properties always at the end of this method.

        EditorUtility.SetDirty(target);
    }

    void FactionSettings ()
    {
        Target.Name = EditorGUILayout.TextField("Name", Target.Name);
        Target.Code = EditorGUILayout.TextField("Code", Target.Code);

        EditorGUILayout.Space();

        Target.capitalBuilding = EditorGUILayout.ObjectField("Capital Building", Target.capitalBuilding, typeof(Building), true) as Building;
        EditorGUILayout.Space();

        Target.centerBuilding = EditorGUILayout.ObjectField("Center Building", Target.centerBuilding, typeof(Building), true) as Building;
        EditorGUILayout.Space();

        Target.populationBuilding = EditorGUILayout.ObjectField("Population Building", Target.populationBuilding, typeof(Building), true) as Building;
        EditorGUILayout.Space();

        EditorGUILayout.Space();

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Extra Buildings");
        EditorGUILayout.Space();
        if (GUILayout.Button("+", SmallButtonLayout))
        {
            Target.extraBuildings.Add(new Building());
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (Target.extraBuildings.Count > 0)
        {
            for (int i = 0; i < Target.extraBuildings.Count; i++)
            {
                GUILayout.BeginHorizontal();
                Target.extraBuildings[i] = EditorGUILayout.ObjectField(Target.extraBuildings[i], typeof(Building), true) as Building;
                if (GUILayout.Button("-", SmallButtonLayout))
                {
                    Target.extraBuildings.Remove(Target.extraBuildings[i]);
                }
                GUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No extra buildings defined for this faction type", MessageType.Warning);
        }

        EditorGUILayout.Space();

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Limits");
        EditorGUILayout.Space();
        if (GUILayout.Button("+", SmallButtonLayout))
        {
            FactionTypeInfo.FactionLimitsVars NewLimitElement = new FactionTypeInfo.FactionLimitsVars();

            Target.Limits.Add(NewLimitElement);
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (Target.Limits.Count > 0)
        {
            for (int i = 0; i < Target.Limits.Count; i++)
            {
                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical();
                Target.Limits[i].Code = EditorGUILayout.TextField("Code", Target.Limits[i].Code);
                Target.Limits[i].MaxAmount = EditorGUILayout.IntField("Max Amount", Target.Limits[i].MaxAmount);
                GUILayout.EndVertical();

                if (GUILayout.Button("-", SmallButtonLayout))
                {
                    Target.Limits.Remove(Target.Limits[i]);
                }
                GUILayout.EndHorizontal();

                EditorGUILayout.Space();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No building/unit limits defined for this faction type", MessageType.Warning);
        }
    }
}
