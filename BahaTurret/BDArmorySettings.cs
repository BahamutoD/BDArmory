using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BahaTurret
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class BDArmorySettings : MonoBehaviour
	{
		public bool settingsGuiEnabled = false;
		
		
		public static string FIRE_KEY = "mouse 0";
		public static bool INSTAKILL = false;
		public static bool BULLET_HITS = true;
		public static float PHYSICS_RANGE = 0;
		public static bool EJECT_SHELLS = true;
		public static bool INFINITE_AMMO = false;
		public static bool CAMERA_TOOLS = true;
		public static bool DRAW_DEBUG_LINES = false;
		public static bool DRAW_AIMERS = true;
		public static bool AIM_ASSIST = true;
		
		public string physicsRangeGui;
		
		float physRangeTimer;
		
		bool drawCursor = false;
		Texture2D cursorTexture;	
	
		public static List<GameObject> Flares = new List<GameObject>();
		
		void Start()
		{
			physRangeTimer = Time.time;
			LoadConfig();
			
			cursorTexture = GameDatabase.Instance.GetTexture("BDArmory/Textures/aimer", false);
			
		}
		
		void Update()
		{
			
			if(Time.time - physRangeTimer > 1)
			{
				ApplyPhysRange();
				physRangeTimer = Time.time;
			}
			
			
			if(Input.GetKey(KeyCode.LeftAlt))
			{
				if(Input.GetKeyDown(KeyCode.B))
				{
					settingsGuiEnabled = !settingsGuiEnabled;
					physicsRangeGui = PHYSICS_RANGE.ToString();
				}
			}
			
			Screen.showCursor = true;
			drawCursor = false;
			foreach(BahaTurret bt in FlightGlobals.ActiveVessel.FindPartModulesImplementing<BahaTurret>())
			{
				if(bt.deployed && DRAW_AIMERS && bt.maxPitch > 1)
				{
					Screen.showCursor = false;
					drawCursor = true;
				}
			}
			
			
		}
		
		public static void LoadConfig()
		{
			try
			{
				Debug.Log ("== BDArmory : Loading settings.cfg ==");
				foreach(ConfigNode cfg in GameDatabase.Instance.GetConfigNodes("BDArmorySettings"))
				{
					if(cfg.HasValue("FireKey"))
					{
						FIRE_KEY = cfg.GetValue("FireKey");	
					}
					if(cfg.HasValue("INSTAKILL"))
					{
						INSTAKILL = bool.Parse(cfg.GetValue("INSTAKILL"));	
					}
					if(cfg.HasValue("BULLET_HITS"))
					{
						BULLET_HITS = Boolean.Parse(cfg.GetValue("BULLET_HITS"));	
					}
					if(cfg.HasValue("PHYSICS_RANGE"))
					{
						PHYSICS_RANGE = float.Parse(cfg.GetValue("PHYSICS_RANGE"));	
					}
					if(cfg.HasValue("EJECT_SHELLS"))
					{
						EJECT_SHELLS = bool.Parse(cfg.GetValue("EJECT_SHELLS"));
					}
					if(cfg.HasValue("INFINITE_AMMO"))
					{
						INFINITE_AMMO = Boolean.Parse(cfg.GetValue("INFINITE_AMMO"));
					}
					/*
					if(cfg.HasValue("CAMERA_TOOLS"))
					{
						CAMERA_TOOLS = Boolean.Parse(cfg.GetValue("CAMERA_TOOLS"));
					}
					*/
				}
			}
			catch(NullReferenceException)
			{
				Debug.Log ("== BDArmory : Failed to load settings config==");	
			}
		}
		
		public static void SaveConfig() //doesn't write to disk
		{
			try
			{
				Debug.Log("== BDArmory : Saving settings.cfg ==	");
				foreach(ConfigNode cfg in GameDatabase.Instance.GetConfigNodes("BDArmorySettings"))
				{
					cfg.SetValue("FireKey", FIRE_KEY);
					cfg.SetValue("INSTAKILL", INSTAKILL.ToString());
					cfg.SetValue("BULLET_HITS", BULLET_HITS.ToString());
					cfg.SetValue("PHYSICS_RANGE", PHYSICS_RANGE.ToString());
					cfg.SetValue("EJECT_SHELLS", EJECT_SHELLS.ToString());
					cfg.SetValue("INFINITE_AMMO", INFINITE_AMMO.ToString());
					//cfg.SetValue("CAMERA_TOOLS", CAMERA_TOOLS.ToString());
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
			DRAW_DEBUG_LINES = GUI.Toggle(new Rect(leftMargin, top + line*spacer, width-2*spacer, spacer), DRAW_DEBUG_LINES, "Draw Debug Lines");
			line++;
			
			FIRE_KEY = GUI.TextField(new Rect(Screen.width/2, top + line*spacer, width/2 - spacer, spacer), FIRE_KEY);
			GUI.Label(new Rect(leftMargin, top + line*spacer, width-2*spacer, spacer), "Gun Fire Key");
			line++;
			
			physicsRangeGui = GUI.TextField(new Rect(Screen.width/2, top + line*spacer, width/2 - spacer, spacer), physicsRangeGui);
			GUI.Label(new Rect(leftMargin, top + line*spacer, width-2*spacer, spacer), "Physics Load Distance");
			line++;
			GUI.Label(new Rect(Screen.width/2, top + line*spacer, width/2 - spacer, 2*spacer), "Warning: Risky! 0 is Default");
			if(GUI.Button(new Rect(leftMargin, top + line*spacer, width/2 - 2*spacer+8, spacer), "Apply Phys Distance"))
			{
				PHYSICS_RANGE = float.Parse(physicsRangeGui);
			}
			
			line++;
			
			if(GUI.Button(new Rect(leftMargin, top + line*spacer +26, width/2 - 2*spacer+8, spacer), "Save and Close"))
			{
				SaveConfig();
				settingsGuiEnabled = false;
			}
		}
		
		
		#endregion
		
		public static void ApplyPhysRange()
		{
			if(PHYSICS_RANGE > 0)
			{
				Vessel.unloadDistance = PHYSICS_RANGE-250;
				Vessel.loadDistance = PHYSICS_RANGE;
				
				
				foreach(Vessel v in FlightGlobals.Vessels)
				{
					
					v.distancePackThreshold = PHYSICS_RANGE;
					v.distanceUnpackThreshold = PHYSICS_RANGE-50;//-4800;
					v.distanceLandedPackThreshold = PHYSICS_RANGE-50;//-150;
					v.distanceLandedUnpackThreshold = PHYSICS_RANGE;
				}
				
				
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
				
					
			}
		}
		
		
	}
}

