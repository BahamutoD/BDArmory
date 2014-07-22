using System;
using UnityEngine;

namespace BahaTurret
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class BDArmorySettings : MonoBehaviour
	{
		public static string FIRE_KEY = "mouse 0";
		public static bool INSTAKILL = false;
		public static bool BULLET_HITS = true;
		public static float PHYSICS_RANGE = 0;
		public static bool EJECT_SHELLS = true;
		
		
		void Start()
		{
			LoadConfig();
			
		}
		
		void Update()
		{
			if(PHYSICS_RANGE > 0)
			{
				Vessel.unloadDistance = PHYSICS_RANGE;
				Vessel.loadDistance = PHYSICS_RANGE+500;
				
				
				foreach(Vessel v in FlightGlobals.Vessels)
				{
					v.distancePackThreshold = PHYSICS_RANGE;
					v.distanceUnpackThreshold = PHYSICS_RANGE+500;
					v.distanceLandedPackThreshold = PHYSICS_RANGE;
					v.distanceLandedUnpackThreshold = PHYSICS_RANGE+500;
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
				}
			}
			catch(NullReferenceException)
			{
				Debug.Log ("== BDArmory : Failed to load settings config==");	
			}
		}
		
		
		
		
	}
}

