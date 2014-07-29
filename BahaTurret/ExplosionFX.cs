using System;
using UnityEngine;

namespace BahaTurret
{
	public class ExplosionFX : MonoBehaviour
	{
		KSPParticleEmitter[] pEmitters;
		Light lightFX;
		float startTime;
		AudioClip exSound;
		AudioSource audioSource;
		float maxTime = 0;
		
		
		
		
		void Start()
		{
			startTime = Time.time;
			pEmitters = gameObject.GetComponentsInChildren<KSPParticleEmitter>();
			foreach(KSPParticleEmitter pe in pEmitters)
			{
				pe.emit = true;	
				pe.force = (4.49f * FlightGlobals.getGeeForceAtPosition(transform.position));
				if(pe.maxEnergy > maxTime)
				{
					maxTime = pe.maxEnergy;	
				}
			}
			lightFX = gameObject.AddComponent<Light>();
			lightFX.color = Misc.ParseColor255("255,238,184,255");
			lightFX.intensity = 8;
			lightFX.range = 50;
			
			exSound = GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/explode1");
			audioSource = gameObject.AddComponent<AudioSource>();
			audioSource.minDistance = 20;
			audioSource.maxDistance = 1000;
			audioSource.volume = Mathf.Sqrt(GameSettings.SHIP_VOLUME);
			audioSource.PlayOneShot(exSound);
		}
		
		void FixedUpdate()
		{
			lightFX.intensity -= 12 * Time.fixedDeltaTime;
			if(Time.time-startTime > 0.09f)
			{
				foreach(KSPParticleEmitter pe in pEmitters)
				{
					pe.emit = false;	
				}
				
				
			}
			if(Time.time-startTime > maxTime)
			{
				GameObject.Destroy(gameObject);	
			}
		}
		
		public static void CreateExplosion(Vector3 position, int size)
		{
			GameObject go;
			if(size == 2)
			{
				go = GameDatabase.Instance.GetModel("BDArmory/Models/explosion/explosionLarge");
			}
			else
			{
				go = GameDatabase.Instance.GetModel("BDArmory/Models/explosion/explosion");
			}
			GameObject newExplosion = (GameObject)	GameObject.Instantiate(go, position, Quaternion.identity);
			newExplosion.SetActive(true);
			newExplosion.AddComponent<ExplosionFX>();
			foreach(KSPParticleEmitter pe in newExplosion.GetComponentsInChildren<KSPParticleEmitter>())
			{
				pe.emit = true;	
				//pe.force = (4.49f * FlightGlobals.getGeeForceAtPosition(position));
			}
			
		}
	}
}

