using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace BahaTurret
{
	[KSPAddon(KSPAddon.Startup.EveryScene, false)]
	public class BDArmorySettings : MonoBehaviour
	{
		

		//=======configurable settings
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
		public static float MAX_BULLET_RANGE = 8000;
		public static float TRIGGER_HOLD_TIME = 0.3f;

		public static bool ALLOW_LEGACY_TARGETING = true;

		public static float TARGET_CAM_RESOLUTION = 1024;
		public static bool BW_TARGET_CAM = true;
		public static float SMOKE_DEFLECTION_FACTOR = 10;

		public static float FLARE_THERMAL = 1900;

		public static float BDARMORY_UI_VOLUME = 0.35f; 
		public static float BDARMORY_WEAPONS_VOLUME = 0.32f;

		public static float MAX_GUARD_VISUAL_RANGE = 3500;


		public static float GLOBAL_LIFT_MULTIPLIER = 0.20f;
		public static float GLOBAL_DRAG_MULTIPLIER = 4f;

		public static float IVA_LOWPASS_FREQ = 2500;

		public static bool PEACE_MODE = false;

		//==================
		//reflection field lists
		FieldInfo[] iFs = null;
		FieldInfo[] inputFields
		{
			get
			{
				if(iFs == null)
				{
					iFs = typeof(BDInputSettingsFields).GetFields();
				}
				return iFs;
			}
		}

		//EVENTS
		public delegate void VolumeChange();
		public static event VolumeChange OnVolumeChange;

		public delegate void SavedSettings();
		public static event SavedSettings OnSavedSettings;

		public delegate void PeaceEnabled();
		public static event PeaceEnabled OnPeaceEnabled;

		//particle optimization
		public static int numberOfParticleEmitters = 0;
	
		
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
		float gpsEntryCount = 0;
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


		//gui styles
		GUIStyle centerLabel;
		GUIStyle centerLabelRed;
		GUIStyle centerLabelOrange;
		GUIStyle centerLabelBlue;
		GUIStyle leftLabel;
		GUIStyle leftLabelRed;
		GUIStyle rightLabelRed;
		GUIStyle leftLabelGray;
		GUIStyle rippleSliderStyle;
		GUIStyle rippleThumbStyle;
		GUIStyle kspTitleLabel;


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

		private Texture2D si;
		public Texture2D settingsIconTexture
		{
			get
			{
				return si ? si : si = GameDatabase.Instance.GetTexture(textureDir + "settingsIcon", false);
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
			

			fireKeyGui = BDInputSettingsFields.WEAP_FIRE_KEY.inputString;

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

			rippleSliderStyle = new GUIStyle(HighLogic.Skin.horizontalSlider);
			rippleThumbStyle = new GUIStyle(HighLogic.Skin.horizontalSliderThumb);
			rippleSliderStyle.fixedHeight = rippleThumbStyle.fixedHeight = 0;

			kspTitleLabel = new GUIStyle();
			kspTitleLabel.normal.textColor = HighLogic.Skin.window.normal.textColor;
			kspTitleLabel.font = HighLogic.Skin.window.font;
			kspTitleLabel.fontSize = HighLogic.Skin.window.fontSize;
			kspTitleLabel.fontStyle = HighLogic.Skin.window.fontStyle;
			kspTitleLabel.alignment = TextAnchor.UpperCenter;
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


				/*
				foreach(var cam in FlightCamera.fetch.cameras)
				{
					cam.gameObject.AddComponent<CameraBulletRenderer>();
				}
				*/

				gpsWindowRect = new Rect(0, 0, toolbarWindowRect.width-10, 0);

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
					ToggleSettingsGUI();
				}
			}
			
		}

		void ToggleSettingsGUI()
		{
			if(HighLogic.LoadedScene == GameScenes.LOADING || HighLogic.LoadedScene == GameScenes.LOADINGBUFFER)
			{
				return;
			}

			settingsGuiEnabled = !settingsGuiEnabled;
			if(settingsGuiEnabled)
			{
				physicsRangeGui = PHYSICS_RANGE.ToString();
				LoadConfig();
			}
			else
			{
				SaveConfig();
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
				ConfigNode fileNode = ConfigNode.Load("GameData/BDArmory/settings.cfg");

				if(!fileNode.HasNode("BDASettings"))
				{
					fileNode.AddNode("BDASettings");
				}

				ConfigNode cfg = fileNode.GetNode("BDASettings");

				//if(cfg.HasValue("FireKey")) FIRE_KEY = cfg.GetValue("FireKey");	
				
				if(cfg.HasValue("INSTAKILL")) INSTAKILL = bool.Parse(cfg.GetValue("INSTAKILL"));	
				
				if(cfg.HasValue("BULLET_HITS")) BULLET_HITS = bool.Parse(cfg.GetValue("BULLET_HITS"));	
				
				if(cfg.HasValue("PHYSICS_RANGE")) PHYSICS_RANGE = float.Parse(cfg.GetValue("PHYSICS_RANGE"));	
				
				if(cfg.HasValue("EJECT_SHELLS")) EJECT_SHELLS = bool.Parse(cfg.GetValue("EJECT_SHELLS"));
				
				if(cfg.HasValue("INFINITE_AMMO")) INFINITE_AMMO = bool.Parse(cfg.GetValue("INFINITE_AMMO"));
				
				if(cfg.HasValue("DRAW_DEBUG_LINES")) DRAW_DEBUG_LINES = bool.Parse(cfg.GetValue("DRAW_DEBUG_LINES"));
				
				if(cfg.HasValue("DRAW_AIMERS")) DRAW_AIMERS = bool.Parse(cfg.GetValue ("DRAW_AIMERS"));
				
				if(cfg.HasValue("AIM_ASSIST")) AIM_ASSIST = bool.Parse(cfg.GetValue("AIM_ASSIST"));
				
				if(cfg.HasValue("REMOTE_SHOOTING")) REMOTE_SHOOTING = bool.Parse(cfg.GetValue("REMOTE_SHOOTING"));
				
				if(cfg.HasValue("DMG_MULTIPLIER")) DMG_MULTIPLIER = float.Parse(cfg.GetValue("DMG_MULTIPLIER"));
				
				if(cfg.HasValue("FLARE_CHANCE_FACTOR")) FLARE_CHANCE_FACTOR = float.Parse(cfg.GetValue("FLARE_CHANCE_FACTOR"));
				
				if(cfg.HasValue("BOMB_CLEARANCE_CHECK")) BOMB_CLEARANCE_CHECK = bool.Parse(cfg.GetValue("BOMB_CLEARANCE_CHECK"));

				if(cfg.HasValue("SMART_GUARDS")) SMART_GUARDS = bool.Parse(cfg.GetValue("SMART_GUARDS"));

				if(cfg.HasValue("MAX_BULLET_RANGE")) MAX_BULLET_RANGE = float.Parse(cfg.GetValue("MAX_BULLET_RANGE"));	

				if(cfg.HasValue("TRIGGER_HOLD_TIME")) TRIGGER_HOLD_TIME = float.Parse(cfg.GetValue("TRIGGER_HOLD_TIME"));

				if(cfg.HasValue("ALLOW_LEGACY_TARGETING")) ALLOW_LEGACY_TARGETING = bool.Parse(cfg.GetValue("ALLOW_LEGACY_TARGETING"));

				if(cfg.HasValue("TARGET_CAM_RESOLUTION")) TARGET_CAM_RESOLUTION = float.Parse(cfg.GetValue("TARGET_CAM_RESOLUTION"));

				if(cfg.HasValue("BW_TARGET_CAM")) BW_TARGET_CAM = bool.Parse(cfg.GetValue("BW_TARGET_CAM"));

				if(cfg.HasValue("SMOKE_DEFLECTION_FACTOR")) SMOKE_DEFLECTION_FACTOR = float.Parse(cfg.GetValue("SMOKE_DEFLECTION_FACTOR"));

				if(cfg.HasValue("FLARE_THERMAL")) FLARE_THERMAL = float.Parse(cfg.GetValue("FLARE_THERMAL"));

				if(cfg.HasValue("BDARMORY_UI_VOLUME")) BDARMORY_UI_VOLUME = float.Parse(cfg.GetValue("BDARMORY_UI_VOLUME"));

				if(cfg.HasValue("BDARMORY_WEAPONS_VOLUME")) BDARMORY_WEAPONS_VOLUME = float.Parse(cfg.GetValue("BDARMORY_WEAPONS_VOLUME"));

				if(cfg.HasValue("MAX_GUARD_VISUAL_RANGE")) MAX_GUARD_VISUAL_RANGE = float.Parse(cfg.GetValue("MAX_GUARD_VISUAL_RANGE"));

				if(cfg.HasValue("GLOBAL_LIFT_MULTIPLIER")) GLOBAL_LIFT_MULTIPLIER = float.Parse(cfg.GetValue("GLOBAL_LIFT_MULTIPLIER"));

				if(cfg.HasValue("GLOBAL_DRAG_MULTIPLIER")) GLOBAL_DRAG_MULTIPLIER = float.Parse(cfg.GetValue("GLOBAL_DRAG_MULTIPLIER"));

				if(cfg.HasValue("IVA_LOWPASS_FREQ")) IVA_LOWPASS_FREQ = float.Parse(cfg.GetValue("IVA_LOWPASS_FREQ"));

				if(cfg.HasValue("PEACE_MODE")) PEACE_MODE = bool.Parse(cfg.GetValue("PEACE_MODE"));

				BDInputSettingsFields.LoadSettings(fileNode);
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
				ConfigNode fileNode = ConfigNode.Load("GameData/BDArmory/settings.cfg");


				if(!fileNode.HasNode("BDASettings"))
				{
					fileNode.AddNode("BDASettings");
				}

				ConfigNode cfg = fileNode.GetNode("BDASettings");


				//cfg.SetValue("FireKey", FIRE_KEY, true);
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
				cfg.SetValue("IVA_LOWPASS_FREQ", IVA_LOWPASS_FREQ.ToString(), true);
				cfg.SetValue("MAX_BULLET_RANGE", MAX_BULLET_RANGE.ToString(), true);
				cfg.SetValue("PEACE_MODE", PEACE_MODE.ToString(), true);
				cfg.SetValue("MAX_GUARD_VISUAL_RANGE", MAX_GUARD_VISUAL_RANGE.ToString(), true);

				BDInputSettingsFields.SaveSettings(fileNode);

				fileNode.Save ("GameData/BDArmory/settings.cfg");

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
					settingsRect = GUI.Window(129419, settingsRect, SettingsGUI, GUIContent.none);
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
					toolbarWindowRect = GUI.Window(321, toolbarWindowRect, ToolbarGUI, "BDA Weapon Manager", HighLogic.Skin.window);
					BDGUIUtils.UseMouseEventInRect(toolbarWindowRect);
					if(showGPSWindow && ActiveWeaponManager)
					{
						//gpsWindowRect = GUI.Window(424333, gpsWindowRect, GPSWindow, "", GUI.skin.box);
						BDGUIUtils.UseMouseEventInRect(gpsWindowRect);
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

		float rippleHeight = 0;
		float weaponsHeight = 0;
		float guardHeight = 0;
		float modulesHeight = 0;
		float gpsHeight = 0;
		bool toolMinimized = false;

		void ToolbarGUI(int windowID)
		{
			GUI.DragWindow(new Rect(30,0,toolWindowWidth-60, 30));

			float line = 0;
			float leftIndent = 10;
			float contentWidth = (toolWindowWidth) - (2*leftIndent);
			float contentTop = 10;
			float entryHeight = 20;
			
			//GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight*1.25f), , HighLogic.Skin.label);
			/*
			if(missileWarning) 
			{
				GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), "Missile", leftLabelRed);
				GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), "Lock", rightLabelRed);
			}
			*/
			line += 1.25f;
			line += 0.25f;
			
			if(ActiveWeaponManager!=null)
			{
				//MINIMIZE BUTTON
				toolMinimized = GUI.Toggle(new Rect(4, 4, 26, 26), toolMinimized, "_", toolMinimized ? HighLogic.Skin.box : HighLogic.Skin.button);
			
				//SETTINGS BUTTON
				if(!BDKeyBinder.current && GUI.Button(new Rect(toolWindowWidth - 30, 4, 26, 26), settingsIconTexture, HighLogic.Skin.button))
				{
					ToggleSettingsGUI();
				}

				GUIStyle armedLabelStyle;
				Rect armedRect = new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth / 2, entryHeight);
				if(ActiveWeaponManager.guardMode)
				{
					if(GUI.Button(armedRect, "- Guard Mode -", HighLogic.Skin.box))
					{
						showGuardMenu = true;
					}
				}
				else
				{
					string armedText = "Trigger is ";
					if(ActiveWeaponManager.isArmed)
					{
						armedText += "ARMED.";
						armedLabelStyle = HighLogic.Skin.box;
					}
					else
					{
						armedText += "disarmed.";
						armedLabelStyle = HighLogic.Skin.button;
					}
					if(GUI.Button(armedRect, armedText, armedLabelStyle))
					{
						ActiveWeaponManager.ToggleArm();
					}
				}
				
				GUIStyle teamButtonStyle;
				string teamText = "Team: ";
				if(ActiveWeaponManager.team)
				{
					teamButtonStyle = HighLogic.Skin.box;
					teamText += "B";
				}
				else
				{
					teamButtonStyle = HighLogic.Skin.button;	
					teamText += "A";
				}
				
				if(GUI.Button(new Rect(leftIndent+(contentWidth/2), contentTop+(line*entryHeight), contentWidth/2, entryHeight), teamText, teamButtonStyle))
				{
					ActiveWeaponManager.ToggleTeam();
				}
				line++;
				line += 0.25f;
				string weaponName = ActiveWeaponManager.selectedWeaponString;// = ActiveWeaponManager.selectedWeapon == null ? "None" : ActiveWeaponManager.selectedWeapon.GetShortName();
				string selectionText = "Weapon: "+weaponName;
				GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight*1.25f), selectionText, HighLogic.Skin.box);
				line += 1.25f;
				line += 0.1f;
				//if weapon can ripple, show option and slider.
				if(ActiveWeaponManager.canRipple)
				{
					string rippleText = ActiveWeaponManager.rippleFire ? "Ripple: " + ActiveWeaponManager.rippleRPM.ToString("0") + " RPM" : "Ripple: OFF";
					GUIStyle rippleStyle = ActiveWeaponManager.rippleFire ? HighLogic.Skin.box : HighLogic.Skin.button;
					ActiveWeaponManager.rippleFire = GUI.Toggle(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth / 2, entryHeight * 1.25f), ActiveWeaponManager.rippleFire, rippleText, rippleStyle);
					if(ActiveWeaponManager.rippleFire)
					{
						Rect sliderRect = new Rect(leftIndent + (contentWidth / 2) + 2, contentTop + (line * entryHeight) + 6.5f, (contentWidth / 2) - 2, 12);
						ActiveWeaponManager.rippleRPM = GUI.HorizontalSlider(sliderRect, ActiveWeaponManager.rippleRPM, 100, 1600, rippleSliderStyle, rippleThumbStyle);
					}
					rippleHeight = Mathf.Lerp(rippleHeight, 1.25f, 0.15f);
				}
				else
				{
					rippleHeight = Mathf.Lerp(rippleHeight, 0, 0.15f);
				}
				//line += 1.25f;
				line+=rippleHeight;
				line += 0.1f;

				if(!toolMinimized)
				{
					showWeaponList = GUI.Toggle(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth / 3, entryHeight), showWeaponList, "Weapons", showWeaponList ? HighLogic.Skin.box : HighLogic.Skin.button);
					showGuardMenu = GUI.Toggle(new Rect(leftIndent + (contentWidth / 3), contentTop + (line * entryHeight), contentWidth / 3, entryHeight), showGuardMenu, "Guard Menu", showGuardMenu ? HighLogic.Skin.box : HighLogic.Skin.button);
					showModules = GUI.Toggle(new Rect(leftIndent + (2 * contentWidth / 3), contentTop + (line * entryHeight), contentWidth / 3, entryHeight), showModules, "Modules", showModules ? HighLogic.Skin.box : HighLogic.Skin.button);
					line++;
				}

				float weaponLines = 0;
				if(showWeaponList && !toolMinimized)
				{
					line += 0.25f;
					GUI.BeginGroup(new Rect(5,  contentTop+(line*entryHeight), toolWindowWidth-10, ActiveWeaponManager.weaponArray.Length * entryHeight),GUIContent.none, HighLogic.Skin.box); //darker box
					weaponLines += 0.1f;
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
						if(GUI.Button(new Rect(leftIndent, (weaponLines*entryHeight), contentWidth, entryHeight), label, wpnListStyle))
						{
							ActiveWeaponManager.CycleWeapon(i);
						}
						weaponLines++;
					}
					weaponLines += 0.1f;
					GUI.EndGroup();
				}
				weaponsHeight = Mathf.Lerp(weaponsHeight, weaponLines, 0.15f);
				line += weaponsHeight;

				float guardLines = 0;
				if(showGuardMenu && !toolMinimized)
				{
					line += 0.25f;
					GUI.BeginGroup(new Rect(5, contentTop+(line*entryHeight), toolWindowWidth-10, 5.45f*entryHeight), GUIContent.none, HighLogic.Skin.box);
					guardLines += 0.1f;
					contentWidth -= 16;
					leftIndent += 3;
					string guardButtonLabel = "Guard Mode " + (ActiveWeaponManager.guardMode ? "ON" : "Off");
					if(GUI.Button(new Rect(leftIndent, (guardLines * entryHeight), contentWidth, entryHeight), guardButtonLabel, ActiveWeaponManager.guardMode ? HighLogic.Skin.box : HighLogic.Skin.button))
					{
						ActiveWeaponManager.ToggleGuardMode();
					}
					guardLines += 1.25f;

					string scanLabel = ALLOW_LEGACY_TARGETING ? "Scan Interval" : "Firing Interval";
					GUI.Label(new Rect(leftIndent, (guardLines*entryHeight), 85, entryHeight), scanLabel, leftLabel);
					ActiveWeaponManager.targetScanInterval = GUI.HorizontalSlider(new Rect(leftIndent+(90), (guardLines*entryHeight), contentWidth-90-38, entryHeight), ActiveWeaponManager.targetScanInterval, 1, 60);
					ActiveWeaponManager.targetScanInterval = Mathf.Round(ActiveWeaponManager.targetScanInterval);
					GUI.Label(new Rect(leftIndent+(contentWidth-35), (guardLines*entryHeight), 35, entryHeight), ActiveWeaponManager.targetScanInterval.ToString(), leftLabel);
					guardLines++;
					
					GUI.Label(new Rect(leftIndent, (guardLines*entryHeight), 85, entryHeight), "Field of View", leftLabel);
					float guardAngle = ActiveWeaponManager.guardAngle;
					guardAngle = GUI.HorizontalSlider(new Rect(leftIndent+90, (guardLines*entryHeight), contentWidth-90-38, entryHeight), guardAngle, 10, 360);
					guardAngle = guardAngle/10;
					guardAngle = Mathf.Round(guardAngle);
					ActiveWeaponManager.guardAngle = guardAngle * 10;
					GUI.Label(new Rect(leftIndent+(contentWidth-35), (guardLines*entryHeight), 35, entryHeight), ActiveWeaponManager.guardAngle.ToString(), leftLabel);
					guardLines++;

					string rangeLabel = ALLOW_LEGACY_TARGETING ? "Guard Range" : "Visual Range";
					GUI.Label(new Rect(leftIndent, (guardLines*entryHeight), 85, entryHeight), rangeLabel, leftLabel);
					float guardRange = ActiveWeaponManager.guardRange;
					float maxVisRange = ALLOW_LEGACY_TARGETING ? Mathf.Clamp(PHYSICS_RANGE, 2500, 100000) : BDArmorySettings.MAX_GUARD_VISUAL_RANGE;
					guardRange = GUI.HorizontalSlider(new Rect(leftIndent+90, (guardLines*entryHeight), contentWidth-90-38, entryHeight), guardRange, 100, maxVisRange);
					guardRange = guardRange/100;
					guardRange = Mathf.Round(guardRange);
					ActiveWeaponManager.guardRange = guardRange * 100;
					GUI.Label(new Rect(leftIndent+(contentWidth-35), (guardLines*entryHeight), 35, entryHeight), ActiveWeaponManager.guardRange.ToString(), leftLabel);
					guardLines++;
					
					string targetType = "Target Type: ";
					if(ActiveWeaponManager.targetMissiles)
					{
						targetType += "Missiles";	
					}
					else
					{
						targetType += "Vessels";	
					}
					
					if(GUI.Button(new Rect(leftIndent, (guardLines*entryHeight), contentWidth, entryHeight), targetType, HighLogic.Skin.button))
					{
						ActiveWeaponManager.ToggleTargetType();	
					}
					guardLines++;
					GUI.EndGroup();
					line += 0.1f;
				}
				guardHeight = Mathf.Lerp(guardHeight, guardLines, 0.15f);
				line += guardHeight;

				float moduleLines = 0;
				if(showModules && !toolMinimized)
				{
					line += 0.25f;
					GUI.BeginGroup(new Rect(5,  contentTop+(line*entryHeight), toolWindowWidth-10, numberOfModules * entryHeight),GUIContent.none, HighLogic.Skin.box); 

					numberOfModules = 0;
					moduleLines += 0.1f;
					//RWR
					if(ActiveWeaponManager.rwr)
					{
						numberOfModules++;
						bool isEnabled = ActiveWeaponManager.rwr.rwrEnabled;
						string label = "Radar Warning Receiver";
						Rect rwrRect = new Rect(leftIndent,  + (moduleLines * entryHeight), contentWidth, entryHeight);
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
						moduleLines++;
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
						if(GUI.Button(new Rect(leftIndent, +(moduleLines*entryHeight), contentWidth, entryHeight), label, moduleStyle))
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
						moduleLines++;
					}

					//RADAR
					foreach(var mr in ActiveWeaponManager.radars)
					{
						numberOfModules++;
						GUIStyle moduleStyle = mr.radarEnabled ? centerLabelBlue : centerLabel;
						string label = mr.part.partInfo.title;
						if(GUI.Button(new Rect(leftIndent, +(moduleLines*entryHeight), contentWidth, entryHeight), label, moduleStyle))
						{
							mr.Toggle();
						}
						moduleLines++;
					}

					//JAMMERS
					foreach(var jammer in ActiveWeaponManager.jammers)
					{
						if(jammer.alwaysOn) continue;

						numberOfModules++;
						GUIStyle moduleStyle = jammer.jammerEnabled ? centerLabelBlue : centerLabel;
						string label = jammer.part.partInfo.title;
						if(GUI.Button(new Rect(leftIndent, +(moduleLines*entryHeight), contentWidth, entryHeight), label, moduleStyle))
						{
							jammer.Toggle();
						}
						moduleLines++;
					}

					//GPS coordinator
					GUIStyle gpsModuleStyle = showGPSWindow ? centerLabelBlue : centerLabel;
					numberOfModules++;
					if(GUI.Button(new Rect(leftIndent, +(moduleLines*entryHeight), contentWidth, entryHeight), "GPS Coordinator", gpsModuleStyle))
					{
						showGPSWindow = !showGPSWindow;
					}
					moduleLines++;

					GUI.EndGroup();

					line += 0.1f;
				}
				modulesHeight = Mathf.Lerp(modulesHeight, moduleLines, 0.15f);
				line += modulesHeight;

				float gpsLines = 0;
				if(showGPSWindow && !toolMinimized)
				{
					line += 0.25f;
					GUI.BeginGroup(new Rect(5, contentTop + (line * entryHeight), toolWindowWidth, gpsWindowRect.height));
					GPSWindow();
					GUI.EndGroup();
					gpsLines = gpsWindowRect.height / entryHeight;
				}
				gpsHeight = Mathf.Lerp(gpsHeight, gpsLines, 0.15f);
				line += gpsHeight;
				
			}
			else
			{
				GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), "No Weapon Manager found.", HighLogic.Skin.box);
				line++;
			}
			
			
			
			
			toolWindowHeight = Mathf.Lerp(toolWindowHeight, contentTop + (line*entryHeight) + 5, 1);
			toolbarWindowRect.height = toolWindowHeight;// = new Rect(toolbarWindowRect.position.x, toolbarWindowRect.position.y, toolWindowWidth, toolWindowHeight);
		}

		bool validGPSName = true;

		//GPS window
		void GPSWindow()
		{
			GUI.Box(gpsWindowRect, GUIContent.none, HighLogic.Skin.box);
			gpsEntryCount = 0;
			Rect listRect = new Rect(gpsBorder, gpsBorder, gpsWindowRect.width - (2 * gpsBorder), gpsWindowRect.height - (2 * gpsBorder));
			GUI.BeginGroup(listRect);
			string targetLabel = "GPS Target: "+ActiveWeaponManager.designatedGPSInfo.name;
			GUI.Label(new Rect(0, 0, listRect.width, gpsEntryHeight), targetLabel, kspTitleLabel);
			gpsEntryCount+=0.85f;
			if(ActiveWeaponManager.designatedGPSCoords != Vector3d.zero)
			{
				GUI.Label(new Rect(0, gpsEntryCount * gpsEntryHeight, listRect.width - gpsEntryHeight, gpsEntryHeight), Misc.FormattedGeoPos(ActiveWeaponManager.designatedGPSCoords, true), HighLogic.Skin.box);
				if(GUI.Button(new Rect(listRect.width - gpsEntryHeight, gpsEntryCount * gpsEntryHeight, gpsEntryHeight, gpsEntryHeight), "X", HighLogic.Skin.button))
				{
					ActiveWeaponManager.designatedGPSInfo = new GPSTargetInfo();
				}
			}
			else
			{
				GUI.Label(new Rect(0, gpsEntryCount * gpsEntryHeight, listRect.width - gpsEntryHeight, gpsEntryHeight), "No Target", HighLogic.Skin.box);
			}
			gpsEntryCount+=1.35f;
			int indexToRemove = -1;
			int index = 0;
			BDATeams myTeam = BDATargetManager.BoolToTeam(ActiveWeaponManager.team);
			foreach(var coordinate in BDATargetManager.GPSTargets[myTeam])
			{
				Color origWColor = GUI.color;
				if(coordinate.EqualsTarget(ActiveWeaponManager.designatedGPSInfo))
				{
					GUI.color = XKCDColors.LightOrange;
				}
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
					if(GUI.Button(new Rect(0, gpsEntryCount * gpsEntryHeight, nameWidth, gpsEntryHeight), coordinate.name, HighLogic.Skin.button))
					{
						editingGPSName = true;
						editingGPSNameIndex = index;
						newGPSName = coordinate.name;
					}
				}
				if(GUI.Button(new Rect(nameWidth, gpsEntryCount * gpsEntryHeight, listRect.width - gpsEntryHeight - nameWidth, gpsEntryHeight), label, HighLogic.Skin.button))
				{
					ActiveWeaponManager.designatedGPSInfo = coordinate;
					editingGPSName = false;
				}
				if(GUI.Button(new Rect(listRect.width - gpsEntryHeight, gpsEntryCount * gpsEntryHeight, gpsEntryHeight, gpsEntryHeight), "X", HighLogic.Skin.button))
				{
					indexToRemove = index;
				}
				gpsEntryCount++;
				index++;
				GUI.color = origWColor;
			}
			if(hasEnteredGPSName && editingGPSNameIndex < BDATargetManager.GPSTargets[myTeam].Count)
			{
				hasEnteredGPSName = false;
				GPSTargetInfo old = BDATargetManager.GPSTargets[myTeam][editingGPSNameIndex];
				if(ActiveWeaponManager.designatedGPSInfo.EqualsTarget(old))
				{
					ActiveWeaponManager.designatedGPSInfo.name = newGPSName;
				}
				BDATargetManager.GPSTargets[myTeam][editingGPSNameIndex] = new GPSTargetInfo(BDATargetManager.GPSTargets[myTeam][editingGPSNameIndex].gpsCoordinates, newGPSName);
				editingGPSNameIndex = 0;
			}

			GUI.EndGroup();

			if(indexToRemove >= 0)
			{
				BDATargetManager.GPSTargets[myTeam].RemoveAt(indexToRemove);
			}

			//gpsWindowRect.x = toolbarWindowRect.x;
			//gpsWindowRect.y = toolbarWindowRect.y + toolbarWindowRect.height;
			gpsWindowRect.height = (2*gpsBorder) + (gpsEntryCount * gpsEntryHeight);
		}






		Rect SLineRect(float line)
		{
			return new Rect(settingsMargin, line * settingsLineHeight, settingsWidth - (2 * settingsMargin), settingsLineHeight);
		}

		Rect SRightRect(float line)
		{
			return new Rect(settingsMargin + ((settingsWidth - 2 * settingsLineHeight) / 2), line * settingsLineHeight, (settingsWidth - (2 * settingsMargin)) / 2, settingsLineHeight);
		}

		Rect SLeftRect(float line)
		{
			return new Rect(settingsMargin, (line * settingsLineHeight), (settingsWidth - (2*settingsMargin))/2, settingsLineHeight);
		}

		float settingsWidth;
		float settingsHeight;
		float settingsLeft;
		float settingsTop;
		float settingsLineHeight;
		float settingsMargin;
		Rect settingsRect;
		bool editKeys = false;
		void SetupSettingsSize()
		{
			settingsWidth = 420;
			settingsHeight = 480;
			settingsLeft = Screen.width/2 - settingsWidth/2;
			settingsTop = 100;
			settingsLineHeight = 22;
			settingsMargin = 18;
			settingsRect = new Rect(settingsLeft, settingsTop, settingsWidth, settingsHeight);
		}

		void SettingsGUI(int windowID)
		{
			float line = 1.25f;
			GUI.Box(new Rect(0, 0, settingsWidth, settingsHeight), "BDArmory Settings");
			GUI.DragWindow(new Rect(0,0,settingsWidth, 25));
			if(editKeys)
			{
				InputSettings();
				return;
			}
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

			bool origPm = PEACE_MODE;
			PEACE_MODE = GUI.Toggle(SLeftRect(line), PEACE_MODE, "Peace Mode");
			if(PEACE_MODE && !origPm)
			{
				BDATargetManager.ClearDatabase();
				if(OnPeaceEnabled != null)
				{
					OnPeaceEnabled();
				}
			}
			line++;
			line++;


			GUI.Label(SLeftRect(line), "Trigger Hold: "+TRIGGER_HOLD_TIME.ToString("0.00")+"s", leftLabel);
			TRIGGER_HOLD_TIME = GUI.HorizontalSlider(SRightRect(line),TRIGGER_HOLD_TIME, 0.02f, 1f);
			line++;


			GUI.Label(SLeftRect(line), "UI Volume: "+(BDARMORY_UI_VOLUME*100).ToString("0"), leftLabel);
			float uiVol = BDARMORY_UI_VOLUME;
			uiVol = GUI.HorizontalSlider(SRightRect(line),uiVol, 0f, 1f);
			if(uiVol != BDARMORY_UI_VOLUME && OnVolumeChange != null)
			{
				OnVolumeChange();
			}
			BDARMORY_UI_VOLUME = uiVol;
			line++;

			GUI.Label(SLeftRect(line), "Weapon Volume: "+(BDARMORY_WEAPONS_VOLUME*100).ToString("0"), leftLabel);
			float weaponVol = BDARMORY_WEAPONS_VOLUME;
			weaponVol = GUI.HorizontalSlider(SRightRect(line),weaponVol, 0f, 1f);
			if(uiVol != BDARMORY_WEAPONS_VOLUME && OnVolumeChange != null)
			{
				OnVolumeChange();
			}
			BDARMORY_WEAPONS_VOLUME = weaponVol;
			line++;
			line++;

			physicsRangeGui = GUI.TextField(SRightRect(line), physicsRangeGui);
			GUI.Label(SLeftRect(line), "Physics Load Distance", leftLabel);
			line++;
			GUI.Label(SLeftRect(line), "Warning: Risky if set high", centerLabel);
			if(GUI.Button(SRightRect(line), "Apply Phys Distance"))
			{
				float physRangeSetting = float.Parse(physicsRangeGui);
				PHYSICS_RANGE = (physRangeSetting>=2500 ? Mathf.Clamp(physRangeSetting, 2500, 100000) : 0);
				physicsRangeGui = PHYSICS_RANGE.ToString();
				ApplyPhysRange();
			}
			
			line++;
			line++;
			if(GUI.Button(SLineRect(line), "Edit Inputs"))
			{
				editKeys = true;
			}
			line++;
			line++;
			if(!BDKeyBinder.current && GUI.Button(SLineRect(line), "Save and Close"))
			{
				SaveConfig();
				settingsGuiEnabled = false;
			}

			line+=1.5f;
			settingsHeight = (line * settingsLineHeight);
			settingsRect.height = settingsHeight;
			BDGUIUtils.UseMouseEventInRect(settingsRect);
		}

		void InputSettings()
		{
			float line = 1.25f;
			int inputID = 0;


			GUI.Label(SLineRect(line), "- Weapons -", centerLabel);
			line++;
			InputSettingsList("WEAP_", ref inputID, ref line);
			line++;

			GUI.Label(SLineRect(line), "- Targeting Pod -", centerLabel);
			line++;
			InputSettingsList("TGP_", ref inputID, ref line);
			line++;

			GUI.Label(SLineRect(line), "- Radar -", centerLabel);
			line++;
			InputSettingsList("RADAR_", ref inputID, ref line);

			line += 2;
			if(!BDKeyBinder.current && GUI.Button(SLineRect(line), "Back"))
			{
				editKeys = false;
			}

			line+=1.5f;
			settingsHeight = (line * settingsLineHeight);
			settingsRect.height = settingsHeight;
			BDGUIUtils.UseMouseEventInRect(settingsRect);
		}

		void InputSettingsList(string prefix, ref int id, ref float line)
		{
			if(inputFields != null)
			{
				for(int i = 0; i < inputFields.Length; i++)
				{
					string fieldName = inputFields[i].Name;
					if(fieldName.StartsWith(prefix, StringComparison.Ordinal))
					{
						InputSettingsLine(fieldName, id++, ref line);
					}
				}
			}
		}

		void InputSettingsLine(string fieldName, int id, ref float line)
		{
			GUI.Box(SLineRect(line), GUIContent.none);
			string label = string.Empty;
			if(BDKeyBinder.IsRecordingID(id))
			{
				string recordedInput;
				if(BDKeyBinder.current.AcquireInputString(out recordedInput))
				{
					BDInputInfo orig = (BDInputInfo)typeof(BDInputSettingsFields).GetField(fieldName).GetValue(null);
					BDInputInfo recorded = new BDInputInfo(recordedInput, orig.description);
					typeof(BDInputSettingsFields).GetField(fieldName).SetValue(null, recorded);
				}

				label = "      Press a key or button.";
			}
			else
			{
				BDInputInfo inputInfo = new BDInputInfo();
				try
				{
					inputInfo = (BDInputInfo)typeof(BDInputSettingsFields).GetField(fieldName).GetValue(null);

				}
				catch(NullReferenceException)
				{
					Debug.Log("Reflection failed to find input info of field: " + fieldName);
					editKeys = false;
					return;
				}
				label = " "+inputInfo.description+" : "+inputInfo.inputString;

				if(GUI.Button(SSetKeyRect(line), "Set Key"))
				{
					BDKeyBinder.BindKey(id);
				}
				if(GUI.Button(SClearKeyRect(line), "Clear"))
				{
					typeof(BDInputSettingsFields).GetField(fieldName).SetValue(null, new BDInputInfo(inputInfo.description));
				}
			}
			GUI.Label(SLeftRect(line), label);
			line++;
		}

		Rect SSetKeyRect(float line)
		{
			return new Rect(settingsMargin + (2*(settingsWidth - 2 * settingsMargin) / 3), line * settingsLineHeight, (settingsWidth - (2 * settingsMargin)) / 6, settingsLineHeight);
		}

		Rect SClearKeyRect(float line)
		{
			return new Rect(settingsMargin + (2*(settingsWidth - 2 * settingsMargin) / 3) + (settingsWidth - 2 * settingsMargin) / 6, line * settingsLineHeight, (settingsWidth - (2 * settingsMargin)) / 6, settingsLineHeight);
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

