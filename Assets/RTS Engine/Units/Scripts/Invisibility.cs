using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/* Invisibility script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
	public class Invisibility : MonoBehaviour {

		public float Duration = 5.0f; //maximum time a unit is allowed to stay invisible.
		float Timer;

		//effected materials:
		public SkinnedMeshRenderer[] AffectedMaterials;

		//alpha material main color when invisible:
		public float InvisibleAlphaColor = 0.4f;

		//sprties:
		public Sprite GoInvisibleSprite;
		public Sprite GoVisibleSprite;
		public int InvisibilityTasksCategory = 0;

		//audio clips:
		public AudioClip GoInvisibleAudio;
		public AudioClip GoVisibleAudio;

		[Header("While invisible:")]
		//list of things that the unit can or can not do while being invisible:
		public bool CanAttack = false;
		public bool CanCollect = false;
		public bool CanBuild = false;
		public bool CanConvert = false;
		public bool CanHeal = false;

        Unit RefUnit;

        void Start () {
            RefUnit = GetComponent<Unit> ();
		}

		void Update ()
		{
            //invisibility timer:
			if (Timer > 0) {
                Timer -= Time.deltaTime;
			}
			if (Timer < 0) {
                Timer = 0.0f;

				ToggleInvisibility ();
			}
		}

        public void ToggleInvisibility()
        {
            if (GameManager.MultiplayerGame == true)
            { //if this is a MP game and it's the local player:
                if (GameManager.Instance.IsLocalPlayer(gameObject))
                {
                    //send input action to the input manager
                    InputVars NewInputAction = new InputVars();
                    //mode:
                    NewInputAction.SourceMode = (byte)InputSourceMode.CustomCommand;
                    NewInputAction.TargetMode = (byte)InputCustomMode.Invisibility;

                    NewInputAction.Source = gameObject;

                    InputManager.SendInput(NewInputAction);
                }
            }
            else
            {
                //offline game? update the attack type directly:
                ToggleInvisibilityLocal();
            }
        }

        public void ToggleInvisibilityLocal ()
		{
			RefUnit.IsInvisible = !RefUnit.IsInvisible;

			if (RefUnit.IsInvisible == true) {
				Timer = Duration;
				AudioManager.PlayAudio (RefUnit.GameMgr.GeneralAudioSource.gameObject, GoVisibleAudio, false);

				//check what the unit is doing and take action:
				if (CanAttack == false) {
					if (RefUnit.AttackMgr) {
						if (RefUnit.AttackMgr.AttackTarget != null) {
							RefUnit.CancelAttack ();
						}
					}
				}
				if (CanBuild == false) {
					if (RefUnit.BuilderMgr) {
						if (RefUnit.BuilderMgr.TargetBuilding != null) {
							RefUnit.CancelBuilding ();
						}
					}
				}
				if (CanConvert == false) {
					if (RefUnit.ConvertMgr) {
						if (RefUnit.ConvertMgr.TargetUnit != null) {
							RefUnit.CancelConverting ();
						}
					}
				}
				if (CanCollect == false) {
					if (RefUnit.ResourceMgr) {
						if (RefUnit.ResourceMgr.TargetResource != null) {
							RefUnit.CancelCollecting ();
						}
					}
				}
				if (CanHeal == false) {
					if (RefUnit.HealMgr) {
						if (RefUnit.HealMgr.TargetUnit != null) {
							RefUnit.CancelHealing ();
						}
					}
				}

				float AlphaColor = (GameManager.PlayerFactionID == RefUnit.FactionID) ? InvisibleAlphaColor : 0.0f;

				//set the aplha color on affected materials:
				for (int i = 0; i < AffectedMaterials.Length; i++) {
					if (AffectedMaterials [i]) {
						AffectedMaterials [i].material.color = new Color (AffectedMaterials [i].material.color.r, AffectedMaterials [i].material.color.g, AffectedMaterials [i].material.color.b, AlphaColor);
					}
				}

				//disable the Selection obj if this is not the local player in a LAN game:
				if (GameManager.PlayerFactionID != RefUnit.FactionID) {
					if (GameManager.Instance.SelectionMgr.SelectedUnits.Contains (RefUnit)) {
						GameManager.Instance.SelectionMgr.DeselectUnit (RefUnit);
					}
					RefUnit.PlayerSelection.gameObject.GetComponent<Collider> ().enabled = false;
				}

				//custom event:
				if (RefUnit.GameMgr.Events) {
					RefUnit.GameMgr.Events.OnUnitGoInvisible (RefUnit);
				}
			} else {
				Timer = 0.0f;
				AudioManager.PlayAudio (RefUnit.GameMgr.GeneralAudioSource.gameObject, GoInvisibleAudio, false);

				//set the aplha color on affected materials:
				for (int i = 0; i < AffectedMaterials.Length; i++) {
					if (AffectedMaterials [i]) {
						AffectedMaterials [i].material.color = new Color (AffectedMaterials [i].material.color.r, AffectedMaterials [i].material.color.g, AffectedMaterials [i].material.color.b, 1.0f);
					}
				}

				if (GameManager.PlayerFactionID != RefUnit.FactionID) {
					RefUnit.PlayerSelection.gameObject.GetComponent<Collider> ().enabled = enabled;
				}

				//custom event:
				if (RefUnit.GameMgr.Events) {
					RefUnit.GameMgr.Events.OnUnitGoVisible (RefUnit);
				}
			}

			RefUnit.UIMgr.UpdateTaskPanel();
        }
	}
}