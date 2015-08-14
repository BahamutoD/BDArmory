using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BahaTurret
{
	[KSPAddon(KSPAddon.Startup.EveryScene, false)]
	public class BDArmorySettings : MonoBehaviour
	{
		

		//=======configurable settings
		public static string FIRE_KEY = "mouse 0";
		public static bool INSTAKILL = false;
		public static bool BULLET_HITS = true;
		public static float PHYSICS_RANGE = 0;
		public static bool EJECT_SHELLS = true;
		public static bool INFINITE_AMMO = false;
		//public static bool CAMERA_TOOLS = true;
		public static bool DRAW_DEBUG_LINES = false;
		public static bool DRAW_DEBUG_LABELS = false;
		public static bool DRAW_AIMERS = true;
		public static bool AIM_ASSIST = true;
		public static bool REMOTE_SHOOTING = false;
		public static bool BOMB_CLEARANCE_CHECK = true;
		public static float DMG_MULTIPLIER = 6000;
		public static float FLARE_CHANCE_FACTOR = 25;
		public static bool SMART_GUARDS = true;
		public static float MAX_BULLET_RANGE = 5000;
		public static float TRIGGER_HOLD_TIME = 0.3f;

		public static bool ALLOW_LEGACY_TARGETING = true;

		public static float TARGET_CAM_RESOLUTION = 360;
		public static bool BW_TARGET_CAM = true;
		public static float SMOKE_DEFLECTION_FACTOR = 10;

		public static float FLARE_THERMAL = 1900;

		public static float BDARMORY_UI_VOLUME = 0.35f; 
		public static float BDARMORY_WEAPONS_VOLUME = 0.32f;

		public static float MAX_GUARD_VISUAL_RANGE = 3500;


		public static float GLOBAL_LIFT_MULTIPLIER = 0.20f;
		public static float GLOBAL_DRAG_MULTIPLIER = 4f;
		//==================
		//EVENTS
		public delegate void VolumeChange();
		public static event VolumeChange OnVolumeChange;

		public delegate void SavedSettings();
		public static event SavedSettings OnSavedSettings;

		//particle optimization
		public static int numberOfParticleEmitters = 0;
		
		//gun volume management
		public static int numberOfGunsFiring = 0;
		
		
		public static BDArmorySettings Instance;

		public static bool GAME_UI_ENABLED = true;
		
		//settings gui
		public bool settingsGuiEnabled = false;
		public string physicsRangeGui;
		public string fireKeyGui;
		
		
		
		//toolbar gui
		public static bool hasAddedButton = false;
		public static bool toolbarGuiEnabled = false;
		float toolWindowWidth = 300;
		float toolWindowHeight = 100;
		public Rect toolbarWindowRect;
		bool showWeaponList = false;
		bool showGuardMenu = false;
		bool showModules = false;
		int numberOfModules = 0;

		//gps window
		public bool showingGPSWindow
		{
			get
			{
				return showGPSWindow;
			}
		}
		bool showGPSWindow = false;
		Rect gpsWindowRect;
		int gpsEntryCount = 0;
		float gpsEntryHeight = 24;
		float gpsBorder = 5;
		bool editingGPSName = false;
		int editingGPSNameIndex = 0;
		bool hasEnteredGPSName = false;
		string newGPSName = string.Empty;

		public MissileFire ActiveWeaponManager = null;
		public bool missileWarning = false;
		public float missileWarningTime = 0;
		
		
		
		//load range stuff
		VesselRanges combatVesselRanges = new VesselRanges();
		float physRangeTimer;
		
			
	
		public static List<CMFlare> Flares = new List<CMFlare>();

		bool isRecordingInput = false;
		bool recordMouseUp = false;
		
		//gui styles
		GUIStyle centerLabel;
		GUIStyle centerLabelRed;
		GUIStyle centerLabelOrange;
		GUIStyle centerLabelBlue;
		GUIStyle leftLabel;
		GUIStyle leftLabelRed;
		GUIStyle rightLabelRed;
		GUIStyle leftLabelGray;

		public enum BDATeams{A, B, None};



		//common textures
		public static string textureDir = "BDArmory/Textures/";

		bool drawCursor = false;
		Texture2D cursorTexture = GameDatabase.Instance.GetTexture(textureDir + "aimer", false);

		private Texture2D dti;
		public Texture2D directionTriangleIcon
		{
			get
			{
				return dti ? dti : dti = GameDatabase.Instance.GetTexture(textureDir + "directionIcon", false);
			}
		}

		private Texture2D cgs;
		public Texture2D crossedGreenSquare
		{
			get
			{
				return cgs ? cgs : cgs = GameDatabase.Instance.GetTexture(textureDir + "crossedGreenSquare", false);
			}
		}

		private Texture2D dlgs;
		public Texture2D dottedLargeGreenCircle
		{
			get
			{
				return dlgs ? dlgs : dlgs = GameDatabase.Instance.GetTexture (textureDir + "dottedLargeGreenCircle", false);
			}
		}

		private Texture2D ogs;
		public Texture2D openGreenSquare
		{
			get
			{
				return ogs ? ogs : ogs = GameDatabase.Instance.GetTexture(textureDir + "openGreenSquare", false);
			}
		}

		private Texture2D gdott;
		public Texture2D greenDotTexture
		{
			get
			{
				return gdott ? gdott : gdott = GameDatabase.Instance.GetTexture(textureDir + "greenDot", false);
			}
		}

		private Texture2D gdt;
		public Texture2D greenDiamondTexture
		{
			get
			{
					return gdt ? gdt : gdt = GameDatabase.Instance.GetTexture(textureDir + "greenDiamond", false);
			}
		}

		private Texture2D lgct;
		public Texture2D largeGreenCircleTexture
		{
			get
			{
				return lgct ? lgct : lgct = GameDatabase.Instance.GetTexture(textureDir + "greenCircle3", false);
			}
		}

		private Texture2D gct;
		public Texture2D greenCircleTexture
		{
			get
			{
				return gct ? gct : gct = GameDatabase.Instance.GetTexture(textureDir + "greenCircle2", false);
			}
		}

		private Texture2D gpct;
		public Texture2D greenPointCircleTexture
		{
			get
			{
				if(gpct == null)
				{
					gpct = GameDatabase.Instance.GetTexture(textureDir + "greenPointCircle", false);
				}
				return gpct;
			}
		}

		private Texture2D gspct;
		public Texture2D greenSpikedPointCircleTexture
		{
			get
			{
				return gspct ? gspct : gspct = GameDatabase.Instance.GetTexture(textureDir + "greenSpikedCircle", false);
			}
		}

		private Texture2D wSqr;
		public Texture2D whiteSquareTexture
		{
			get
			{
				return wSqr ? wSqr : wSqr = GameDatabase.Instance.GetTexture(textureDir + "whiteSquare", false);
			}
		}

		private Texture2D oWSqr;
		public Texture2D openWhiteSquareTexture
		{
			get
			{
				return oWSqr ? oWSqr : oWSqr = GameDatabase.Instance.GetTexture(textureDir + "openWhiteSquare", false);;
			}
		}

		private Texture2D tDir;
		public Texture2D targetDirectionTexture
		{
			get
			{
				return tDir ? tDir : tDir = GameDatabase.Instance.GetTexture(textureDir + "targetDirectionIndicator", false);
			}
		}

		private Texture2D hInd;
		public Texture2D horizonIndicatorTexture
		{
			get
			{
				return hInd ? hInd : hInd = GameDatabase.Instance.GetTexture(textureDir + "horizonIndicator", false);
			}
		}
		//end textures


		public static bool GameIsPaused
		{
			get
			{
				return PauseMenu.isOpen || Time.timeScale == 0;
			}
		}



		void Start()
		{	
			Instance = this;

			//settings
			SetupSettingsSize();
			LoadConfig();

			//wmgr tolbar
			toolbarWindowRect = new Rect(Screen.width-toolWindowWidth-4, 150, toolWindowWidth, toolWindowHeight);

			physRangeTimer = Time.time;

			
			GAME_UI_ENABLED = true;
			

			fireKeyGui = FIRE_KEY;

			//setup gui styles
			centerLabel = new GUIStyle();
			centerLabel.alignment = TextAnchor.UpperCenter;
			centerLabel.normal.textColor = Color.white;
			
			centerLabelRed = new GUIStyle();
			centerLabelRed.alignment = TextAnchor.UpperCenter;
			centerLabelRed.normal.textColor = Color.red;
			
			centerLabelOrange = new GUIStyle();
			centerLabelOrange.alignment = TextAnchor.UpperCenter;
			centerLabelOrange.normal.textColor = XKCDColors.BloodOrange;
			
			centerLabelBlue = new GUIStyle();
			centerLabelBlue.alignment = TextAnchor.UpperCenter;
			centerLabelBlue.normal.textColor = XKCDColors.AquaBlue;
			
			leftLabel = new GUIStyle();
			leftLabel.alignment = TextAnchor.UpperLeft;
			leftLabel.normal.textColor = Color.white;
			
			leftLabelRed = new GUIStyle();
			leftLabelRed.alignment = TextAnchor.UpperLeft;
			leftLabelRed.normal.textColor = Color.red;
			
			rightLabelRed = new GUIStyle();
			rightLabelRed.alignment = TextAnchor.UpperRight;
			rightLabelRed.normal.textColor = Color.red;
			
			leftLabelGray = new GUIStyle();
			leftLabelGray.alignment = TextAnchor.UpperLeft;
			leftLabelGray.normal.textColor = Color.gray;
			//

			if(HighLogic.LoadedSceneIsFlight)
			{
				ApplyPhysRange();
				SaveVolumeSettings();

				GameEvents.onHideUI.Add(HideGameUI);
				GameEvents.onShowUI.Add(ShowGameUI);
				GameEvents.onVesselGoOffRails.Add(OnVesselGoOffRails);
				GameEvents.OnGameSettingsApplied.Add(SaveVolumeSettings);
				GameEvents.onVesselCreate.Add(ApplyNewVesselRanges);


				foreach(var cam in FlightCamera.fetch.cameras)
				{
					cam.gameObject.AddComponent<CameraBulletRenderer>();
				}

				gpsWindowRect = new Rect(0, 0, toolbarWindowRect.width, 0);

				GameEvents.onVesselChange.Add(VesselChange);
			}
		}
		
		void Update()
		{
			if(HighLogic.LoadedSceneIsFlight)
			{
				if(missileWarning && Time.time - missileWarningTime > 1.5f)
				{
					missileWarning = false;	
				}

				/*
				if(Input.GetKeyDown(KeyCode.Keypad1))
				{
					VesselRanges vr = FlightGlobals.ActiveVessel.vesselRanges;
					Debug.Log ("Flying: ");
					Debug.Log ("load: " + vr.flying.load);
					Debug.Log ("unload: " + vr.flying.unload);
					Debug.Log ("pack: " + vr.flying.pack);
					Debug.Log ("unpack" + vr.flying.unpack);

					Debug.Log ("Landed: ");
					Debug.Log ("load: " + vr.landed.load);
					Debug.Log ("unload: " + vr.landed.unload);
					Debug.Log ("pack: " + vr.landed.pack);
					Debug.Log ("unpack" + vr.landed.unpack);

					Debug.Log ("Splashed: ");
					Debug.Log ("load: " + vr.splashed.load);
					Debug.Log ("unload: " + vr.splashed.unload);
					Debug.Log ("pack: " + vr.splashed.pack);
					Debug.Log ("unpack" + vr.splashed.unpack);

				}
				*/


			

			
			
			
				if(Input.GetKeyDown(KeyCode.KeypadMultiply))
				{
					toolbarGuiEnabled = !toolbarGuiEnabled;	
				}
			
			}

			if(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
			{
				if(Input.GetKeyDown(KeyCode.B))
				{
					settingsGuiEnabled = !settingsGuiEnabled;
					if(settingsGuiEnabled)
					{
						LoadConfig();
					}
					else
					{
						SaveConfig();
					}
					physicsRangeGui = PHYSICS_RANGE.ToString();
				}
			}
			
		}

		void LateUpdate()
		{
			if(HighLogic.LoadedSceneIsFlight)
			{
				DrawAimerCursor();
			}
		}
		
		void DrawAimerCursor()
		{
			if(ActiveWeaponManager == null)
			{
				return;
			}

			Screen.showCursor = true;
			drawCursor = false;
			if(!MapView.MapIsEnabled && !Misc.CheckMouseIsOnGui() && !PauseMenu.isOpen)
			{
				/*
				foreach(BahaTurret bt in FlightGlobals.ActiveVessel.FindPartModulesImplementing<BahaTurret>())
				{
					if(bt.deployed && DRAW_AIMERS && bt.maxPitch > 1)
					{
						Screen.showCursor = false;
						drawCursor = true;
						return;
					}
				}
				*/

				foreach(ModuleWeapon mw in FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleWeapon>())
				{
					if(mw.weaponState == ModuleWeapon.WeaponStates.Enabled && mw.maxPitch > 1 && !mw.slaved && !mw.aiControlled)
					{
						Screen.showCursor = false;
						drawCursor = true;
						return;
					}
				}
			}
		}
		
	

		void VesselChange(Vessel v)
		{
			if(v.isActiveVessel)
			{
				GetWeaponManager();
			}
		}
		
		void GetWeaponManager()
		{
			foreach(var mf in FlightGlobals.ActiveVessel.FindPartModulesImplementing<MissileFire>())	
			{
				ActiveWeaponManager = mf;
				return;
			}
			
			ActiveWeaponManager = null;
			return;
		}
		
		public static void LoadConfig()
		{
			try
			{
				Debug.Log ("== BDArmory : Loading settings.cfg ==");
				ConfigNode cfg = ConfigNode.Load("GameData/BDArmory/settings.cfg");
				
				if(cfg.HasValue("FireKey")) FIRE_KEY = cfg.GetValue("FireKey");	
				
				if(cfg.HasValue("INSTAKILL")) INSTAKILL = bool.Parse(cfg.GetValue("INSTAKILL"));	
				
				if(cfg.HasValue("BULLET_HITS")) BULLET_HITS = Boolean.Parse(cfg.GetValue("BULLET_HITS"));	
				
				if(cfg.HasValue("PHYSICS_RANGE")) PHYSICS_RANGE = float.Parse(cfg.GetValue("PHYSICS_RANGE"));	
				
				if(cfg.HasValue("EJECT_SHELLS")) EJECT_SHELLS = bool.Parse(cfg.GetValue("EJECT_SHELLS"));
				
				if(cfg.HasValue("INFINITE_AMMO")) INFINITE_AMMO = Boolean.Parse(cfg.GetValue("INFINITE_AMMO"));
				
				if(cfg.HasValue("DRAW_DEBUG_LINES")) DRAW_DEBUG_LINES = Boolean.Parse(cfg.GetValue("DRAW_DEBUG_LINES"));
				
				if(cfg.HasValue("DRAW_AIMERS")) DRAW_AIMERS = Boolean.Parse(cfg.GetValue ("DRAW_AIMERS"));
				
				if(cfg.HasValue("AIM_ASSIST")) AIM_ASSIST = Boolean.Parse(cfg.GetValue("AIM_ASSIST"));
				
				if(cfg.HasValue("REMOTE_SHOOTING")) REMOTE_SHOOTING = Boolean.Parse(cfg.GetValue("REMOTE_SHOOTING"));
				
				if(cfg.HasValue("DMG_MULTIPLIER")) DMG_MULTIPLIER = float.Parse(cfg.GetValue("DMG_MULTIPLIER"));
				
				if(cfg.HasValue("FLARE_CHANCE_FACTOR")) FLARE_CHANCE_FACTOR = float.Parse(cfg.GetValue("FLARE_CHANCE_FACTOR"));
				
				if(cfg.HasValue("BOMB_CLEARANCE_CHECK")) BOMB_CLEARANCE_CHECK = Boolean.Parse(cfg.GetValue("BOMB_CLEARANCE_CHECK"));

				if(cfg.HasValue("SMART_GUARDS")) SMART_GUARDS = Boolean.Parse(cfg.GetValue("SMART_GUARDS"));

				if(cfg.HasValue("TRIGGER_HOLD_TIME")) TRIGGER_HOLD_TIME = float.Parse(cfg.GetValue("TRIGGER_HOLD_TIME"));

				if(cfg.HasValue("ALLOW_LEGACY_TARGETING")) ALLOW_LEGACY_TARGETING = bool.Parse(cfg.GetValue("ALLOW_LEGACY_TARGETING"));

				if(cfg.HasValue("TARGET_CAM_RESOLUTION")) TARGET_CAM_RESOLUTION = float.Parse(cfg.GetValue("TARGET_CAM_RESOLUTION"));

				if(cfg.HasValue("BW_TARGET_CAM")) BW_TARGET_CAM = bool.Parse(cfg.GetValue("BW_TARGET_CAM"));

				if(cfg.HasValue("SMOKE_DEFLECTION_FACTOR")) SMOKE_DEFLECTION_FACTOR = float.Parse(cfg.GetValue("SMOKE_DEFLECTION_FACTOR"));

				if(cfg.HasValue("FLARE_THERMAL")) FLARE_THERMAL = float.Parse(cfg.GetValue("FLARE_THERMAL"));

				if(cfg.HasValue("BDARMORY_UI_VOLUME")) BDARMORY_UI_VOLUME = float.Parse(cfg.GetValue("BDARMORY_UI_VOLUME"));

				if(cfg.HasValue("BDARMORY_WEAPONS_VOLUME")) BDARMORY_WEAPONS_VOLUME = float.Parse(cfg.GetValue("BDARMORY_WEAPONS_VOLUME"));

				if(cfg.HasValue("GLOBAL_LIFT_MULTIPLIER")) GLOBAL_LIFT_MULTIPLIER = float.Parse(cfg.GetValue("GLOBAL_LIFT_MULTIPLIER"));

				if(cfg.HasValue("GLOBAL_DRAG_MULTIPLIER")) GLOBAL_DRAG_MULTIPLIER = float.Parse(cfg.GetValue("GLOBAL_DRAG_MULTIPLIER"));
			}
			catch(NullReferenceException)
			{
				Debug.Log ("== BDArmory : Failed to load settings config==");	
			}
		}
		
		public static void SaveConfig() 
		{
			try
			{
				Debug.Log("== BDArmory : Saving settings.cfg ==	");
				ConfigNode cfg = ConfigNode.Load("GameData/BDArmory/settings.cfg");
				
				cfg.SetValue("FireKey", FIRE_KEY, true);
				cfg.SetValue("INSTAKILL", INSTAKILL.ToString(), true);
				cfg.SetValue("BULLET_HITS", BULLET_HITS.ToString(), true);
				cfg.SetValue("PHYSICS_RANGE", PHYSICS_RANGE.ToString(), true);
				cfg.SetValue("EJECT_SHELLS", EJECT_SHELLS.ToString(), true);
				cfg.SetValue("INFINITE_AMMO", INFINITE_AMMO.ToString(), true);
				cfg.SetValue("DRAW_DEBUG_LINES", DRAW_DEBUG_LINES.ToString(), true);
				cfg.SetValue("DRAW_AIMERS", DRAW_AIMERS.ToString(), true);
				cfg.SetValue("AIM_ASSIST", AIM_ASSIST.ToString(), true);
				cfg.SetValue("REMOTE_SHOOTING", REMOTE_SHOOTING.ToString(), true);
				cfg.SetValue("DMG_MULTIPLIER", DMG_MULTIPLIER.ToString(), true);
				cfg.SetValue("FLARE_CHANCE_FACTOR", FLARE_CHANCE_FACTOR.ToString(), true);
				cfg.SetValue("BOMB_CLEARANCE_CHECK", BOMB_CLEARANCE_CHECK.ToString(), true);
				cfg.SetValue("SMART_GUARDS", SMART_GUARDS.ToString(), true);
				cfg.SetValue("TRIGGER_HOLD_TIME", TRIGGER_HOLD_TIME.ToString(), true);
				cfg.SetValue("ALLOW_LEGACY_TARGETING", ALLOW_LEGACY_TARGETING.ToString(), true);
				cfg.SetValue("TARGET_CAM_RESOLUTION", TARGET_CAM_RESOLUTION.ToString(), true);
				cfg.SetValue("BW_TARGET_CAM", BW_TARGET_CAM.ToString(), true);
				cfg.SetValue("SMOKE_DEFLECTION_FACTOR", SMOKE_DEFLECTION_FACTOR.ToString(), true);
				cfg.SetValue("FLARE_THERMAL", FLARE_THERMAL.ToString(), true);
				cfg.SetValue("BDARMORY_UI_VOLUME", BDARMORY_UI_VOLUME.ToString(), true);
				cfg.SetValue("BDARMORY_WEAPONS_VOLUME", BDARMORY_WEAPONS_VOLUME.ToString(), true);
				cfg.SetValue("GLOBAL_LIFT_MULTIPLIER", GLOBAL_LIFT_MULTIPLIER.ToString(), true);
				cfg.SetValue("GLOBAL_DRAG_MULTIPLIER", GLOBAL_DRAG_MULTIPLIER.ToString(), true);

				cfg.Save ("GameData/BDArmory/settings.cfg");

				if(OnSavedSettings!=null)
				{
					OnSavedSettings();
				}
				
			}
			catch(NullReferenceException)
			{
				Debug.Log ("== BDArmory : Failed to save settings.cfg ==");
			}
		}
		
		
		#region GUI
		
		void OnGUI()
		{
			if(GAME_UI_ENABLED)
			{
				if(settingsGuiEnabled)
				{
					SettingsGUI();
				}
				
				if(drawCursor)
				{
					//mouse cursor
					float cursorSize = 40;
					Vector3 cursorPos = Input.mousePosition;
					Rect cursorRect = new Rect(cursorPos.x - (cursorSize/2), Screen.height - cursorPos.y - (cursorSize/2), cursorSize, cursorSize);
					GUI.DrawTexture(cursorRect, cursorTexture);	
				}
				
				if(toolbarGuiEnabled && HighLogic.LoadedSceneIsFlight)
				{
					toolbarWindowRect = GUI.Window(321, toolbarWindowRect, ToolbarGUI, "");
					if(showGPSWindow && ActiveWeaponManager)
					{
						gpsWindowRect = GUI.Window(424333, gpsWindowRect, GPSWindow, "", GUI.skin.box);
						foreach(var coordinate in BDATargetManager.GPSTargets[BDATargetManager.BoolToTeam(ActiveWeaponManager.team)])
						{
							BDGUIUtils.DrawTextureOnWorldPos(coordinate.worldPos, BDArmorySettings.Instance.greenDotTexture, new Vector2(8,8), 0);
						}
					}
				}
			}
			
		

			if(DRAW_DEBUG_LABELS && HighLogic.LoadedSceneIsFlight)
			{
				if(RadarUtils.radarRT)
				{
					GUI.DrawTexture(new Rect(20,20,128,128), RadarUtils.radarRT, ScaleMode.StretchToFill, true);
				}
			}
		}
		
		void ToolbarGUI(int windowID)
		{
			GUI.DragWindow(new Rect(0,0,toolWindowWidth, 30));

			float line = 0;
			float leftIndent = 10;
			float contentWidth = (toolWindowWidth) - (2*leftIndent);
			float contentTop = 20;
			float entryHeight = 18;
			
			GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), "BDA Weapon Manager", centerLabel);
			if(missileWarning) 
			{
				GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), "Missile", leftLabelRed);
				GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), "Lock", rightLabelRed);
			}
			line++;
			line += 0.25f;
			
			if(ActiveWeaponManager!=null)
			{
				GUIStyle armedLabelStyle;
				string armedText = "System is ";
				if(ActiveWeaponManager.isArmed)
				{
					armedText += "ARMED.";
					armedLabelStyle = leftLabelRed;
				}
				else
				{
					armedText += "disarmed.";
					armedLabelStyle = leftLabelGray;
				}
				if(GUI.Button(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth/2, entryHeight), armedText, armedLabelStyle))
				{
					ActiveWeaponManager.ToggleArm();
				}
				
				GUIStyle teamButtonStyle;
				string teamText = "Team: ";
				if(ActiveWeaponManager.team)
				{
					teamButtonStyle = centerLabelOrange;
					teamText += "B";
				}
				else
				{
					teamButtonStyle = centerLabelBlue;	
					teamText += "A";
				}
				
				if(GUI.Button(new Rect(leftIndent+(contentWidth/2), contentTop+(line*entryHeight), contentWidth/2, entryHeight), teamText, teamButtonStyle))
				{
					ActiveWeaponManager.ToggleTeam();
				}
				line++;

				string weaponName = ActiveWeaponManager.selectedWeaponString;// = ActiveWeaponManager.selectedWeapon == null ? "None" : ActiveWeaponManager.selectedWeapon.GetShortName();
				string selectionText = "Weapon: "+weaponName;
				GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), selectionText, centerLabel);
				line++;

				//if weapon can ripple, show option and slider.
				if(ActiveWeaponManager.canRipple)
				{
					string rippleText = ActiveWeaponManager.rippleFire ? "Ripple: ON - "+ActiveWeaponManager.rippleRPM.ToString("0")+" RPM" : "Ripple: OFF";
					ActiveWeaponManager.rippleFire = GUI.Toggle(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth/2, entryHeight), ActiveWeaponManager.rippleFire, rippleText, leftLabel);
					if(ActiveWeaponManager.rippleFire)
					{
						ActiveWeaponManager.rippleRPM = GUI.HorizontalSlider(new Rect(leftIndent+(contentWidth/2), contentTop+(line*entryHeight), contentWidth/2, entryHeight), ActiveWeaponManager.rippleRPM, 100, 1600);
					}
				}
				line++;
				
				showWeaponList = GUI.Toggle(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth/3, entryHeight), showWeaponList, "Weapons");
				showGuardMenu = GUI.Toggle(new Rect(leftIndent+(contentWidth/3), contentTop+(line*entryHeight), contentWidth/3, entryHeight), showGuardMenu, "Guard Menu");
				showModules = GUI.Toggle(new Rect(leftIndent+(2*contentWidth/3), contentTop+(line*entryHeight), contentWidth/3, entryHeight), showModules, "Modules");
				line++;
				
				if(showWeaponList)
				{
					line += 0.25f;
					GUI.Box(new Rect(5,  contentTop+(line*entryHeight), toolWindowWidth-10, ActiveWeaponManager.weaponArray.Length * entryHeight),""); //darker box
					for(int i = 0; i < ActiveWeaponManager.weaponArray.Length; i++)
					{
						GUIStyle wpnListStyle;
						if(i == ActiveWeaponManager.weaponIndex)
						{
							wpnListStyle = centerLabelOrange;
						}
						else 
						{
							wpnListStyle = centerLabel;
						}
						string label = ActiveWeaponManager.weaponArray[i] == null ? "None" : ActiveWeaponManager.weaponArray[i].GetShortName();
						if(GUI.Button(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), label, wpnListStyle))
						{
							ActiveWeaponManager.CycleWeapon(i);
						}
						line++;
					}
				}
				
				if(showGuardMenu)
				{
					line += 0.25f;
					GUI.Box(new Rect(5, contentTop+(line*entryHeight), toolWindowWidth-10, 5*entryHeight), "");
					contentWidth -= 16;
					leftIndent += 3;
					ActiveWeaponManager.guardMode =  GUI.Toggle(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), ActiveWeaponManager.guardMode, " Guard Mode");
					line++;

					string scanLabel = ALLOW_LEGACY_TARGETING ? "Scan Interval" : "Firing Interval";
					GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), 85, entryHeight), scanLabel, leftLabel);
					ActiveWeaponManager.targetScanInterval = GUI.HorizontalSlider(new Rect(leftIndent+(90), contentTop+(line*entryHeight), contentWidth-90-38, entryHeight), ActiveWeaponManager.targetScanInterval, 1, 60);
					ActiveWeaponManager.targetScanInterval = Mathf.Round(ActiveWeaponManager.targetScanInterval);
					GUI.Label(new Rect(leftIndent+(contentWidth-35), contentTop+(line*entryHeight), 35, entryHeight), ActiveWeaponManager.targetScanInterval.ToString(), leftLabel);
					line++;
					
					GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), 85, entryHeight), "Field of View", leftLabel);
					float guardAngle = ActiveWeaponManager.guardAngle;
					guardAngle = GUI.HorizontalSlider(new Rect(leftIndent+90, contentTop+(line*entryHeight), contentWidth-90-38, entryHeight), guardAngle, 10, 360);
					guardAngle = guardAngle/10;
					guardAngle = Mathf.Round(guardAngle);
					ActiveWeaponManager.guardAngle = guardAngle * 10;
					GUI.Label(new Rect(leftIndent+(contentWidth-35), contentTop+(line*entryHeight), 35, entryHeight), ActiveWeaponManager.guardAngle.ToString(), leftLabel);
					line++;

					string rangeLabel = ALLOW_LEGACY_TARGETING ? "Guard Range" : "Visual Range";
					GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), 85, entryHeight), rangeLabel, leftLabel);
					float guardRange = ActiveWeaponManager.guardRange;
					float maxVisRange = ALLOW_LEGACY_TARGETING ? Mathf.Clamp(PHYSICS_RANGE, 2500, 100000) : BDArmorySettings.MAX_GUARD_VISUAL_RANGE;
					guardRange = GUI.HorizontalSlider(new Rect(leftIndent+90, contentTop+(line*entryHeight), contentWidth-90-38, entryHeight), guardRange, 100, maxVisRange);
					guardRange = guardRange/100;
					guardRange = Mathf.Round(guardRange);
					ActiveWeaponManager.guardRange = guardRange * 100;
					GUI.Label(new Rect(leftIndent+(contentWidth-35), contentTop+(line*entryHeight), 35, entryHeight), ActiveWeaponManager.guardRange.ToString(), leftLabel);
					line++;
					
					string targetType = "Target Type: ";
					if(ActiveWeaponManager.targetMissiles)
					{
						targetType += "Missiles";	
					}
					else
					{
						targetType += "Vessels";	
					}
					
					if(GUI.Button(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), targetType, leftLabel))
					{
						ActiveWeaponManager.ToggleTargetType();	
					}
					line++;
				}

				if(showModules)
				{
					line += 0.25f;
					if(numberOfModules > 0)
					{
						GUI.Box(new Rect(5,  contentTop+(line*entryHeight), toolWindowWidth-10, numberOfModules * entryHeight),""); //darker box
					}
					numberOfModules = 0;

					//RWR
					if(ActiveWeaponManager.rwr)
					{
						numberOfModules++;
						bool isEnabled = ActiveWeaponManager.rwr.rwrEnabled;
						string label = "Radar Warning Receiver";
						Rect rwrRect = new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight);
						if(GUI.Button(rwrRect, label, isEnabled ? centerLabelOrange : centerLabel))
						{
							if(isEnabled)
							{
								ActiveWeaponManager.rwr.DisableRWR();
							}
							else
							{
								ActiveWeaponManager.rwr.EnableRWR();
							}
						}
						line++;
					}

					//TGP
					foreach(var mtc in ActiveWeaponManager.targetingPods)
					{
						numberOfModules++;
						bool isEnabled = (mtc.cameraEnabled);
						bool isActive = (mtc == ModuleTargetingCamera.activeCam);
						GUIStyle moduleStyle = isEnabled ? centerLabelOrange : centerLabel;// = mtc 
						string label = mtc.part.partInfo.title;
						if(isActive)
						{
							moduleStyle = centerLabelRed;
							label = "["+label+"]";
						}
						if(GUI.Button(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), label, moduleStyle))
						{
							if(isActive)
							{
								mtc.ToggleCamera();
							}
							else
							{
								mtc.EnableCamera();
							}
						}
						line++;
					}

					//RADAR
					foreach(var mr in ActiveWeaponManager.radars)
					{
						numberOfModules++;
						GUIStyle moduleStyle = mr.radarEnabled ? centerLabelBlue : centerLabel;
						string label = mr.part.partInfo.title;
						if(GUI.Button(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), label, moduleStyle))
						{
							mr.Toggle();
						}
						line++;
					}

					//JAMMERS
					foreach(var jammer in ActiveWeaponManager.jammers)
					{
						if(jammer.alwaysOn) continue;

						numberOfModules++;
						GUIStyle moduleStyle = jammer.jammerEnabled ? centerLabelBlue : centerLabel;
						string label = jammer.part.partInfo.title;
						if(GUI.Button(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), label, moduleStyle))
						{
							jammer.Toggle();
						}
						line++;
					}

					//GPS coordinator
					GUIStyle gpsModuleStyle = showGPSWindow ? centerLabelBlue : centerLabel;
					numberOfModules++;
					if(GUI.Button(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), "GPS Coordinator", gpsModuleStyle))
					{
						showGPSWindow = !showGPSWindow;
					}
					line++;


					if(numberOfModules == 0)
					{
						GUI.Box(new Rect(5,  contentTop+(line*entryHeight), toolWindowWidth-10, 1 * entryHeight),"");
						GUI.Label(new Rect(5,  contentTop+(line*entryHeight), toolWindowWidth-10, 1 * entryHeight),"No modules.", centerLabel);
						line++;
					}

				}
				
			}
			else
			{
				GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), "No Weapon Manager found.", centerLabel);
				line++;
			}
			
			
			
			
			toolWindowHeight = contentTop + (line*entryHeight) + 5;
			toolbarWindowRect = new Rect(toolbarWindowRect.position.x, toolbarWindowRect.position.y, toolWindowWidth, toolWindowHeight);
		}

		bool validGPSName = true;

		//GPS window
		void GPSWindow(int windowID)
		{
			gpsEntryCount = 0;
			Rect listRect = new Rect(gpsBorder, gpsBorder, gpsWindowRect.width - (2 * gpsBorder), gpsWindowRect.height - (2 * gpsBorder));
			GUI.BeginGroup(listRect);
			GUI.Label(new Rect(0, 0, listRect.width, gpsEntryHeight), "Designated GPS Targets:", centerLabel);
			gpsEntryCount++;
			if(ActiveWeaponManager.designatedGPSCoords != Vector3d.zero)
			{
				GUI.Label(new Rect(0, gpsEntryHeight, listRect.width - gpsEntryHeight, gpsEntryHeight), Misc.FormattedGeoPos(ActiveWeaponManager.designatedGPSCoords, true), centerLabelOrange);
				if(GUI.Button(new Rect(listRect.width - gpsEntryHeight, gpsEntryHeight, gpsEntryHeight, gpsEntryHeight), "X"))
				{
					ActiveWeaponManager.designatedGPSCoords = Vector3d.zero;
				}
			}
			else
			{
				GUI.Label(new Rect(0, gpsEntryHeight, listRect.width - gpsEntryHeight, gpsEntryHeight), "No Target", centerLabelOrange);
			}
			gpsEntryCount++;
			int indexToRemove = -1;
			int index = 0;
			BDATeams myTeam = BDATargetManager.BoolToTeam(ActiveWeaponManager.team);
			foreach(var coordinate in BDATargetManager.GPSTargets[myTeam])
			{
				string label = Misc.FormattedGeoPosShort(coordinate.gpsCoordinates, false);
				float nameWidth = 100;
				if(editingGPSName && index == editingGPSNameIndex)
				{
					if(validGPSName && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
					{
						editingGPSName = false;
						hasEnteredGPSName = true;
					}
					else
					{
						Color origColor = GUI.color;
						if(newGPSName.Contains(";") || newGPSName.Contains(":") || newGPSName.Contains(","))
						{
							validGPSName = false;
							GUI.color = Color.red;
						}
						else
						{
							validGPSName = true;
						}
						newGPSName = GUI.TextField(new Rect(0, gpsEntryCount * gpsEntryHeight, nameWidth, gpsEntryHeight), newGPSName, 12);
						GUI.color = origColor;
					}
				}
				else
				{
					if(GUI.Button(new Rect(0, gpsEntryCount * gpsEntryHeight, nameWidth, gpsEntryHeight), coordinate.name))
					{
						editingGPSName = true;
						editingGPSNameIndex = index;
						newGPSName = coordinate.name;
					}
				}
				if(GUI.Button(new Rect(nameWidth, gpsEntryCount * gpsEntryHeight, listRect.width - gpsEntryHeight - nameWidth, gpsEntryHeight), label))
				{
					ActiveWeaponManager.designatedGPSCoords = coordinate.gpsCoordinates;
					editingGPSName = false;
				}
				if(GUI.Button(new Rect(listRect.width - gpsEntryHeight, gpsEntryCount * gpsEntryHeight, gpsEntryHeight, gpsEntryHeight), "X"))
				{
					indexToRemove = index;
				}
				gpsEntryCount++;
				index++;
			}
			if(hasEnteredGPSName && editingGPSNameIndex < BDATargetManager.GPSTargets[myTeam].Count)
			{
				hasEnteredGPSName = false;
				BDATargetManager.GPSTargets[myTeam][editingGPSNameIndex] = new GPSTargetInfo(BDATargetManager.GPSTargets[myTeam][editingGPSNameIndex].gpsCoordinates, newGPSName);
				editingGPSNameIndex = 0;
			}

			GUI.EndGroup();

			if(indexToRemove >= 0)
			{
				BDATargetManager.GPSTargets[myTeam].RemoveAt(indexToRemove);
			}

			gpsWindowRect.x = toolbarWindowRect.x;
			gpsWindowRect.y = toolbarWindowRect.y + toolbarWindowRect.height;
			gpsWindowRect.height = (2*gpsBorder) + (gpsEntryCount * gpsEntryHeight);
		}






		Rect SLineRect(float line)
		{
			return new Rect(settingsLeftMargin, settingsTop + line * settingsSpacer, settingsWidth - 2 * settingsSpacer, settingsSpacer);
		}

		Rect SRightRect(float line)
		{
			return new Rect(settingsLeftMargin + ((settingsWidth - 2 * settingsSpacer) / 2), settingsTop + line * settingsSpacer, (settingsWidth - 2 * settingsSpacer) / 2, settingsSpacer);
		}

		Rect SLeftRect(float line)
		{
			return new Rect(settingsLeftMargin, settingsTop + (line * settingsSpacer), (settingsWidth - (2*settingsSpacer))/2, settingsSpacer);
		}

		float settingsWidth;
		float settingsHeight;
		float settingsLeft;
		float settingsTop;
		float settingsSpacer;
		float settingsLeftMargin;
		void SetupSettingsSize()
		{
			settingsWidth = 360;
			settingsHeight = 480;
			settingsLeft = Screen.width/2 - settingsWidth/2;
			settingsTop = Screen.height/2 - settingsHeight/2;
			settingsSpacer = 24;
			settingsLeftMargin = settingsLeft+18;
		}

		void SettingsGUI()
		{
			
			float line = 1.25f;
			GUI.Box(new Rect(settingsLeft, settingsTop, settingsWidth, settingsHeight), "");
			GUI.Box(new Rect(settingsLeft, settingsTop, settingsWidth, settingsHeight), "BDArmory Settings");
			INSTAKILL = GUI.Toggle(SLeftRect(line), INSTAKILL, "Instakill");
			INFINITE_AMMO = GUI.Toggle(SRightRect(line), INFINITE_AMMO, "Infinte Ammo");
			line++;
			BULLET_HITS = GUI.Toggle(SLeftRect(line), BULLET_HITS, "Bullet Hits");
			EJECT_SHELLS = GUI.Toggle(SRightRect(line), EJECT_SHELLS, "Eject Shells");
			line++;
			AIM_ASSIST = GUI.Toggle(SLeftRect(line), AIM_ASSIST, "Aim Assist");
			DRAW_AIMERS = GUI.Toggle(SRightRect(line), DRAW_AIMERS, "Draw Aimers");
			line++;
			DRAW_DEBUG_LINES = GUI.Toggle(SLeftRect(line), DRAW_DEBUG_LINES, "Debug Lines");
			DRAW_DEBUG_LABELS = GUI.Toggle(SRightRect(line), DRAW_DEBUG_LABELS, "Debug Labels");
			line++;
			REMOTE_SHOOTING = GUI.Toggle(SLeftRect(line), REMOTE_SHOOTING, "Remote Firing");
			BOMB_CLEARANCE_CHECK = GUI.Toggle(SRightRect(line), BOMB_CLEARANCE_CHECK, "Clearance Check");
			line++;
			ALLOW_LEGACY_TARGETING = GUI.Toggle(SLeftRect(line), ALLOW_LEGACY_TARGETING, "Legacy Targeting");
			line++;
			line++;

			string gunFireKeyLabel;
			if(isRecordingInput)
			{
				gunFireKeyLabel = "Press a key or button.";

				string inputString = BDInputUtils.GetInputString();
				if(inputString.Length > 0 && recordMouseUp)
				{
					FIRE_KEY = inputString;
					isRecordingInput = false;
				}

				if(Input.GetKeyUp (KeyCode.Mouse0))
				{
					recordMouseUp = true;
				}
			}
			else
			{
				gunFireKeyLabel = "Fire key: "+FIRE_KEY;

				if(GUI.Button(new Rect(settingsLeftMargin + 200, settingsTop + line*settingsSpacer, 100-(settingsLeftMargin-settingsLeft), settingsSpacer), "Set Key"))
				{
					recordMouseUp = false;
					isRecordingInput = true;
				}
			}
			GUI.Label(SLineRect(line), gunFireKeyLabel);
			line++;

			GUI.Label(new Rect(settingsLeftMargin, settingsTop + line*settingsSpacer, (settingsWidth-2*settingsSpacer)/2, settingsSpacer), "Trigger Hold: "+TRIGGER_HOLD_TIME.ToString("0.00")+"s");
			TRIGGER_HOLD_TIME = GUI.HorizontalSlider(new Rect(settingsLeftMargin+((settingsWidth-2*settingsSpacer)/2), settingsTop + line*settingsSpacer, (settingsWidth-2*settingsSpacer)/2, settingsSpacer),TRIGGER_HOLD_TIME, 0.02f, 1f);
			line++;


			GUI.Label(new Rect(settingsLeftMargin, settingsTop + line*settingsSpacer, (settingsWidth-2*settingsSpacer)/2, settingsSpacer), "UI Volume: "+(BDARMORY_UI_VOLUME*100).ToString("0"));
			float uiVol = BDARMORY_UI_VOLUME;
			uiVol = GUI.HorizontalSlider(new Rect(settingsLeftMargin+((settingsWidth-2*settingsSpacer)/2), settingsTop + line*settingsSpacer, (settingsWidth-2*settingsSpacer)/2, settingsSpacer),uiVol, 0f, 1f);
			if(uiVol != BDARMORY_UI_VOLUME && OnVolumeChange != null)
			{
				OnVolumeChange();
			}
			BDARMORY_UI_VOLUME = uiVol;
			line++;

			GUI.Label(new Rect(settingsLeftMargin, settingsTop + line*settingsSpacer, (settingsWidth-2*settingsSpacer)/2, settingsSpacer), "Weapon Volume: "+(BDARMORY_WEAPONS_VOLUME*100).ToString("0"));
			float weaponVol = BDARMORY_WEAPONS_VOLUME;
			weaponVol = GUI.HorizontalSlider(new Rect(settingsLeftMargin+((settingsWidth-2*settingsSpacer)/2), settingsTop + line*settingsSpacer, (settingsWidth-2*settingsSpacer)/2, settingsSpacer),weaponVol, 0f, 1f);
			if(uiVol != BDARMORY_WEAPONS_VOLUME && OnVolumeChange != null)
			{
				OnVolumeChange();
			}
			BDARMORY_WEAPONS_VOLUME = weaponVol;
			line++;
			line++;

			physicsRangeGui = GUI.TextField(new Rect(Screen.width/2, settingsTop + line*settingsSpacer, settingsWidth/2 - settingsSpacer, settingsSpacer), physicsRangeGui);
			GUI.Label(SLineRect(line), "Physics Load Distance");
			line++;
			GUI.Label(new Rect(Screen.width/2, settingsTop + line*settingsSpacer, settingsWidth/2 - settingsSpacer, 2*settingsSpacer), "Warning: Risky if set high");
			if(GUI.Button(new Rect(settingsLeftMargin, settingsTop + line*settingsSpacer, settingsWidth/2 - 2*settingsSpacer+8, settingsSpacer), "Apply Phys Distance"))
			{
				float physRangeSetting = float.Parse(physicsRangeGui);
				PHYSICS_RANGE = (physRangeSetting>=2500 ? Mathf.Clamp(physRangeSetting, 2500, 100000) : 0);
				physicsRangeGui = PHYSICS_RANGE.ToString();
				ApplyPhysRange();
			}
			
			line++;
			
			if(GUI.Button(new Rect(settingsLeftMargin, settingsTop + line*settingsSpacer +26, settingsWidth/2 - 2*settingsSpacer+8, settingsSpacer), "Save and Close"))
			{
				SaveConfig();
				settingsGuiEnabled = false;
			}

			line+=3;
			settingsHeight = (line * settingsSpacer);
		}
		
		
		#endregion
		
		public void ApplyPhysRange()
		{
			if(!HighLogic.LoadedSceneIsFlight)
			{
				return;
			}

			if(PHYSICS_RANGE <= 2500) PHYSICS_RANGE = 0;
			
			
			if(PHYSICS_RANGE > 0)
			{
				float pack = PHYSICS_RANGE;
				float unload = PHYSICS_RANGE * 0.9f;
				float load = unload * 0.9f;
				float unpack = load * 0.9f;

				VesselRanges defaultRanges = PhysicsGlobals.Instance.VesselRangesDefault;
				VesselRanges.Situation combatSituation = new VesselRanges.Situation(load, unload, pack, unpack);

				
				
				VesselRanges.Situation combatFlyingSituation = ClampedSituation(combatSituation, defaultRanges.flying);
				VesselRanges.Situation combatLandedSituation = ClampedSituationLanded(combatSituation, defaultRanges.landed);
				VesselRanges.Situation combatSplashedSituation = ClampedSituation(combatSituation, defaultRanges.splashed);
				VesselRanges.Situation combatOrbitSituation = ClampedSituation(combatSituation, defaultRanges.orbit);
				VesselRanges.Situation combatSubOrbitSituation = ClampedSituation(combatSituation, defaultRanges.subOrbital);
				VesselRanges.Situation combatPrelaunchSituation = ClampedSituation(combatSituation, defaultRanges.prelaunch);

				combatVesselRanges.flying = combatFlyingSituation;
				combatVesselRanges.landed = combatLandedSituation;
				combatVesselRanges.splashed = combatSplashedSituation;
				combatVesselRanges.orbit = combatOrbitSituation;
				combatVesselRanges.subOrbital = combatSubOrbitSituation;
				combatVesselRanges.prelaunch = combatPrelaunchSituation;

				foreach(Vessel v in FlightGlobals.Vessels)
				{
					v.vesselRanges = new VesselRanges(combatVesselRanges);
				}
				
				FloatingOrigin.fetch.threshold = Mathf.Pow(PHYSICS_RANGE + 3500, 2);
			}
			else
			{
				foreach(Vessel v in FlightGlobals.Vessels)
				{
					v.vesselRanges = PhysicsGlobals.Instance.VesselRangesDefault;
				}
				
				FloatingOrigin.fetch.threshold = Mathf.Pow(6000, 2);
			}
		}

		private VesselRanges.Situation ClampedSituation(VesselRanges.Situation input, VesselRanges.Situation minSituation)
		{
			float load = Mathf.Clamp(input.load, minSituation.load, 81000);
			float unload = Mathf.Clamp(input.unload, minSituation.unload, 90000);
			float pack = Mathf.Clamp(input.pack, minSituation.pack, 100000);
			float unpack = Mathf.Clamp(input.unpack, minSituation.unpack, 72900);
		
			VesselRanges.Situation output = new VesselRanges.Situation(load, unload, pack, unpack);
			return output;
		
		}

		private VesselRanges.Situation ClampedSituationLanded(VesselRanges.Situation input, VesselRanges.Situation minSituation)
		{
			float maxLanded = 11000;
			float load = Mathf.Clamp(input.load, minSituation.load, maxLanded*.9f*.9f);
			float unload = Mathf.Clamp(input.unload, minSituation.unload, maxLanded*.9f);
			float pack = Mathf.Clamp(input.pack, minSituation.pack, maxLanded);
			float unpack = Mathf.Clamp(input.unpack, minSituation.unpack, maxLanded*.9f*.9f*.9f);
			
			VesselRanges.Situation output = new VesselRanges.Situation(load, unload, pack, unpack);
			return output;
		}

		public void ApplyNewVesselRanges(Vessel v)
		{
			v.vesselRanges = new VesselRanges(combatVesselRanges);
		}
		
		void HideGameUI()
		{
			GAME_UI_ENABLED = false;	
		}
		
		void ShowGameUI()
		{
			GAME_UI_ENABLED = true;	
		}
		

		

		
		void OnVesselGoOffRails(Vessel v)
		{
			if(v.Landed && BDArmorySettings.DRAW_DEBUG_LABELS)
			{
				Debug.Log ("Loaded vessel: "+v.vesselName+", Velocity: "+v.srf_velocity+", packed: "+v.packed);
				//v.SetWorldVelocity(Vector3d.zero);	
			}
		}
		
		public void SaveVolumeSettings()
		{
			SeismicChargeFX.originalShipVolume = GameSettings.SHIP_VOLUME;
			SeismicChargeFX.originalMusicVolume = GameSettings.MUSIC_VOLUME;
			SeismicChargeFX.originalAmbienceVolume = GameSettings.AMBIENCE_VOLUME;	
		}
		
	}
}

