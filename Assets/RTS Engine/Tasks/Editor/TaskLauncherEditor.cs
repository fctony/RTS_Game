using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RTSEngine;

/* Task Launcher Editor script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

[CustomEditor(typeof(TaskLauncher))]
public class TaskLauncherEditor : Editor
{
    TaskLauncher Target;
    SerializedObject SOTarget;
    TaskLauncher.TasksVars CurrentTask;
    string TaskPath;
    string TabName;

    //General serialized properties (applicable to all task types):
    SerializedProperty TaskResources;

    //Unit creation task type only serialized properties:
    SerializedProperty CreateUnit_Prefabs;
    SerializedProperty Upgrade_TargetPrefabs;
    SerializedProperty Upgrade_Resources;
    SerializedProperty Upgrade_NewResources;

    bool ShowUpgrades;

    //Unit research task type only serialized properites:
    SerializedProperty Research_Prefabs;

    //Properties for the task events:
    SerializedProperty Event_Launch;
    SerializedProperty Event_Complete;
    SerializedProperty Event_Cancel;

    GUIStyle TitleGUIStyle = new GUIStyle();

    public void OnEnable ()
    {
        Target = (TaskLauncher)target;

        SOTarget = new SerializedObject(Target);
    }

    void TaskLauncherSettings ()
    {
        //Task Launcher settings:
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Task Launcher Settings:", TitleGUIStyle);
        EditorGUILayout.Space();

        Target.Code = EditorGUILayout.TextField("Task Launcher Code:", Target.Code);
        Target.MinTaskHealth = EditorGUILayout.FloatField("Min Health (to launch task):", Target.MinTaskHealth);
        Target.MaxTasks = EditorGUILayout.IntField("Max Allowed Active Tasks:", Target.MaxTasks);
        Target.LaunchTaskAudio = EditorGUILayout.ObjectField ("Launch Task Sound Effect:", Target.LaunchTaskAudio, typeof(AudioClip), true) as AudioClip;
        Target.DeclinedTaskAudio = EditorGUILayout.ObjectField ("Declined Task Sound Effect:", Target.DeclinedTaskAudio, typeof(AudioClip), true) as AudioClip;
    }

    void GeneralTaskSettings ()
    {
        CurrentTask.Code = EditorGUILayout.TextField("Code:", CurrentTask.Code);
        CurrentTask.FactionSpecific = EditorGUILayout.Toggle("Faction Specific?", CurrentTask.FactionSpecific);
        if (CurrentTask.FactionSpecific == true)
        {
            CurrentTask.FactionCode = EditorGUILayout.TextField("Faction Code:", CurrentTask.FactionCode);
        }

        CurrentTask.AllowedTaskType = (TaskLauncher.AllowedTaskTypes)EditorGUILayout.EnumPopup("Task Type:", CurrentTask.AllowedTaskType);
        switch (CurrentTask.AllowedTaskType)
        {
            case TaskLauncher.AllowedTaskTypes.CreateUnit:
                CurrentTask.TaskType = TaskManager.TaskTypes.CreateUnit;
                break;
            case TaskLauncher.AllowedTaskTypes.Research:
                CurrentTask.TaskType = TaskManager.TaskTypes.Research;
                break;
            case TaskLauncher.AllowedTaskTypes.Destroy:
                CurrentTask.TaskType = TaskManager.TaskTypes.Destroy;
                break;
            case TaskLauncher.AllowedTaskTypes.CustomTask:
                CurrentTask.TaskType = TaskManager.TaskTypes.CustomTask;
                break;
            default:
                break;
        }

        CurrentTask.Description = EditorGUILayout.TextField("Description:", CurrentTask.Description);
        CurrentTask.TaskPanelCategory = EditorGUILayout.IntField("Task Panel Category:", CurrentTask.TaskPanelCategory);
        CurrentTask.TaskIcon = EditorGUILayout.ObjectField("Icon:", CurrentTask.TaskIcon, typeof(Sprite), true) as Sprite;
        CurrentTask.ReloadTime = EditorGUILayout.FloatField("Reload Time:", CurrentTask.ReloadTime);

        TaskResources = SOTarget.FindProperty(TaskPath).FindPropertyRelative("RequiredResources");
        if(TaskResources != null)
            EditorGUILayout.PropertyField(TaskResources, true);

        CurrentTask.TaskCompletedAudio = EditorGUILayout.ObjectField("On Complete Audio:", CurrentTask.TaskCompletedAudio, typeof(AudioClip), true) as AudioClip;

        CurrentTask.UseOnce = EditorGUILayout.Toggle("One-time use?", CurrentTask.UseOnce);
    }

    void CreateUnitSettings ()
    {
        EditorGUILayout.LabelField("Unit Creation Settings:", TitleGUIStyle);
        EditorGUILayout.HelpBox("The prefabs list allows multiple unit prefabs to be added but only one (randomly selected) will be created each time.", MessageType.Warning);
        EditorGUILayout.Space();

        CreateUnit_Prefabs = SOTarget.FindProperty(TaskPath).FindPropertyRelative("UnitCreationSettings.Prefabs");
        EditorGUILayout.PropertyField(CreateUnit_Prefabs, true);

        EditorGUILayout.Space();

        ShowUpgrades = EditorGUILayout.Foldout(ShowUpgrades, "Upgrades");

        if (ShowUpgrades == true)
        {

            if (GUILayout.Button("Add Upgrade (Upgrade Tasks Count: " + CurrentTask.UnitCreationSettings.Upgrades.Count + ")"))
            {
                TaskManager.UnitCreationTask.UnitUpgradeSubTask NewUpgrade = new TaskManager.UnitCreationTask.UnitUpgradeSubTask();
                CurrentTask.UnitCreationSettings.Upgrades.Add(NewUpgrade);

                Target.UpgradeID = CurrentTask.UnitCreationSettings.Upgrades.Count - 1;
            }

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("<<"))
                {
                    Move(ref Target.UpgradeID, -1, CurrentTask.UnitCreationSettings.Upgrades.Count);
                }
                if (GUILayout.Button(">>"))
                {
                    Move(ref Target.UpgradeID, 1, CurrentTask.UnitCreationSettings.Upgrades.Count);
                }
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (CurrentTask.UnitCreationSettings.Upgrades.Count > 0)
            {
                EditorGUILayout.LabelField("Upgrade ID: " + Target.UpgradeID.ToString(), TitleGUIStyle);
                EditorGUILayout.Space();

                string UpgradePath = TaskPath + ".UnitCreationSettings.Upgrades.Array.data[" + Target.UpgradeID.ToString() + "]";

                TaskManager.UnitCreationTask.UnitUpgradeSubTask CurrentUpgrade = CurrentTask.UnitCreationSettings.Upgrades[Target.UpgradeID];

                EditorGUILayout.HelpBox("The upgrade will have its own independent task in the task panel, following settings are for that task.", MessageType.Info);
                EditorGUILayout.HelpBox("There will be only one upgrade available to be launched for a single task at a time, each time an upgrade task is completed, the next one in the list will be available.", MessageType.Warning);
                EditorGUILayout.Space();

                Upgrade_TargetPrefabs = SOTarget.FindProperty(UpgradePath).FindPropertyRelative("TargetPrefabs");
                EditorGUILayout.PropertyField(Upgrade_TargetPrefabs, true);

                CurrentUpgrade.UpgradeDescription = EditorGUILayout.TextField("Upgrade Description:", CurrentUpgrade.UpgradeDescription);
                CurrentUpgrade.TaskPanelCategory = EditorGUILayout.IntField("Upgrade Task Panel Category:", CurrentUpgrade.TaskPanelCategory);
                CurrentUpgrade.UpgradeIcon = EditorGUILayout.ObjectField("Upgrade Icon:", CurrentUpgrade.UpgradeIcon, typeof(Sprite), true) as Sprite;
                CurrentUpgrade.UpgradeReload = EditorGUILayout.FloatField("Upgrade Reload Time:", CurrentUpgrade.UpgradeReload);

                Upgrade_Resources = SOTarget.FindProperty(UpgradePath).FindPropertyRelative("UpgradeResources");
                if (Upgrade_Resources != null)
                    EditorGUILayout.PropertyField(Upgrade_Resources, true);

                EditorGUILayout.Space();

                //New Task settings:
                EditorGUILayout.HelpBox("The configurations below will replace the task's default configurations after the upgrade is done.", MessageType.Info);

                CurrentUpgrade.NewTaskDescription = EditorGUILayout.TextField("New Description:", CurrentUpgrade.NewTaskDescription);
                CurrentUpgrade.NewTaskIcon = EditorGUILayout.ObjectField("New Icon:", CurrentUpgrade.NewTaskIcon, typeof(Sprite), true) as Sprite;
                CurrentUpgrade.NewReloadTime = EditorGUILayout.FloatField("New Reload Time:", CurrentUpgrade.NewReloadTime);

                Upgrade_NewResources = SOTarget.FindProperty(UpgradePath).FindPropertyRelative("NewTaskResources");
                if (Upgrade_NewResources != null)
                    EditorGUILayout.PropertyField(Upgrade_NewResources, true);

                EditorGUILayout.Space();
                if (GUILayout.Button("Remove Upgrade"))
                {
                    CurrentTask.UnitCreationSettings.Upgrades.RemoveAt(Target.UpgradeID);
                    if (Target.UpgradeID > 0)
                        Move(ref Target.UpgradeID, -1, CurrentTask.UnitCreationSettings.Upgrades.Count);
                }

            }
        }
    }

    void ResearchSettings ()
    {
        EditorGUILayout.LabelField("Unit Research Settings:", TitleGUIStyle);
        EditorGUILayout.Space();

        Research_Prefabs = SOTarget.FindProperty(TaskPath).FindPropertyRelative("UnitResearchSettings.Prefabs");
        EditorGUILayout.PropertyField(Research_Prefabs, true);

        EditorGUILayout.HelpBox("The value in the fields below will be added to the already existing attributes for the units in the list above.", MessageType.Info);
        EditorGUILayout.Space();

        CurrentTask.UnitResearchSettings.AddSpeed = EditorGUILayout.FloatField("Add Speed:", CurrentTask.UnitResearchSettings.AddSpeed);
        CurrentTask.UnitResearchSettings.AddUnitDamage = EditorGUILayout.FloatField("Add Unit Damage:", CurrentTask.UnitResearchSettings.AddUnitDamage);
        CurrentTask.UnitResearchSettings.AddBuildingDamage = EditorGUILayout.FloatField("Add Building Damage:", CurrentTask.UnitResearchSettings.AddBuildingDamage);
        CurrentTask.UnitResearchSettings.AddAttackReload = EditorGUILayout.FloatField("Add Attack Reload:", CurrentTask.UnitResearchSettings.AddAttackReload);
        CurrentTask.UnitResearchSettings.AddSearchRange = EditorGUILayout.FloatField("Add Search Range:", CurrentTask.UnitResearchSettings.AddSearchRange);
        CurrentTask.UnitResearchSettings.AddMaxHealth = EditorGUILayout.FloatField("Add Max Health:", CurrentTask.UnitResearchSettings.AddMaxHealth);
    }

    void EventsSettings ()
    {
        EditorGUILayout.HelpBox("In addition to the delegate events (which are called for all tasks), you can trigger events for this task independently.", MessageType.Info);
        EditorGUILayout.Space();

        Event_Launch = SOTarget.FindProperty(TaskPath).FindPropertyRelative("TaskLaunchEvent");
        EditorGUILayout.PropertyField(Event_Launch, true);

        Event_Complete = SOTarget.FindProperty(TaskPath).FindPropertyRelative("TaskCompleteEvent");
        EditorGUILayout.PropertyField(Event_Complete, true);

        Event_Cancel = SOTarget.FindProperty(TaskPath).FindPropertyRelative("TaskCancelEvent");
        EditorGUILayout.PropertyField(Event_Cancel, true);
    }

    void TaskSettings ()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Task Settings:", TitleGUIStyle);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox("Go through tasks, add/remove tasks using the buttons below.", MessageType.Info);
        EditorGUILayout.Space();

        if (GUILayout.Button("Add Task (Task Count: " + Target.TasksList.Count + ")"))
        {
            TaskLauncher.TasksVars NewTask = new TaskLauncher.TasksVars();
            Target.TasksList.Add(NewTask);

            Target.TaskID = Target.TasksList.Count - 1;
        }

        GUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("<<"))
            {
                Move(ref Target.TaskID, -1, Target.TasksList.Count);
            }
            if (GUILayout.Button(">>"))
            {
                Move(ref Target.TaskID, 1, Target.TasksList.Count);
            }
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        //making sure there tasks to begin with:
        if (Target.TasksList.Count > 0)
        {
            //Tasks:
            //task to display:
            CurrentTask = Target.TasksList[Target.TaskID];
            TaskPath = "TasksList.Array.data[" + Target.TaskID.ToString() + "]";

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Task ID: " + Target.TaskID.ToString(), TitleGUIStyle);
            EditorGUILayout.Space();

            TitleGUIStyle.alignment = TextAnchor.MiddleLeft;
            TitleGUIStyle.fontStyle = FontStyle.Bold;

            EditorGUI.BeginChangeCheck();

            Target.TabID = GUILayout.Toolbar(Target.TabID, new string[] { "General Settings", "Custom Settings", "Events" });

            switch (Target.TabID)
            {
                case 0:
                    TabName = "General Settings";
                    break;
                case 1:
                    //The following settings will be shown depending on the task's type:
                    TabName = "Custom Settings";
                    break;

                case 2:
                    TabName = "Events";
                    break;
                default:
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                SOTarget.ApplyModifiedProperties();
                GUI.FocusControl(null);
            }

            EditorGUILayout.Space();

            switch (TabName)
            {
                case "General Settings":
                    GeneralTaskSettings();
                    break;
                case "Custom Settings":
                    //The following settings will be shown depending on the task's type:
                    switch (CurrentTask.TaskType)
                    {
                        case TaskManager.TaskTypes.CreateUnit: //Unit creation task:

                            CreateUnitSettings();

                            break;

                        case TaskManager.TaskTypes.Research: //Unit resarch task:

                            ResearchSettings();

                            break;

                        default:
                            EditorGUILayout.HelpBox("No custom settings for the current task type.", MessageType.None);

                            break;
                    }
                    break;

                case "Events":
                    EventsSettings();

                    break;
                default:
                    break;
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            if (GUILayout.Button("Delete Task"))
            {
                Target.TasksList.RemoveAt(Target.TaskID);
                if (Target.TaskID > 0)
                    Move(ref Target.TaskID, -1, Target.TasksList.Count);
            }
        }

        else
        {
            EditorGUILayout.HelpBox("There are no tasks, create one using the button above.", MessageType.Warning);
        }
    }

    public override void OnInspectorGUI()
    {
        SOTarget.Update(); //Always update the Serialized Object.

        TitleGUIStyle.fontSize = 13;
        TitleGUIStyle.alignment = TextAnchor.MiddleCenter;
        TitleGUIStyle.fontStyle = FontStyle.Bold;

        TaskLauncherSettings();

        TaskSettings();

        SOTarget.ApplyModifiedProperties(); //Apply all modified properties always at the end of this method.
    }

    void Move (ref int ID, int Step, int Max)
    {
        if (ID + Step >= 0 && ID + Step < Max)
        {
            ID += Step;
        }
    }
}
