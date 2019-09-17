using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using RTSEngine;

[CustomEditor(typeof(ResourceManager))]
public class ResourceManagerEditor : Editor {

	public override void OnInspectorGUI ()
	{
		//draw the default inspector as well
		DrawDefaultInspector ();

		EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		ResourceManager Target = (ResourceManager)target;

		if (GUILayout.Button ("Generate Resources List")) {

            Target.AllResources.Clear();

            Target.AllResources.AddRange(Target.ResourcesParent.transform.GetComponentsInChildren<Resource>(true));
            if(Target.AllResources.Count > 0)
            {
                foreach(Resource Element in Target.AllResources)
                {
                    Element.FactionID = -1;
                    Element.WorkerMgr = Element.gameObject.GetComponent<WorkerManager>();
                }
            }

            Debug.Log("Resource List generated.");
        }
    }
}
