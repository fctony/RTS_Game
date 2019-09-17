using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RTSEngine;

[CustomEditor(typeof(ResourceTypeInfo))]
public class ResourceTypeInfoEditor : Editor {

    ResourceTypeInfo Target;
    SerializedObject SOTarget;

    //GUI Style:
    GUILayoutOption[] SmallButtonLayout;

    public void OnEnable()
    {
        SmallButtonLayout = new GUILayoutOption[] { GUILayout.Width(20.0f), GUILayout.Height(20.0f) };

        Target = (ResourceTypeInfo)target;

        SOTarget = new SerializedObject(Target);
    }

    public override void OnInspectorGUI()
    {
        SOTarget.Update(); //Always update the Serialized Object.
 
        ResourceSettings();

        SOTarget.ApplyModifiedProperties(); //Apply all modified properties always at the end of this method.

        EditorUtility.SetDirty(target);
    }

    void ResourceSettings()
    {
        Target.Name = EditorGUILayout.TextField("Name", Target.Name);
        Target.StartingAmount = EditorGUILayout.IntField("Starting Amount", Target.StartingAmount);

        Target.Icon = EditorGUILayout.ObjectField("Icon", Target.Icon, typeof(Sprite), false) as Sprite;
        Target.MinimapIconColor = EditorGUILayout.ColorField("Minimap Color", Target.MinimapIconColor);

        Target.SelectionAudio = EditorGUILayout.ObjectField("Selection Audio Clip", Target.SelectionAudio, typeof(AudioClip), false) as AudioClip;

        EditorGUILayout.Space();

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Collection Audio Clips");
        EditorGUILayout.Space();
        if (GUILayout.Button("+", SmallButtonLayout))
        {
            Target.CollectionAudio.Add(null);
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (Target.CollectionAudio.Count > 0)
        {
            for (int i = 0; i < Target.CollectionAudio.Count; i++)
            {
                GUILayout.BeginHorizontal();
                Target.CollectionAudio[i] = EditorGUILayout.ObjectField(Target.CollectionAudio[i], typeof(AudioClip), false) as AudioClip;
                if (GUILayout.Button("-", SmallButtonLayout))
                {
                    Target.CollectionAudio.Remove(Target.CollectionAudio[i]);
                }
                GUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No collection audio clips defined for this resource type", MessageType.Warning);
        }
    }
}
