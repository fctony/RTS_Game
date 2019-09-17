using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RTSEngine;

/* NPC Manager Editor script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

[CustomEditor(typeof(NPCManager))]
public class NPCManagerEditor : Editor {

    NPCManager targetNPCMgr;
    SerializedObject target_SO;

    private string regulatorFolderPath;

    GUIStyle titleGUIStyle = new GUIStyle();

    public void OnEnable()
    {
        targetNPCMgr = (NPCManager)target;

        target_SO = new SerializedObject(target);

        //can't have a '/' at the end of the prefab path.
        if (targetNPCMgr.prefabPath[targetNPCMgr.prefabPath.Length - 1] == '/')
            targetNPCMgr.prefabPath = targetNPCMgr.prefabPath.Remove(targetNPCMgr.prefabPath.Length - 1);

        regulatorFolderPath = targetNPCMgr.prefabPath + "/" + targetNPCMgr.code + "_regulators";
    }

    public override void OnInspectorGUI()
    {
        target_SO.Update(); //Always update the Serialized Object.

        titleGUIStyle.fontSize = 13;
        titleGUIStyle.alignment = TextAnchor.MiddleCenter;
        titleGUIStyle.fontStyle = FontStyle.Bold;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("NPC Manager", titleGUIStyle);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox("The 'Name' field will be used in the menus when picking a NPC manager/difficulty level for a NPC faction.", MessageType.Info);
        targetNPCMgr.Name = EditorGUILayout.TextField("Name: ", targetNPCMgr.Name);
        targetNPCMgr.code = EditorGUILayout.TextField("Code: ", targetNPCMgr.code);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("The 'Prefab Path' field is simply the NPC manager's prefab path & where the unit/building regulators will be generated.", MessageType.Info);
        targetNPCMgr.prefabPath = EditorGUILayout.TextField("Prefab Path: ", targetNPCMgr.prefabPath);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("The button below will create/refresh the unit/building regulators by scanning the unit/building prefabs.", MessageType.Info);
        //a button that refreshes the NPC Manager's building/unit regulators:
        if (GUILayout.Button("Refresh Regulators"))
        {
            //does the regulators folder not exist?
            if(AssetDatabase.IsValidFolder(regulatorFolderPath) == false)
                AssetDatabase.CreateFolder(targetNPCMgr.prefabPath, targetNPCMgr.code + "_regulators"); //create it.

            //go through all files in the Resources/Prefabs path.
            Object[] objects = Resources.LoadAll("Prefabs", typeof(GameObject));

            //go through the prefabs objects
            foreach (GameObject obj in objects)
            {
                CreateUnitRegulator(obj.GetComponent<Unit>()); //if this object is a unit prefab, a unit regulator will be created.
                CreateBuildingRegulator(obj.GetComponent<Building>()); //if this object is building prefab a unit regulator will be created.
            }
        }

        target_SO.ApplyModifiedProperties(); //Apply all modified properties always at the end of this method.
    }

    //creates a unit regulator asset
    void CreateUnitRegulator (Unit unit)
    {
        if (unit == null)
            return;

        string folderPath = regulatorFolderPath + "/Unit Regulators";
        //does the unit regulator folder not exist?
        if (AssetDatabase.IsValidFolder(folderPath) == false)
            AssetDatabase.CreateFolder(regulatorFolderPath, "Unit Regulators"); //create it.

        //see if the regulator already exists by going through the already created NPC Unit Regulators:
        int i = 0;
        while(i < targetNPCMgr.unitRegulatorAssets.Count)
        {
            NPCUnitRegulator nur = targetNPCMgr.unitRegulatorAssets[i];
            if (nur != null) //if the unit regulator is valid
            {
                //if there are actual prefabs in this regulator:
                if (nur.prefabs.Count > 0)
                    if (nur.prefabs[0].Code == unit.Code) //if it has the same code with this unit
                    {
                        //if the prefab isn't already in:
                        if (nur.prefabs.Contains(unit) == false)
                        {
                            //simply add it as a new prefab in the same regulator:
                            nur.prefabs.Add(unit);
                            EditorUtility.SetDirty(nur);
                        }
                        return; //do not proceed.
                    }
                i++;
            }
            else //regulator isn't valid:
            {
                //remove it:
                targetNPCMgr.unitRegulatorAssets.RemoveAt(i);
            }
        }

        NPCUnitRegulator newRegulator = ScriptableObject.CreateInstance<NPCUnitRegulator>();
        AssetDatabase.CreateAsset(newRegulator, folderPath + "/" + unit.Name + "Regulator_"+targetNPCMgr.code+".asset"); //create an asset file for it.
        newRegulator.prefabs.Add(unit); //add the unit.
        EditorUtility.SetDirty(newRegulator);

        //add to list:
        targetNPCMgr.unitRegulatorAssets.Add(newRegulator);
    }

    //creates a building regulator asset
    void CreateBuildingRegulator(Building building)
    {
        //if the building is invalid then do not proceed.
        if (building == null)
            return;

        string folderPath = regulatorFolderPath + "/Building Regulators";
        //does the building regulator folder not exist?
        if (AssetDatabase.IsValidFolder(folderPath) == false)
            AssetDatabase.CreateFolder(regulatorFolderPath, "Building Regulators"); //create it.

        //see if the regulator already exists by going through the already created NPC Building Regulators:
        int i = 0;
        while (i < targetNPCMgr.buildingRegulatorAssets.Count)
        {
            NPCBuildingRegulator nbr = targetNPCMgr.buildingRegulatorAssets[i];
            if (nbr != null) //if the unit regulator is valid
            {
                //if there are actual prefabs in this regulator:
                if (nbr.prefabs.Count > 0)
                    if (nbr.prefabs[0].Code == building.Code) //if it has the same code with this building
                    {
                        //if the prefab isn't already in:
                        if (nbr.prefabs.Contains(building) == false)
                        {
                            //simply add it as a new prefab in the same regulator:
                            nbr.prefabs.Add(building);
                            EditorUtility.SetDirty(nbr);
                        }
                        return; //do not proceed.
                    }
                i++;
            }
            else //invalid building regulator
            {
                targetNPCMgr.buildingRegulatorAssets.RemoveAt(i); //remove it.
            }
        }

        //reaching this stage means we need to create a new one.
        NPCBuildingRegulator newRegulator = ScriptableObject.CreateInstance<NPCBuildingRegulator>(); //new instance of the building regulator
        AssetDatabase.CreateAsset(newRegulator, folderPath + "/" + building.Name + "Regulator_" + targetNPCMgr.code + ".asset"); //create an asset file for it.
        newRegulator.prefabs.Add(building); //add the building.

        EditorUtility.SetDirty(newRegulator);

        //add to list:
        targetNPCMgr.buildingRegulatorAssets.Add(newRegulator);
    }
}
