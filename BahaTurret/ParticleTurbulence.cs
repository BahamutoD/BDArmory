using System;
using UnityEngine;

namespace BahaTurret
{	
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class ParticleTurbulence : MonoBehaviour
	{
		public static Vector3 flareTurbulence = Vector3.zero;
		float flareTurbulenceX = 0;
		float flareTurbulenceY = 0;
		float flareTurbulenceZ = 0;
		float flareTurbDelta = 0.2f;
		float flareTurbTimer = 0;
		
		
		void FixedUpdate()
		{
			
			if(BDArmorySettings.Flares.Count > 0)
			{
				if(Time.time-flareTurbTimer > flareTurbDelta)
				{
					flareTurbTimer = Time.time;
					
					if(flareTurbulenceX >= 1) flareTurbulenceX = Mathf.Clamp(1 - UnityEngine.Random.Range(0f,2f), 0, 1);
					else if(flareTurbulenceX <= -1) flareTurbulenceX = Mathf.Clamp (-1 + UnityEngine.Random.Range(0f,2f), -1, 1);
					else flareTurbulenceX += Mathf.Clamp (UnityEngine.Random.Range(-1f,1f), -1, 1);
					
					if(flareTurbulenceY >= 1) flareTurbulenceY = Mathf.Clamp(1 - UnityEngine.Random.Range(0f,2f), 0, 1);
					else if(flareTurbulenceY <= -1) flareTurbulenceY = Mathf.Clamp (-1 + UnityEngine.Random.Range(0f,2f), -1, 1);
					else flareTurbulenceY += Mathf.Clamp (UnityEngine.Random.Range(-1f,1f), -1, 1);
					
					if(flareTurbulenceZ >= 1) flareTurbulenceZ = Mathf.Clamp(1 - UnityEngine.Random.Range(0f,2f), 0, 1);
					else if(flareTurbulenceZ <= -1) flareTurbulenceZ = Mathf.Clamp (-1 + UnityEngine.Random.Range(0f,2f), -1, 1);
					else flareTurbulenceZ += Mathf.Clamp (UnityEngine.Random.Range(-1f,1f), -1, 1);
				}
				
				flareTurbulence = Vector3.Lerp(flareTurbulence, new Vector3(flareTurbulenceX, flareTurbulenceY, flareTurbulenceZ), UnityEngine.Random.Range(0.05f,0.15f));
			}	
		}
	}
}

