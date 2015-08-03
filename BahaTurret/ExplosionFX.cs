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
			try
			{
				GameObject go;
				AudioClip soundClip;
				
				go = GameDatabase.Instance.GetModel(explModelPath);
				soundClip = GameDatabase.Instance.GetAudioClip(soundPath);
				
				
				Quaternion rotation = Quaternion.LookRotation(FlightGlobals.getUpAxis());
				GameObject newExplosion = (GameObject)	GameObject.Instantiate(go, position, rotation);
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
				
				RaycastHit[] hits = Physics.SphereCastAll(position+(radius*Vector3.right), radius, (2*radius*Vector3.left), 2*radius, 557057);
				foreach(RaycastHit hitExplosion in hits)
				{
					//Debug.Log ("Explosion Raycast hit something: "+hitExplosion.collider.gameObject.name+", layer: "+hitExplosion.collider.gameObject.layer);

					//hitting parts
					Part explodePart = null;
					try
					{
						if(hitExplosion.rigidbody)
						{
							explodePart = Part.FromGO(hitExplosion.rigidbody.gameObject);
							if(!explodePart.vessel.loaded)
							{
								explodePart.vessel.Unload();
							}
							explodePart.Unpack();
						}
					}catch(NullReferenceException){}
					if(explodePart!=null && !explodePart.partInfo.name.Contains("Strut") && !explodePart.packed)
					{
						
						if(!MissileLauncher.CheckIfMissile(explodePart) || ((explodePart.GetComponent<MissileLauncher>().sourceVessel != sourceVessel || explodePart.GetComponent<MissileLauncher>().sourceVessel==null) && explodePart.GetComponent<MissileLauncher>().hasFired))
						{
							//Debug.Log ("Explosion hit part from vessel: "+explodePart.vessel.vesselName);
							
							
							RaycastHit expCheck;
							if(Physics.Raycast(position, explodePart.transform.position-position, out expCheck, radius, 557057) && expCheck.rigidbody.gameObject == hitExplosion.rigidbody.gameObject)
							{
								if(MissileLauncher.CheckIfMissile(explodePart) && explodePart.GetComponent<MissileLauncher>().hasFired && (expCheck.point-position).sqrMagnitude < (radius*radius)/2)
								{
									explodePart.temperature = explodePart.maxTemp + 500;	//immediate destroy intercepted missiles
								}
								else
								{
									float random = UnityEngine.Random.Range(0f,100f);
									float sqrDistance = (explodePart.transform.position-position).sqrMagnitude;
									float chance = (((radius*radius)-sqrDistance)/(radius*radius)) * (BDArmorySettings.DMG_MULTIPLIER/explodePart.crashTolerance) * 0.0064f * 100;
									//Debug.LogWarning("Hitting part: "+explodePart.partInfo.title+", explode chance: "+chance.ToString("0.0")+"%");
									if(random < chance)
									{
										explodePart.temperature = explodePart.maxTemp+500;
									}
									else
									{
										explodePart.rb.AddExplosionForce(power, position, radius, 0, ForceMode.Impulse);	
									}
								}
							}
							else
							{
								explodePart.rb.AddExplosionForce(power/20, position, radius, 0, ForceMode.Impulse);		
								
							}
							
						}
						
					}
					else
					{
					
						//hitting buildings
						DestructibleBuilding hitBuilding = null;
						try{
							hitBuilding = hitExplosion.collider.gameObject.GetComponentUpwards<DestructibleBuilding>();
						}
						catch(NullReferenceException){}
						if(hitBuilding!=null && hitBuilding.IsIntact)
						{
							float sqrDistance = (hitExplosion.point-position).sqrMagnitude;
							float damageToBuilding = (BDArmorySettings.DMG_MULTIPLIER/200) * power*(((radius*radius)-sqrDistance)/(radius*radius));
							if(damageToBuilding > hitBuilding.impactMomentumThreshold/10) hitBuilding.AddDamage(damageToBuilding);
							if(hitBuilding.Damage > hitBuilding.impactMomentumThreshold) hitBuilding.Demolish();
							if(BDArmorySettings.DRAW_DEBUG_LINES) Debug.Log("explosion hit destructible building! Damage: "+(damageToBuilding).ToString("0.00")+ ", total Damage: "+hitBuilding.Damage);
						}
					}
				}
				
			}
			catch(NullReferenceException e)
			{
				Debug.LogWarning("NRE in ExplosionFX.  Aborting. \n"+e);	
			}
		}
	}
}

