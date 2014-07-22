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
		
		
		void Start()
		{
			startTime = Time.time;
			pEmitters = gameObject.GetComponentsInChildren<KSPParticleEmitter>();
			foreach(KSPParticleEmitter pe in pEmitters)
			{
				pe.emit = true;	
				pe.force = (4.49f * FlightGlobals.getGeeForceAtPosition(transform.position));
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
			if(Time.time-startTime > 0.03f)
			{
				foreach(KSPParticleEmitter pe in pEmitters)
				{
					pe.emit = false;	
				}
				
				
			}
			if(Time.time-startTime > 5.03f)
			{
				GameObject.Destroy(gameObject);	
			}
		}
		
		public static void CreateExplosion(Vector3 position)
		{
			GameObject go = GameDatabase.Instance.GetModel("BDArmory/Models/explosion/explosion");
			GameObject newExplosion = (GameObject)	GameObject.Instantiate(go, position, Quaternion.identity);
			newExplosion.SetActive(true);
			newExplosion.AddComponent<ExplosionFX>();
			/*
			foreach(KSPParticleEmitter pe in newExplosion.GetComponentsInChildren<KSPParticleEmitter>())
			{
				pe.emit = true;	
				pe.force = (4.49f * FlightGlobals.getGeeForceAtPosition(position));
			}
			*/
		}
	}
}

