using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RTSEngine;

[CustomEditor(typeof(InputManager))]
public class InputManagerEditor : Editor
{

    public override void OnInspectorGUI()
    {
        //draw the default inspector as well
        DrawDefaultInspector();

        InputManager Target = (InputManager)target;

        if (GUILayout.Button("Update Spawnable Prefabs"))
        {
            Target.SpawnablePrefabs.Clear();

            Object[] Objects = Resources.LoadAll("Prefabs", typeof(GameObject));
            foreach (GameObject Obj in Objects)
            {
                if (!Target.SpawnablePrefabs.Contains(Obj.gameObject))
                {
                    if (Obj.gameObject.GetComponent<Building>() || Obj.gameObject.GetComponent<Unit>() || Obj.gameObject.GetComponent<Resource>())
                    {
                        Target.SpawnablePrefabs.Add(Obj.gameObject);
                    }
                }
            }

            Debug.Log("Spawnable Prefabs list updated.");
        }
        if (GUILayout.Button("Reset Spawnable Prefabs"))
        {
            Target.SpawnablePrefabs.Clear();
            Debug.Log("Spawnable Prefabs list cleared.");
        }

        EditorUtility.SetDirty(target);
    }
}
