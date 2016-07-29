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

		[KSPField(guiActive = false, guiActiveEditor = true, guiName = "")]
		public string guiTitle = "WingCommander:";

		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Spread"),
			UI_FloatRange(minValue = 20f, maxValue = 200f, stepIncrement = 1, scene = UI_Scene.Editor)]
		public float spread = 50;

		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Lag"),
			UI_FloatRange(minValue = 0f, maxValue = 100f, stepIncrement = 1, scene = UI_Scene.Editor)]
		public float lag = 10;

		[KSPField(isPersistant = true)]
		public bool commandSelf = false;

		List<GPSTargetInfo> commandedPositions;
		bool drawMouseDiamond = false;

		ScreenMessage screenMessage;


		//int focusIndex = 0;
		List<int> focusIndexes;

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
				focusIndexes = new List<int>();
				commandedPositions = new List<GPSTargetInfo>();
				part.force_activate();

				StartCoroutine(StartupRoutine());

				GameEvents.onGameStateSave.Add(SaveWingmen);
				GameEvents.onVesselLoaded.Add(OnVesselLoad);
				GameEvents.onVesselDestroy.Add(OnVesselLoad);
				GameEvents.onVesselGoOnRails.Add(OnVesselLoad);
				MissileFire.OnToggleTeam += OnToggleTeam;

				screenMessage = new ScreenMessage("", 2, ScreenMessageStyle.LOWER_CENTER);
			}
		}

		void OnToggleTeam(MissileFire mf, BDArmorySettings.BDATeams team)
		{
			RefreshFriendlies();
			RefreshWingmen();
		}

		IEnumerator StartupRoutine()
		{
			while(vessel.packed)
			{
				yield return null;
			}

			weaponManager = part.FindModuleImplementing<MissileFire>();

			RefreshFriendlies();
			RefreshWingmen();
			LoadWingmen();
		}

		void OnDestroy()
		{
			if(HighLogic.LoadedSceneIsFlight)
			{
				GameEvents.onGameStateSave.Remove(SaveWingmen);
				GameEvents.onVesselLoaded.Remove(OnVesselLoad);
				GameEvents.onVesselDestroy.Remove(OnVesselLoad);
				GameEvents.onVesselGoOnRails.Remove(OnVesselLoad);
				MissileFire.OnToggleTeam -= OnToggleTeam;
			}
			
		}

		void OnVesselLoad(Vessel v)
		{
			if(HighLogic.LoadedSceneIsFlight && FlightGlobals.ready && !vessel.packed)
			{
				RefreshFriendlies();
				RefreshWingmen();
			}
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

			//TEMPORARY
			wingmen = new List<BDModulePilotAI>();
			foreach(var p in friendlies)
			{
				wingmen.Add(p);
			}
		}

		void RefreshWingmen()
		{
			if(wingmen == null)
			{
				wingmen = new List<BDModulePilotAI>();
				//focusIndex = 0;
				focusIndexes.Clear();
				return;
			}
			else
			{
				wingmen.RemoveAll(w => w == null || (w.weaponManager && w.weaponManager.team != weaponManager.team));
			}

			List<int> uniqueIndexes = new List<int>();
			foreach(var focusIndex in focusIndexes)
			{
				int clampedIndex = Mathf.Clamp(focusIndex, 0, wingmen.Count - 1);
				if(!uniqueIndexes.Contains(clampedIndex))
				{
					uniqueIndexes.Add(clampedIndex);
				}
			}
			focusIndexes = new List<int>(uniqueIndexes);

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


		public bool showGUI = false;
		public Rect guiWindowRect;
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
			if(HighLogic.LoadedSceneIsFlight && vessel && vessel.isActiveVessel && !vessel.packed)
			{
				if(BDArmorySettings.GAME_UI_ENABLED)
				{
					if(showGUI)
					{
						if(!rectInit)
						{
							guiWindowRect = new Rect(45, 75, 240, 800);
							buttonWidth = guiWindowRect.width - (2 * margin);
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

						if(showAGWindow)
						{
							AGWindow();
						}
					}

					//command position diamonds
					float diamondSize = 24;
					foreach(var comPos in commandedPositions)
					{
						BDGUIUtils.DrawTextureOnWorldPos(comPos.worldPos, BDArmorySettings.Instance.greenDiamondTexture, new Vector2(diamondSize, diamondSize), 0);
						Vector2 labelPos;
						if(BDGUIUtils.WorldToGUIPos(comPos.worldPos, out labelPos))
						{
							labelPos.x += diamondSize/2;
							labelPos.y -= 10;
							GUI.Label(new Rect(labelPos.x, labelPos.y, 300, 20), comPos.name);
						}
					}

					if(drawMouseDiamond)
					{
						Vector2 mouseDiamondPos = Input.mousePosition;
						Rect mouseDiamondRect = new Rect(mouseDiamondPos.x - (diamondSize / 2), Screen.height-mouseDiamondPos.y - (diamondSize / 2), diamondSize, diamondSize);
						GUI.DrawTexture(mouseDiamondRect, BDArmorySettings.Instance.greenDiamondTexture, ScaleMode.StretchToFill, true);
					}
				}
			}
		}
		
		delegate void CommandFunction(BDModulePilotAI wingman, int index, object data);
		void WingmenWindow(int windowID)
		{
			float height = buttonStartY;
			GUI.DragWindow(new Rect(0, 0, guiWindowRect.width-buttonStartY-margin-margin, buttonStartY));

			//close buttton
			float xSize = buttonStartY - margin - margin;
			if(GUI.Button(new Rect(buttonWidth + (2 * buttonGap)-xSize, margin, xSize, xSize), "X", HighLogic.Skin.button))
			{
				showGUI = false;
			}

			GUI.Box(new Rect(margin-buttonGap, buttonStartY - buttonGap, buttonWidth + (2 * buttonGap), Mathf.Max(wingmen.Count * (buttonHeight + buttonGap), 10)), GUIContent.none, HighLogic.Skin.box);
			buttonEndY = buttonStartY;
			for(int i = 0; i < wingmen.Count; i++)
			{
				WingmanButton(i, out buttonEndY);
			}
			buttonEndY = Mathf.Max(buttonEndY, 15f);
			height += buttonEndY;

			//command buttons
			float commandButtonLine = 0;
			CommandButton(SelectAll, "Select All", ref commandButtonLine, false, false);
			//commandButtonLine += 0.25f;

			commandSelf = GUI.Toggle(new Rect(margin, margin + buttonEndY + (commandButtonLine * (buttonHeight + buttonGap)), buttonWidth, buttonHeight), commandSelf, "Command Self", HighLogic.Skin.toggle);
			commandButtonLine++;

			commandButtonLine += 0.10f;

			CommandButton(CommandFollow, "Follow", ref commandButtonLine, true, false);
			CommandButton(CommandFlyTo, "Fly To Pos", ref commandButtonLine, true, waitingForFlytoPos);
			CommandButton(CommandAttack, "Attack Pos", ref commandButtonLine, true, waitingForAttackPos);
			CommandButton(OpenAGWindow, "Action Group", ref commandButtonLine, false, showAGWindow);
			CommandButton(CommandTakeOff, "Take Off", ref commandButtonLine, true, false);
			commandButtonLine += 0.5f;
			CommandButton(CommandRelease, "Release", ref commandButtonLine, true, false);

			commandButtonLine += 0.5f;
			GUI.Label(new Rect(margin, buttonEndY + margin + (commandButtonLine * (buttonHeight + buttonGap)), buttonWidth, 20), "Formation Settings:", HighLogic.Skin.label);
			commandButtonLine++;
			GUI.Label(new Rect(margin, buttonEndY + margin + (commandButtonLine * (buttonHeight + buttonGap)), buttonWidth/3, 20), "Spread: "+spread.ToString("0"), HighLogic.Skin.label);
			spread = GUI.HorizontalSlider(new Rect(margin + (buttonWidth/3),  buttonEndY + margin + (commandButtonLine * (buttonHeight + buttonGap)), 2*buttonWidth/3, 20), spread, 20f, 200f, HighLogic.Skin.horizontalSlider, HighLogic.Skin.horizontalSliderThumb);
			commandButtonLine++;
			GUI.Label(new Rect(margin,  buttonEndY + margin + (commandButtonLine * (buttonHeight + buttonGap)), buttonWidth/3, 20), "Lag: "+lag.ToString("0"), HighLogic.Skin.label);
			lag = GUI.HorizontalSlider(new Rect(margin + (buttonWidth/3),  buttonEndY + margin + (commandButtonLine * (buttonHeight + buttonGap)), 2*buttonWidth/3, 20), lag, 0f, 100f, HighLogic.Skin.horizontalSlider, HighLogic.Skin.horizontalSliderThumb);
			commandButtonLine++;

			//resize window
			height += ((commandButtonLine-1) * (buttonHeight + buttonGap));
			guiWindowRect.height = height;
		}

		void WingmanButton(int index, out float buttonEndY)
		{
			int i = index;
			Rect buttonRect = new Rect(margin, buttonStartY + (i * (buttonHeight+buttonGap)), buttonWidth, buttonHeight);
			GUIStyle style = (focusIndexes.Contains(i)) ? wingmanButtonSelectedStyle : wingmanButtonStyle;
			string label = " "+wingmen[i].vessel.vesselName + " (" + wingmen[i].currentStatus + ")";
			if(GUI.Button(buttonRect, label, style))
			{
				if(focusIndexes.Contains(i))
				{
					focusIndexes.Remove(i);
				}
				else
				{
					focusIndexes.Add(i);
				}
			}
			buttonEndY = buttonStartY + ((i + 1.5f) * buttonHeight);
		}

		void CommandButton(CommandFunction func, string buttonLabel, ref float buttonLine, bool sendToWingmen, bool pressed, object data = null)
		{
			CommandButton(func, buttonLabel, ref buttonLine, buttonEndY, margin, buttonGap, buttonWidth, buttonHeight, sendToWingmen, pressed, data);
		}

		void CommandButton(CommandFunction func, string buttonLabel, ref float buttonLine, float startY, float margin, float buttonGap, float buttonWidth, float buttonHeight, bool sendToWingmen, bool pressed, object data)
		{
			float yPos = startY + margin + ((buttonHeight + buttonGap) * buttonLine);
			if(GUI.Button(new Rect(margin, yPos, buttonWidth, buttonHeight), buttonLabel, pressed ? HighLogic.Skin.box : HighLogic.Skin.button))
			{
				if(sendToWingmen)
				{
					if(wingmen.Count > 0)
					{
						foreach(var index in focusIndexes)
						{
							func(wingmen[index], index, data);
						}
					}

					if(commandSelf)
					{
						foreach(var ai in vessel.FindPartModulesImplementing<BDModulePilotAI>())
						{
							func(ai, -1, data);
						}
					}
				}
				else
				{
					func(null, -1, null);
				}
			}

			buttonLine++;
		}

		void CommandRelease(BDModulePilotAI wingman, int index, object data)
		{
			wingman.ReleaseCommand();
		}

		void CommandFollow(BDModulePilotAI wingman, int index, object data)
		{
			wingman.CommandFollow(this, index);
		}

		public void CommandAllFollow()
		{
			RefreshFriendlies();
			int i = 0;
			foreach(var wingman in friendlies)
			{
				wingman.CommandFollow(this, i);
				i++;
			}
		}

		void CommandAG(BDModulePilotAI wingman, int index, object ag)
		{
			//Debug.Log("object to string: "+ag.ToString());
			KSPActionGroup actionGroup = (KSPActionGroup)ag;
			//Debug.Log("ag to string: " + actionGroup.ToString());
			wingman.CommandAG(actionGroup);
		}

		void CommandTakeOff(BDModulePilotAI wingman, int index, object data)
		{
			wingman.ActivatePilot();
			wingman.standbyMode = false;
		}

		void OpenAGWindow(BDModulePilotAI wingman, int index, object data)
		{
			showAGWindow = !showAGWindow;
		}

		public bool showAGWindow = false;
		float agWindowHeight = 10;
		public Rect agWindowRect;
		void AGWindow()
		{
			float width = 100;
			float buttonHeight = 20;
			float agMargin = 5;
			float newHeight = 0;
			agWindowRect = new Rect(guiWindowRect.x + guiWindowRect.width, guiWindowRect.y, width, agWindowHeight);
			GUI.Box(agWindowRect, string.Empty, HighLogic.Skin.window);
			GUI.BeginGroup(agWindowRect);
			newHeight += agMargin;
			GUIStyle titleStyle = new GUIStyle(HighLogic.Skin.label);
			titleStyle.alignment = TextAnchor.MiddleCenter;
			GUI.Label(new Rect(agMargin, 5, width - (2*agMargin), 20), "Action Groups", titleStyle);
			newHeight += 20;
			float startButtonY = newHeight;
			float buttonLine = 0;
			int i = -1;
			foreach(var ag in Enum.GetValues(typeof(KSPActionGroup)))
			{
				i++;
				if(i <= 1) continue;
				CommandButton(CommandAG, ag.ToString(), ref buttonLine, startButtonY, agMargin, buttonGap, width-(2*agMargin), buttonHeight, true, false, ag);
				newHeight += buttonHeight + buttonGap;

			}

			newHeight += agMargin;
			GUI.EndGroup();

			agWindowHeight = newHeight;
		}

		void SelectAll(BDModulePilotAI wingman, int index, object data)
		{
			for(int i = 0; i < wingmen.Count; i++)
			{
				if(!focusIndexes.Contains(i))
				{
					focusIndexes.Add(i);
				}
			}
		}

		void CommandFlyTo(BDModulePilotAI wingman, int index, object data)
		{
			StartCoroutine(CommandPosition(wingman, BDModulePilotAI.PilotCommands.FlyTo));
		}

		void CommandAttack(BDModulePilotAI wingman, int index, object data)
		{
			StartCoroutine(CommandPosition(wingman, BDModulePilotAI.PilotCommands.Attack));
		}


		bool waitingForFlytoPos = false;
		bool waitingForAttackPos = false;
		IEnumerator CommandPosition(BDModulePilotAI wingman, BDModulePilotAI.PilotCommands command)
		{
			if(focusIndexes.Count == 0 && !commandSelf)
			{
				yield break;
			}

			DisplayScreenMessage("Select target coordinates.\nRight-click to cancel.");

			if(command == BDModulePilotAI.PilotCommands.FlyTo)
			{
				waitingForFlytoPos = true;
			}
			else if(command == BDModulePilotAI.PilotCommands.Attack)
			{
				waitingForAttackPos = true;
			}

			yield return null;

			bool waitingForPos = true;
			drawMouseDiamond = true;
			while(waitingForPos)
			{
				

				if(Input.GetMouseButtonDown(1))
				{
					break;
				}
				if(Input.GetMouseButtonDown(0))
				{
					Vector3 mousePos = new Vector3(Input.mousePosition.x/Screen.width, Input.mousePosition.y/Screen.height, 0);
					Plane surfPlane = new Plane(vessel.upAxis, vessel.transform.position - (vessel.altitude * vessel.upAxis));
					Ray ray = FlightCamera.fetch.mainCamera.ViewportPointToRay(mousePos);
					float dist;
					if(surfPlane.Raycast(ray, out dist))
					{
						Vector3 worldPoint = ray.GetPoint(dist);
						Vector3d gps = VectorUtils.WorldPositionToGeoCoords(worldPoint, vessel.mainBody);

						if(command == BDModulePilotAI.PilotCommands.FlyTo)
						{
							wingman.CommandFlyTo(gps);
						}
						else if(command == BDModulePilotAI.PilotCommands.Attack)
						{
							wingman.CommandAttack(gps);
						}

						StartCoroutine(CommandPositionGUIRoutine(wingman, new GPSTargetInfo(gps, command.ToString())));

					}

					break;
				}
				yield return null;
			}

			waitingForAttackPos = false;
			waitingForFlytoPos = false;
			drawMouseDiamond = false;
			ScreenMessages.RemoveMessage(screenMessage);
		}

		IEnumerator CommandPositionGUIRoutine(BDModulePilotAI wingman, GPSTargetInfo tInfo)
		{
			//RemoveCommandPos(tInfo);
			commandedPositions.Add(tInfo);
			yield return new WaitForSeconds(0.25f);
			while(Vector3d.Distance(wingman.commandGPS, tInfo.gpsCoordinates) < 0.01f && (wingman.currentCommand == BDModulePilotAI.PilotCommands.Attack || wingman.currentCommand == BDModulePilotAI.PilotCommands.FlyTo))
			{
				yield return null;
			}
			RemoveCommandPos(tInfo);
		}


		void RemoveCommandPos(GPSTargetInfo tInfo)
		{
			commandedPositions.RemoveAll(t => t.EqualsTarget(tInfo));
		}

		void DisplayScreenMessage(string message)
		{
			if(BDArmorySettings.GAME_UI_ENABLED && vessel == FlightGlobals.ActiveVessel)
			{
				ScreenMessages.RemoveMessage(screenMessage);
				screenMessage.message = message;
				ScreenMessages.PostScreenMessage(screenMessage);
			}
		}
	}
}

