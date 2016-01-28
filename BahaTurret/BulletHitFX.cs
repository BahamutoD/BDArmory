using System;
using UnityEngine;

namespace BahaTurret
{
	public class BulletHitFX : MonoBehaviour
	{
		KSPParticleEmitter[] pEmitters;
		AudioSource audioSource;
		AudioClip hitSound;
		public Vector3 normal;
		float startTime;
		public bool ricochet;
		
		//static GameObject go = GameDatabase.Instance.GetModel("BDArmory/Models/bulletHit/bulletHit"); //===TODO: static object wont load after scene reload
		
		void Start()
		{
			startTime = Time.time;
			pEmitters = gameObject.GetComponentsInChildren<KSPParticleEmitter>();
			audioSource = gameObject.AddComponent<AudioSource>();
			audioSource.minDistance = 1;
			audioSource.maxDistance = 50;
			audioSource.volume = BDArmorySettings.BDARMORY_WEAPONS_VOLUME;
			
			int random = UnityEngine.Random.Range(1,3);
			
			if(ricochet)
			{
				string path = "BDArmory/Sounds/ricochet" + random;
				hitSound = GameDatabase.Instance.GetAudioClip(path);
			}
			else		
			{
				string path = "BDArmory/Sounds/bulletHit" + random;
				hitSound = GameDatabase.Instance.GetAudioClip(path);
			}
			
			
			
			audioSource.PlayOneShot(hitSound);
			
		}
		
		void Update()
		{
			if(Time.time - startTime > 0.03f)
			{
				foreach(KSPParticleEmitter pe in pEmitters)
				{
					pe.emit = false;	
				}
			}
			
			if(Time.time - startTime > 2f)
			{
				Destroy(gameObject);	
			}
		}	
		
		public static void CreateBulletHit(Vector3 position, Vector3 normalDirection, bool ricochet)
		{
			GameObject go = GameDatabase.Instance.GetModel("BDArmory/Models/bulletHit/bulletHit");
			GameObject newExplosion = (GameObject) GameObject.Instantiate(go, position, Quaternion.LookRotation(normalDirection));
			newExplosion.SetActive(true);
			newExplosion.AddComponent<BulletHitFX>();
			newExplosion.GetComponent<BulletHitFX>().ricochet = ricochet;
			foreach(KSPParticleEmitter pe in newExplosion.GetComponentsInChildren<KSPParticleEmitter>())
			{
				pe.emit = true;	
				if(pe.gameObject.name == "sparks")
				{
					pe.force = (4.49f * FlightGlobals.getGeeForceAtPosition(position));
				}
				else if(pe.gameObject.name == "smoke")
				{
					pe.force = (1.49f * FlightGlobals.getGeeForceAtPosition(position));
				}
			}
		}
		
	}
}

