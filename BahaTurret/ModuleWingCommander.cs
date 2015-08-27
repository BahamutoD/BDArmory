using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BahaTurret
{
	public class ModuleWingCommander : PartModule
	{
		
		public MissileFire weaponManager;

		List<BDModulePilotAI> friendlies;

		List<BDModulePilotAI> wingmen;
		[KSPField(isPersistant = true)]
		public string savedWingmen = string.Empty;

		[KSPField(isPersistant = true)]
		public float spread = 20;

		[KSPField(isPersistant = true)]
		public float lag = 10;


		int focusIndex = 0;
		bool selectAll = true;

		[KSPEvent(guiActive = true, guiName = "ToggleGUI")]
		public void ToggleGUI()
		{
			showGUI = !showGUI;
			if(showGUI)
			{
				RefreshFriendlies();

				//TEMPORARY
				wingmen = new List<BDModulePilotAI>();
				foreach(var p in friendlies)
				{
					wingmen.Add(p);
				}
			}
		}


		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			if(HighLogic.LoadedSceneIsFlight)
			{
				part.force_activate();

				StartCoroutine(StartupRoutine());

				GameEvents.onGameStateSave.Add(SaveWingmen);
				GameEvents.onVesselLoaded.Add(OnVesselLoad);
				GameEvents.onVesselDestroy.Add(OnVesselLoad);
				GameEvents.onVesselGoOnRails.Add(OnVesselLoad);
			}
		}

		IEnumerator StartupRoutine()
		{
			while(vessel.packed)
			{
				yield return null;
			}

			foreach(var mf in vessel.FindPartModulesImplementing<MissileFire>())
			{
				weaponManager = mf;
				break;
			}

			RefreshFriendlies();
			RefreshWingmen();
			LoadWingmen();
		}

		void OnDestroy()
		{
			GameEvents.onGameStateSave.Remove(SaveWingmen);
			GameEvents.onVesselLoaded.Remove(OnVesselLoad);
			GameEvents.onVesselDestroy.Remove(OnVesselLoad);
			GameEvents.onVesselGoOnRails.Remove(OnVesselLoad);
		}

		void OnVesselLoad(Vessel v)
		{
			RefreshFriendlies();
			RefreshWingmen();
		}

		void RefreshFriendlies()
		{
			if(!weaponManager) return;
			friendlies = new List<BDModulePilotAI>();

			foreach(var v in BDATargetManager.LoadedVessels)
			{
				if(!v || !v.loaded || v == vessel) continue;

				BDModulePilotAI pilot = null;
				MissileFire wm = null;
				foreach(var p in v.FindPartModulesImplementing<BDModulePilotAI>())
				{
					pilot = p;
					break;
				}

				if(!pilot) continue;

				foreach(var w in v.FindPartModulesImplementing<MissileFire>())
				{
					wm = w;
				}

				if(!wm || wm.team != weaponManager.team) continue;
				friendlies.Add(pilot);
			}
		}

		void RefreshWingmen()
		{
			if(wingmen == null)
			{
				wingmen = new List<BDModulePilotAI>();
				focusIndex = 0;
				return;
			}

			wingmen.RemoveAll(w => w == null ||  w.weaponManager.team != weaponManager.team);

			focusIndex = Mathf.Clamp(focusIndex, 0, wingmen.Count - 1);
		}

		void SaveWingmen(ConfigNode cfg)
		{
			if(wingmen == null)
			{
				return;
			}

			savedWingmen = string.Empty;
			foreach(var pilot in wingmen)
			{
				savedWingmen += pilot.vessel.id.ToString() + ",";
			}
		}

		void LoadWingmen()
		{
			wingmen = new List<BDModulePilotAI>();

			if(savedWingmen != string.Empty)
			{
				string[] wingIDs = savedWingmen.Split(new char[]{ ',' });
				for(int i = 0; i < wingIDs.Length; i++)
				{
					foreach(Vessel v in BDATargetManager.LoadedVessels)
					{
						if(!v || !v.loaded) continue;

						if(v.id.ToString() == wingIDs[i])
						{
							foreach(var pilot in v.FindPartModulesImplementing<BDModulePilotAI>())
							{
								wingmen.Add(pilot);
								break;
							}
						}
					}
				}
			}
		}


		bool showGUI = false;
		Rect guiWindowRect;
		bool rectInit = false;
		float buttonStartY = 30;
		float buttonHeight = 24;
		float buttonGap = 3;
		float margin = 6;
		float buttonWidth;
		float buttonEndY;
		GUIStyle wingmanButtonStyle;
		GUIStyle wingmanButtonSelectedStyle;
		void OnGUI()
		{
			if(showGUI && vessel.isActiveVessel)
			{
				if(!rectInit)
				{
					guiWindowRect = new Rect(45, 75, 240, 800);
					buttonWidth = guiWindowRect.width - (2*margin);
					buttonEndY = buttonStartY;
					wingmanButtonStyle = new GUIStyle(HighLogic.Skin.button);
					wingmanButtonStyle.alignment = TextAnchor.MiddleLeft;
					wingmanButtonStyle.wordWrap = false;
					wingmanButtonStyle.fontSize = 11;
					wingmanButtonSelectedStyle = new GUIStyle(HighLogic.Skin.box);
					wingmanButtonSelectedStyle.alignment = TextAnchor.MiddleLeft;
					wingmanButtonSelectedStyle.wordWrap = false;
					wingmanButtonSelectedStyle.fontSize = 11;
					rectInit = true;
				}
				guiWindowRect = GUI.Window(1293293, guiWindowRect, WingmenWindow, "WingCommander", HighLogic.Skin.window);
			}
		}
		
		delegate void CommandFunction(BDModulePilotAI wingman, int index);
		void WingmenWindow(int windowID)
		{
			float height = buttonStartY;
			GUI.DragWindow(new Rect(0, 0, guiWindowRect.width, buttonStartY));

			GUI.Box(new Rect(margin-buttonGap, buttonStartY - buttonGap, buttonWidth + (2 * buttonGap), wingmen.Count * (buttonHeight + buttonGap)), GUIContent.none, HighLogic.Skin.box);
			for(int i = 0; i < wingmen.Count; i++)
			{
				WingmanButton(i, out buttonEndY);
			}
			height += buttonEndY;

			//command buttons
			float commandButtonIndex = 0;
			CommandButton(SelectAll, "Select All", ref commandButtonIndex);
			commandButtonIndex += 0.5f;
			CommandButton(CommandFollow, "Follow", ref commandButtonIndex);
			CommandButton(CommandRelease, "Release", ref commandButtonIndex);

			//resize window
			height += (commandButtonIndex * (buttonHeight + buttonGap));
			guiWindowRect.height = height;
		}

		void WingmanButton(int index, out float buttonEndY)
		{
			int i = index;
			Rect buttonRect = new Rect(margin, buttonStartY + (i * (buttonHeight+buttonGap)), buttonWidth, buttonHeight);
			GUIStyle style = (i == focusIndex || selectAll) ? wingmanButtonSelectedStyle : wingmanButtonStyle;
			string label = " "+wingmen[i].vessel.vesselName + " (" + wingmen[i].currentCommand + ")";
			if(GUI.Button(buttonRect, label, style))
			{
				selectAll = false;
				focusIndex = i;
			}

			buttonEndY = buttonStartY + ((i + 1.5f) * buttonHeight);
		}

		void CommandButton(CommandFunction func, string buttonLabel, ref float buttonIndex)
		{
			if(GUI.Button(new Rect(margin, buttonEndY + (buttonIndex*(buttonHeight+buttonGap)), buttonWidth, buttonHeight), buttonLabel, HighLogic.Skin.button))
			{
				if(!selectAll)
				{
					if(focusIndex < wingmen.Count)
					{
						func(wingmen[focusIndex], focusIndex);
					}
				}
				else
				{
					for(int i = 0; i < wingmen.Count; i++)
					{
						func(wingmen[i], i);
					}
				}
			}

			buttonIndex++;
		}

		void CommandRelease(BDModulePilotAI wingman, int index)
		{
			wingman.ReleaseCommand();
		}

		void CommandFollow(BDModulePilotAI wingman, int index)
		{
			wingman.CommandFollow(this, index);
		}

		void SelectAll(BDModulePilotAI wingman, int index)
		{
			selectAll = true;
		}



	}
}

