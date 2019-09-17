using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RTSEngine;

public class MenuItems : MonoBehaviour {

	[MenuItem("RTS Engine/Configure New Map", false, 51)]
	private static void ConfigNewMapOption()
	{
		GameObject MapSettingsClone = Instantiate(Resources.Load("NewMap", typeof(GameObject))) as GameObject;

		if (MapSettingsClone != null) {
			for (int i = MapSettingsClone.transform.childCount-1; i >= 0; i--) {
				MapSettingsClone.transform.GetChild (0).SetParent (null, true);
			}
		}

		DestroyImmediate (MapSettingsClone);

        print("Please set up the factions in order to fully configure the new map: http://soumidelrio.com/docs/unity-rts-engine/game-manager/");
	}

	[MenuItem("RTS Engine/Single Player Menu", false, 101)]
	private static void SinglePlayerMenuOption()
	{
		GameObject SinglePlayerMenu = Instantiate(Resources.Load("SinglePlayerMenu", typeof(GameObject))) as GameObject;

		if (SinglePlayerMenu != null) {
			for (int i = SinglePlayerMenu.transform.childCount-1; i >= 0; i--) {
				SinglePlayerMenu.transform.GetChild (0).SetParent (null, true);
			}
		}

		DestroyImmediate (SinglePlayerMenu);
	}

	[MenuItem("RTS Engine/Multiplayer Menu", false, 102)]
	private static void MultiplayerMenuMenu()
	{
		GameObject MultiPlayerMenu = Instantiate(Resources.Load("MultiPlayerMenu", typeof(GameObject))) as GameObject;

		if (MultiPlayerMenu != null) {
			for (int i = MultiPlayerMenu.transform.childCount-1; i >= 0; i--) {
				MultiPlayerMenu.transform.GetChild (0).SetParent (null, true);
			}
		}

		DestroyImmediate (MultiPlayerMenu);
	}

    [MenuItem("RTS Engine/New Unit", false, 151)]
    private static void NewUnitOption()
    {
        Instantiate(Resources.Load("NewUnit", typeof(GameObject)));
    }

    [MenuItem("RTS Engine/New Building", false, 152)]
    private static void NewBuildingOption()
    {
        Instantiate(Resources.Load("NewBuilding", typeof(GameObject)));
    }

    [MenuItem("RTS Engine/New Resource", false, 153)]
    private static void NewResourceOption()
    {
        Instantiate(Resources.Load("NewResource", typeof(GameObject)));
    }

    [MenuItem("RTS Engine/New NPC Manager", false, 154)]
    private static void NewNPCManager()
    {
        Instantiate(Resources.Load("NewNPCManager", typeof(GameObject)));
    }

    [MenuItem("RTS Engine/Documentation", false, 201)]
    private static void DocOption()
    {
        Application.OpenURL("http://soumidelrio.com/docs/unity-rts-engine/");
    }
    [MenuItem("RTS Engine/Review", false, 202)]
    private static void ReviewOption()
    {
        Application.OpenURL("https://assetstore.unity.com/packages/templates/packs/rts-engine-79732");
    }
}
