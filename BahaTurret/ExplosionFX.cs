using System;
using System.Collections.Generic;
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
		
		
	
		public static void CreateExplosion(Vector3 position, float radius, float power, Vessel sourceVessel, Vector3 direction, string explModelPath, string soundPath)
		{
			GameObject go;
			AudioClip soundClip;
				
			go = GameDatabase.Instance.GetModel(explModelPath);
			soundClip = GameDatabase.Instance.GetAudioClip(soundPath);
				
				
			Quaternion rotation = Quaternion.LookRotation(VectorUtils.GetUpDirection(position));
			GameObject newExplosion = (GameObject)GameObject.Instantiate(go, position, rotation);
			newExplosion.SetActive(true);
			ExplosionFX eFx = newExplosion.AddComponent<ExplosionFX>();
			eFx.exSound = soundClip;
			eFx.audioSource = newExplosion.AddComponent<AudioSource>();
			eFx.audioSource.minDistance = 20;
			eFx.audioSource.maxDistance = 1000;
				
			if(power <= 5)
			{
				eFx.audioSource.minDistance = 4f;
				eFx.audioSource.maxDistance = 1000;
				eFx.audioSource.priority = 9999;
			}
			foreach(KSPParticleEmitter pe in newExplosion.GetComponentsInChildren<KSPParticleEmitter>())
			{
				pe.emit = true;	
			}

			DoExplosionDamage(position, power, radius);
		}

		public static float ExplosionHeatMultiplier = 2800;
		public static float ExplosionImpulseMultiplier = 1;

		public static void DoExplosionRay(Ray ray, float power, float maxDistance, ref List<Part> ignoreParts, ref List<DestructibleBuilding> ignoreBldgs)
		{
			RaycastHit rayHit;
			if(Physics.Raycast(ray, out rayHit, maxDistance, 557057))
			{
				float sqrDist = (rayHit.point - ray.origin).sqrMagnitude;
				float sqrMaxDist = maxDistance * maxDistance;
				float distanceFactor = Mathf.Clamp01((sqrMaxDist - sqrDist) / sqrMaxDist);
				//parts
				Part part = rayHit.collider.GetComponentInParent<Part>();
				if(part && !ignoreParts.Contains(part) && part.physicalSignificance == Part.PhysicalSignificance.FULL)
				{
					ignoreParts.Add(part);
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
				if(building && !ignoreBldgs.Contains(building))
				{
					ignoreBldgs.Add(building);
					float damageToBuilding = ExplosionHeatMultiplier * 0.00685f * power * distanceFactor;
					if(damageToBuilding > building.impactMomentumThreshold/10) building.AddDamage(damageToBuilding);
					if(building.Damage > building.impactMomentumThreshold) building.Demolish();
					if(BDArmorySettings.DRAW_DEBUG_LABELS) Debug.Log("== Explosion hit destructible building! Damage: "+(damageToBuilding).ToString("0.00")+ ", total Damage: "+building.Damage);
				}
			}
		}

		public static List<Part> ignoreParts = new List<Part>(); 
		public static List<DestructibleBuilding> ignoreBuildings = new List<DestructibleBuilding>();

		public static void DoExplosionDamage(Vector3 position, float power, float maxDistance)
		{
			if(BDArmorySettings.DRAW_DEBUG_LABELS) Debug.Log("======= Doing explosion sphere =========");
			ignoreParts.Clear();
			ignoreBuildings.Clear();
			foreach(var vessel in BDATargetManager.LoadedVessels)
			{
				if(vessel == null) continue;
				if(vessel.loaded && !vessel.packed && (vessel.transform.position - position).magnitude < maxDistance * 4)
				{
					foreach(var part in vessel.parts)
					{
						if(!part) continue;
						DoExplosionRay(new Ray(position, part.transform.TransformPoint(part.CoMOffset) - position), power, maxDistance, ref ignoreParts, ref ignoreBuildings);
					}
				}
			}

			foreach(var bldg in BDATargetManager.LoadedBuildings)
			{
				if(bldg == null) continue;
				if((bldg.transform.position - position).magnitude < maxDistance * 1000)
				{
					DoExplosionRay(new Ray(position, bldg.transform.position - position), power, maxDistance, ref ignoreParts, ref ignoreBuildings);
				}
			}
		}
	}
}

