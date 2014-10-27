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
		public static bool DRAW_AIMERS = true;
		public static bool AIM_ASSIST = true;
		public static bool REMOTE_SHOOTING = false;
		public static bool BOMB_CLEARANCE_CHECK = true;
		public static float DMG_MULTIPLIER = 6000;
		public static float FLARE_CHANCE_FACTOR = 25;
		//==================
		
		
		
		
		
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
		
		
		
		
		float physRangeTimer;
		
		bool drawCursor = false;
		Texture2D cursorTexture;	
	
		public static List<GameObject> Flares = new List<GameObject>();
		
		
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
			
			GAME_UI_ENABLED = true;
			
			ApplyPhysRange();
			
			fireKeyGui = FIRE_KEY;
			
		}
		
		void Update()
		{
			if(missileWarning && Time.time - missileWarningTime > 1.7f)
			{
				missileWarning = false;	
			}
			
			if(Time.time - physRangeTimer > 1)
			{
				ApplyPhysRange();
				physRangeTimer = Time.time;
			}
			
			if(Vessel.unloadDistance < PHYSICS_RANGE - 250 || Vessel.loadDistance < PHYSICS_RANGE)
			{
				ApplyPhysRange();
			}
			
			DrawAimerCursor();
			
			if(Input.GetKey(KeyCode.LeftAlt))
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
				
				cfg.SetValue("FireKey", FIRE_KEY);
				cfg.SetValue("INSTAKILL", INSTAKILL.ToString());
				cfg.SetValue("BULLET_HITS", BULLET_HITS.ToString());
				cfg.SetValue("PHYSICS_RANGE", PHYSICS_RANGE.ToString());
				cfg.SetValue("EJECT_SHELLS", EJECT_SHELLS.ToString());
				cfg.SetValue("INFINITE_AMMO", INFINITE_AMMO.ToString());
				cfg.SetValue("DRAW_DEBUG_LINES", DRAW_DEBUG_LINES.ToString());
				cfg.SetValue("DRAW_AIMERS", DRAW_AIMERS.ToString());
				cfg.SetValue("AIM_ASSIST", AIM_ASSIST.ToString());
				cfg.SetValue("REMOTE_SHOOTING", REMOTE_SHOOTING.ToString());
				cfg.SetValue("DMG_MULTIPLIER", DMG_MULTIPLIER.ToString());
				cfg.SetValue("FLARE_CHANCE_FACTOR", FLARE_CHANCE_FACTOR.ToString());
				cfg.SetValue("BOMB_CLEARANCE_CHECK", BOMB_CLEARANCE_CHECK.ToString());
				
				
				
				
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
		}
		
		void ToolbarGUI(int windowID)
		{
			GUI.DragWindow(new Rect(0,0,toolWindowWidth, 30));
			
			GUIStyle centerLabel = new GUIStyle();
			centerLabel.alignment = TextAnchor.UpperCenter;
			centerLabel.normal.textColor = Color.white;
			
			GUIStyle centerLabelRed = new GUIStyle();
			centerLabelRed.alignment = TextAnchor.UpperCenter;
			centerLabelRed.normal.textColor = Color.red;
			
			GUIStyle centerLabelOrange = new GUIStyle();
			centerLabelOrange.alignment = TextAnchor.UpperCenter;
			centerLabelOrange.normal.textColor = XKCDColors.BloodOrange;
			
			GUIStyle centerLabelBlue = new GUIStyle();
			centerLabelBlue.alignment = TextAnchor.UpperCenter;
			centerLabelBlue.normal.textColor = XKCDColors.AquaBlue;
			
			GUIStyle leftLabel = new GUIStyle();
			leftLabel.alignment = TextAnchor.UpperLeft;
			leftLabel.normal.textColor = Color.white;
			
			GUIStyle leftLabelRed = new GUIStyle();
			leftLabelRed.alignment = TextAnchor.UpperLeft;
			leftLabelRed.normal.textColor = Color.red;
			
			GUIStyle rightLabelRed = new GUIStyle();
			rightLabelRed.alignment = TextAnchor.UpperRight;
			rightLabelRed.normal.textColor = Color.red;
			
			GUIStyle leftLabelGray = new GUIStyle();
			leftLabelGray.alignment = TextAnchor.UpperLeft;
			leftLabelGray.normal.textColor = Color.gray;
			
			
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
					guardRange = GUI.HorizontalSlider(new Rect(leftIndent+90, contentTop+(line*entryHeight), contentWidth-90-38, entryHeight), guardRange, 100, BDArmorySettings.PHYSICS_RANGE);
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
			float height = 400;
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
			
			fireKeyGui = GUI.TextField(new Rect(Screen.width/2, top + line*spacer, width/2 - spacer, spacer), fireKeyGui);
			string gunFireKeyLabel = "Gun Fire Key";
			try
			{
				if(Input.GetKey(fireKeyGui))
				{
				}
				
			}
			catch(UnityException e)
			{
				if(e.Message.Contains("Input"))	
				{
					gunFireKeyLabel += " INVALID";
				}
			}
			if(!gunFireKeyLabel.Contains("INVALID"))
			{
				FIRE_KEY = fireKeyGui;	
			}
			GUI.Label(new Rect(leftMargin, top + line*spacer, width-2*spacer, spacer), gunFireKeyLabel);
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
			if(PHYSICS_RANGE < 2500 && PHYSICS_RANGE > 0) PHYSICS_RANGE = 0;
			
			
			if(PHYSICS_RANGE > 0)
			{
				Vessel.unloadDistance = PHYSICS_RANGE-250;
				Vessel.loadDistance = PHYSICS_RANGE;
				
				
				foreach(Vessel v in FlightGlobals.Vessels)
				{
					v.distancePackThreshold = PHYSICS_RANGE*1.5f;
					v.distanceUnpackThreshold = PHYSICS_RANGE*0.97f;
					v.distanceLandedPackThreshold = PHYSICS_RANGE * 0.8f;
					v.distanceLandedUnpackThreshold = PHYSICS_RANGE*0.75f;
				}
				
				FloatingOrigin.fetch.threshold = (PHYSICS_RANGE + 3500) * (PHYSICS_RANGE + 3500);
				
				
			}
			else
			{
				Vessel.unloadDistance = 2250;
				Vessel.loadDistance = 2500;
				
				
				foreach(Vessel v in FlightGlobals.Vessels)
				{
					v.distancePackThreshold = 5000;
					v.distanceUnpackThreshold = 200;
					v.distanceLandedPackThreshold = 350;
					v.distanceLandedUnpackThreshold = 200;
				}
				
				FloatingOrigin.fetch.threshold = 6000 * 6000;
					
			}
			
			
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
			if(v.Landed)
			{
				Debug.Log ("Loaded vessel: "+v.vesselName+", Velocity: "+v.srf_velocity);
				//v.SetWorldVelocity(Vector3d.zero);	
			}
			
		}
		
	}
}

