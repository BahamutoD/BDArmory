using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BahaTurret
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
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
		public static float BDARMORY_VOLUME = 0.5f; //TODO
		//==================
		
		
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
		
		public MissileFire wpnMgr = null;
		public bool missileWarning = false;
		public float missileWarningTime = 0;
		
		
		
		//load range stuff
		VesselRanges combatVesselRanges = new VesselRanges();
		float physRangeTimer;
		
		bool drawCursor = false;
		Texture2D cursorTexture;	
	
		public static List<GameObject> Flares = new List<GameObject>();

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

		public enum BDATeams{A, B};



		void Start()
		{	
			Instance = this;
			toolbarWindowRect = new Rect(Screen.width-toolWindowWidth-4, 150, toolWindowWidth, toolWindowHeight);
			AddToolbarButton();
			
			physRangeTimer = Time.time;
			LoadConfig();
			
			cursorTexture = GameDatabase.Instance.GetTexture("BDArmory/Textures/aimer", false);
			
			GameEvents.onHideUI.Add(HideGameUI);
			GameEvents.onShowUI.Add(ShowGameUI);
			GameEvents.onVesselGoOffRails.Add(OnVesselGoOffRails);
			GameEvents.OnGameSettingsApplied.Add(SaveVolumeSettings);
			GameEvents.onVesselCreate.Add(ApplyNewVesselRanges);
			
			GAME_UI_ENABLED = true;
			
			ApplyPhysRange();
			SaveVolumeSettings();
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


		}
		
		void Update()
		{
			if(missileWarning && Time.time - missileWarningTime > 1.5f)
			{
				missileWarning = false;	
			}

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

			DrawAimerCursor();
			
			if(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
			{
				if(Input.GetKeyDown(KeyCode.B))
				{
					settingsGuiEnabled = !settingsGuiEnabled;
					if(settingsGuiEnabled) LoadConfig();
					physicsRangeGui = PHYSICS_RANGE.ToString();
				}
			}
			
			
			
			if(Input.GetKeyDown(KeyCode.KeypadMultiply))
			{
				toolbarGuiEnabled = !toolbarGuiEnabled;	
			}
			
			
		}
		
		void DrawAimerCursor()
		{
			Screen.showCursor = true;
			drawCursor = false;
			if(!MapView.MapIsEnabled && !Misc.CheckMouseIsOnGui() && !PauseMenu.isOpen)
			{
				foreach(BahaTurret bt in FlightGlobals.ActiveVessel.FindPartModulesImplementing<BahaTurret>())
				{
					if(bt.deployed && DRAW_AIMERS && bt.maxPitch > 1)
					{
						Screen.showCursor = false;
						drawCursor = true;
						return;
					}
				}
			}
		}
		
		void FixedUpdate()
		{
			if(FlightGlobals.ActiveVessel != null)
			{
				GetWeaponManager();
			}
		}
		
		void GetWeaponManager()
		{
			
			foreach(var mf in FlightGlobals.ActiveVessel.FindPartModulesImplementing<MissileFire>())	
			{
				wpnMgr = mf;
				return;
			}
			
			wpnMgr = null;
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

				
				
				
				cfg.Save ("GameData/BDArmory/settings.cfg");
				
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
				
				if(toolbarGuiEnabled)
				{
					toolbarWindowRect = GUI.Window(321, toolbarWindowRect, ToolbarGUI, "");
				}
			}
			
			if(DRAW_DEBUG_LINES)
			{
				/*
				GUI.Label(new Rect(200,200,600,600), "floating origin continuous: "+FloatingOrigin.fetch.continuous
					+"\n Forced center tf name:  "+(FloatingOrigin.fetch.forcedCenterTransform !=null? FloatingOrigin.fetch.forcedCenterTransform.name : "")
					+"\n Floating threshold: "+FloatingOrigin.fetch.threshold
					);
					*/
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
			
			if(wpnMgr!=null)
			{
				GUIStyle armedLabelStyle;
				string armedText = "System is ";
				if(wpnMgr.isArmed)
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
					wpnMgr.ToggleArm();
				}
				
				GUIStyle teamButtonStyle;
				string teamText = "Team: ";
				if(wpnMgr.team)
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
					wpnMgr.ToggleTeam();
				}
				line++;
				
				string selectionText = "Weapon: "+wpnMgr.selectedWeapon;
				GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), selectionText, centerLabel);
				line++;

				//if weapon can ripple, show option and slider.
				if(wpnMgr.canRipple)
				{
					string rippleText = wpnMgr.rippleFire ? "Ripple: ON - "+wpnMgr.rippleRPM.ToString("0")+" RPM" : "Ripple: OFF";
					wpnMgr.rippleFire = GUI.Toggle(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth/2, entryHeight), wpnMgr.rippleFire, rippleText, leftLabel);
					if(wpnMgr.rippleFire)
					{
						wpnMgr.rippleRPM = GUI.HorizontalSlider(new Rect(leftIndent+(contentWidth/2), contentTop+(line*entryHeight), contentWidth/2, entryHeight), wpnMgr.rippleRPM, 100, 1600);
					}
				}
				line++;
				
				showWeaponList = GUI.Toggle(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth/2, entryHeight), showWeaponList, " Show Weapon List");
				showGuardMenu = GUI.Toggle(new Rect(leftIndent+(contentWidth/2), contentTop+(line*entryHeight), contentWidth/2, entryHeight), showGuardMenu, " Show Guard Menu");
				line++;
				
				if(showWeaponList)
				{
					line += 0.25f;
					GUI.Box(new Rect(5,  contentTop+(line*entryHeight), toolWindowWidth-10, wpnMgr.weaponArray.Length * entryHeight),""); //darker box
					for(int i = 0; i < wpnMgr.weaponArray.Length; i++)
					{
						GUIStyle wpnListStyle;
						if(i == wpnMgr.weaponIndex) wpnListStyle = centerLabelOrange;
						else wpnListStyle = centerLabel;
						if(GUI.Button(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), wpnMgr.weaponArray[i], wpnListStyle))
						{
							wpnMgr.CycleWeapon(i);
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
					wpnMgr.guardMode =  GUI.Toggle(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), wpnMgr.guardMode, " Guard Mode");
					line++;
					
					GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), 85, entryHeight), "Scan Interval", leftLabel);
					wpnMgr.targetScanInterval = GUI.HorizontalSlider(new Rect(leftIndent+(90), contentTop+(line*entryHeight), contentWidth-90-38, entryHeight), wpnMgr.targetScanInterval, 1, 60);
					wpnMgr.targetScanInterval = Mathf.Round(wpnMgr.targetScanInterval);
					GUI.Label(new Rect(leftIndent+(contentWidth-35), contentTop+(line*entryHeight), 35, entryHeight), wpnMgr.targetScanInterval.ToString(), leftLabel);
					line++;
					
					GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), 85, entryHeight), "Field of View", leftLabel);
					float guardAngle = wpnMgr.guardAngle;
					guardAngle = GUI.HorizontalSlider(new Rect(leftIndent+90, contentTop+(line*entryHeight), contentWidth-90-38, entryHeight), guardAngle, 10, 360);
					guardAngle = guardAngle/10;
					guardAngle = Mathf.Round(guardAngle);
					wpnMgr.guardAngle = guardAngle * 10;
					GUI.Label(new Rect(leftIndent+(contentWidth-35), contentTop+(line*entryHeight), 35, entryHeight), wpnMgr.guardAngle.ToString(), leftLabel);
					line++;
					
					GUI.Label(new Rect(leftIndent, contentTop+(line*entryHeight), 85, entryHeight), "Guard Range", leftLabel);
					float guardRange = wpnMgr.guardRange;
					guardRange = GUI.HorizontalSlider(new Rect(leftIndent+90, contentTop+(line*entryHeight), contentWidth-90-38, entryHeight), guardRange, 100, Mathf.Clamp(PHYSICS_RANGE, 2500, 100000));
					guardRange = guardRange/100;
					guardRange = Mathf.Round(guardRange);
					wpnMgr.guardRange = guardRange * 100;
					GUI.Label(new Rect(leftIndent+(contentWidth-35), contentTop+(line*entryHeight), 35, entryHeight), wpnMgr.guardRange.ToString(), leftLabel);
					line++;
					
					string targetType = "Target Type: ";
					if(wpnMgr.targetMissiles)
					{
						targetType += "Missiles";	
					}
					else
					{
						targetType += "Vessels";	
					}
					
					if(GUI.Button(new Rect(leftIndent, contentTop+(line*entryHeight), contentWidth, entryHeight), targetType, leftLabel))
					{
						wpnMgr.ToggleTargetType();	
					}
					line++;
					
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
		
		void SettingsGUI()
		{
			float width = 360;
			float height = 450;
			float left = Screen.width/2 - width/2;
			float top = Screen.height/2 - height/2;
			float spacer = 24;
			float leftMargin = left+18;
			float line = 2;
			GUI.Box(new Rect(left, top, width, height), "");
			GUI.Box(new Rect(left, top, width, height), "BDArmory Settings");
			INSTAKILL = GUI.Toggle(new Rect(leftMargin, top + line*spacer, width-2*spacer, spacer), INSTAKILL, "Instakill");
			line++;
			BULLET_HITS = GUI.Toggle(new Rect(leftMargin, top + line*spacer, width-2*spacer, spacer), BULLET_HITS, "Bullet Hits");
			line++;
			EJECT_SHELLS = GUI.Toggle(new Rect(leftMargin, top + line*spacer, width-2*spacer, spacer), EJECT_SHELLS, "Eject Shells");
			line++;
			INFINITE_AMMO = GUI.Toggle(new Rect(leftMargin, top + line*spacer, width-2*spacer, spacer), INFINITE_AMMO, "Infinte Ammo");
			line++;
			AIM_ASSIST = GUI.Toggle(new Rect(leftMargin, top + line*spacer, width-2*spacer, spacer), AIM_ASSIST, "Aim Assist");
			line++;
			DRAW_AIMERS = GUI.Toggle(new Rect(leftMargin, top + line*spacer, width-2*spacer, spacer), DRAW_AIMERS, "Draw Aimers");
			line++;
			DRAW_DEBUG_LINES = GUI.Toggle(new Rect(leftMargin, top + line*spacer, width-2*spacer, spacer), DRAW_DEBUG_LINES, "Draw Debug Lines");
			line++;
			REMOTE_SHOOTING = GUI.Toggle(new Rect(leftMargin, top + line*spacer, width-2*spacer, spacer), REMOTE_SHOOTING, "Allow Remote Firing");
			line++;
			BOMB_CLEARANCE_CHECK = GUI.Toggle(new Rect(leftMargin, top + line*spacer, width-2*spacer, spacer), BOMB_CLEARANCE_CHECK, "Bomb Clearance Check");
			line++;
			//SMART_GUARDS = GUI.Toggle(new Rect(leftMargin, top + line*spacer, width-2*spacer, spacer), SMART_GUARDS, "Smart Guards");
			//line++;
			DRAW_DEBUG_LABELS = GUI.Toggle(new Rect(leftMargin, top + line*spacer, width-2*spacer, spacer), DRAW_DEBUG_LABELS, "Debug Labels");
			line++;

			//fireKeyGui = GUI.TextField(new Rect(Screen.width/2, top + line*spacer, width/2 - spacer, spacer), fireKeyGui);
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

				if(GUI.Button(new Rect(leftMargin + 200, top + line*spacer, 100-(leftMargin-left), spacer), "Set Key"))
				{
					recordMouseUp = false;
					isRecordingInput = true;
				}
			}
			GUI.Label(new Rect(leftMargin, top + line*spacer, width-2*spacer, spacer), gunFireKeyLabel);
			line++;

			GUI.Label(new Rect(leftMargin, top + line*spacer, (width-2*spacer)/2, spacer), "Trigger Hold: "+TRIGGER_HOLD_TIME.ToString("0.00")+"s");
			TRIGGER_HOLD_TIME = GUI.HorizontalSlider(new Rect(leftMargin+((width-2*spacer)/2), top + line*spacer, (width-2*spacer)/2, spacer),TRIGGER_HOLD_TIME, 0.02f, 1f);
			line++;


			physicsRangeGui = GUI.TextField(new Rect(Screen.width/2, top + line*spacer, width/2 - spacer, spacer), physicsRangeGui);
			GUI.Label(new Rect(leftMargin, top + line*spacer, width-2*spacer, spacer), "Physics Load Distance");
			line++;
			GUI.Label(new Rect(Screen.width/2, top + line*spacer, width/2 - spacer, 2*spacer), "Warning: Risky if set high");
			if(GUI.Button(new Rect(leftMargin, top + line*spacer, width/2 - 2*spacer+8, spacer), "Apply Phys Distance"))
			{
				float physRangeSetting = float.Parse(physicsRangeGui);
				PHYSICS_RANGE = (physRangeSetting>=2500 ? Mathf.Clamp(physRangeSetting, 2500, 100000) : 0);
				physicsRangeGui = PHYSICS_RANGE.ToString();
				ApplyPhysRange();
			}
			
			line++;
			
			if(GUI.Button(new Rect(leftMargin, top + line*spacer +26, width/2 - 2*spacer+8, spacer), "Save and Close"))
			{
				SaveConfig();
				settingsGuiEnabled = false;
			}
		}
		
		
		#endregion
		
		public void ApplyPhysRange()
		{
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
		
		void AddToolbarButton()
		{
			if(!hasAddedButton)
			{
				Texture buttonTexture = GameDatabase.Instance.GetTexture("BDArmory/Textures/icon", false);
				ApplicationLauncher.Instance.AddModApplication(ShowToolbarGUI, HideToolbarGUI, Dummy, Dummy, Dummy, Dummy, ApplicationLauncher.AppScenes.FLIGHT, buttonTexture);
				hasAddedButton = true;
			}
		}
		
		public void ShowToolbarGUI()
		{
			toolbarGuiEnabled = true;	
		}
		
		public void HideToolbarGUI()
		{
			toolbarGuiEnabled = false;	
		}
		
		void Dummy()
		{}
		
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

