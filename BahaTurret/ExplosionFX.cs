using System;
using UnityEngine;

namespace BahaTurret
{
	public class ExplosionFX : MonoBehaviour
	{
		KSPParticleEmitter[] pEmitters;
		Light lightFX;
		float startTime;
		public AudioClip exSound;
		public AudioSource audioSource;
		float maxTime = 0;
		
		
		
		
		void Start()
		{
			startTime = Time.time;
			pEmitters = gameObject.GetComponentsInChildren<KSPParticleEmitter>();
			foreach(KSPParticleEmitter pe in pEmitters)
			{
				pe.emit = true;	
				if(!pe.useWorldSpace) pe.force = (4.49f * FlightGlobals.getGeeForceAtPosition(transform.position));
				if(pe.maxEnergy > maxTime)
				{
					maxTime = pe.maxEnergy;	
				}
			}
			lightFX = gameObject.AddComponent<Light>();
			lightFX.color = Misc.ParseColor255("255,238,184,255");
			lightFX.intensity = 8;
			lightFX.range = 50;
			
			
			
			
			audioSource.volume = BDArmorySettings.BDARMORY_WEAPONS_VOLUME;
			
			audioSource.PlayOneShot(exSound);
		}
		
		
		void FixedUpdate()
		{
			lightFX.intensity -= 12 * Time.fixedDeltaTime;
			if(Time.time-startTime > 0.2f)
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
		
		
		/* explosion sizes:
		 * 1: small, regular sound (like missiles and rockets)
		 * 2: large, regular sound (like mk82 bomb)
		 * 3: small, pop sound (like cluster submunition)
		 */
		public static void CreateExplosion(Vector3 position, float radius, float power, Vessel sourceVessel, Vector3 direction, string explModelPath, string soundPath)
		{
			GameObject go;
			AudioClip soundClip;
				
			go = GameDatabase.Instance.GetModel(explModelPath);
			soundClip = GameDatabase.Instance.GetAudioClip(soundPath);
				
				
			Quaternion rotation = Quaternion.LookRotation(FlightGlobals.getUpAxis());
			GameObject newExplosion = (GameObject)GameObject.Instantiate(go, position, rotation);
			newExplosion.SetActive(true);
			newExplosion.AddComponent<ExplosionFX>();
			newExplosion.GetComponent<ExplosionFX>().exSound = soundClip;
			newExplosion.GetComponent<ExplosionFX>().audioSource = newExplosion.AddComponent<AudioSource>();
			newExplosion.GetComponent<ExplosionFX>().audioSource.minDistance = 20;
			newExplosion.GetComponent<ExplosionFX>().audioSource.maxDistance = 1000;
				
			if(power <= 5)
			{
				newExplosion.GetComponent<ExplosionFX>().audioSource.minDistance = 4f;
				newExplosion.GetComponent<ExplosionFX>().audioSource.maxDistance = 1000;
				newExplosion.GetComponent<ExplosionFX>().audioSource.priority = 9999;
			}
			foreach(KSPParticleEmitter pe in newExplosion.GetComponentsInChildren<KSPParticleEmitter>())
			{
				pe.emit = true;	
			}

			DoExplosionSphere(position, power, radius);
		}

		public static float ExplosionHeatMultiplier = 1200;
		public static float ExplosionImpulseMultiplier = 2;

		public static void DoExplosionRay(Ray ray, float power, float maxDistance)
		{
			RaycastHit rayHit;
			if(Physics.Raycast(ray, out rayHit, maxDistance, 557057))
			{
				float sqrDist = (rayHit.point - ray.origin).sqrMagnitude;
				float distanceFactor = Mathf.Clamp01((Mathf.Pow(maxDistance, 2) - sqrDist) / Mathf.Pow(maxDistance, 2));
				//parts
				Part part = rayHit.collider.GetComponentInParent<Part>();
				if(part && part.physicalSignificance == Part.PhysicalSignificance.FULL)
				{
					Rigidbody rb = part.GetComponent<Rigidbody>();
					if(rb)
					{
						rb.AddForceAtPosition(ray.direction * power* distanceFactor * ExplosionImpulseMultiplier, rayHit.point, ForceMode.Impulse);
					}

					float heatDamage = ExplosionHeatMultiplier * power * distanceFactor/part.crashTolerance;
					part.temperature += heatDamage;
					if(BDArmorySettings.DRAW_DEBUG_LABELS) Debug.Log("====== Explosion ray hit part! Damage: " + heatDamage);
					return;
				}

				//buildings
				DestructibleBuilding building = rayHit.collider.GetComponentInParent<DestructibleBuilding>();
				if(building)
				{
					float damageToBuilding = (ExplosionHeatMultiplier/500) * power * distanceFactor;
					if(damageToBuilding > building.impactMomentumThreshold/10) building.AddDamage(damageToBuilding);
					if(building.Damage > building.impactMomentumThreshold) building.Demolish();
					if(BDArmorySettings.DRAW_DEBUG_LABELS) Debug.Log("== Explosion hit destructible building! Damage: "+(damageToBuilding).ToString("0.00")+ ", total Damage: "+building.Damage);
				}
			}
		}

		public static void DoExplosionSphere(Vector3 position, float power, float maxDistance)
		{
			if(BDArmorySettings.DRAW_DEBUG_LABELS) Debug.Log("======= Doing explosion sphere =========");
			int rays = 500;

			for(int i = 0; i < rays; i++)
			{
				Ray ray = new Ray(position, UnityEngine.Random.onUnitSphere);
				DoExplosionRay(ray, power, maxDistance);
			}
		}
	}
}

