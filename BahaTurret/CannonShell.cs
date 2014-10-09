using System;
using UnityEngine;

namespace BahaTurret
{
	public class CannonShell : MonoBehaviour
	{
		public float startTime;
		public float bulletLifeTime = 8;
		public Vessel sourceVessel;
		
		public Color projectileColor;
		
		
		public float radius = 30;
		public float blastPower = 8;
		
		public bool bulletDrop = true;
		
		public Vector3 prevPosition;
		public Vector3 currPosition;
		
		public bool instakill = false;
		
		private AudioSource audioSource;
		private GameObject explosion;
		
		LineRenderer bulletTrail;
		public float tracerStartWidth = 1;
		public float tracerEndWidth = 1;
		public float tracerLength = 0;
		
		void Start()
		{
			startTime = Time.time;
			prevPosition = gameObject.transform.position;
			
			Light light = gameObject.AddComponent<Light>();
			light.type = LightType.Point;
			light.range = 15;
			light.intensity = 8;
			
			audioSource = gameObject.AddComponent<AudioSource>();
			audioSource.minDistance = 0.1f;
			audioSource.maxDistance = 75;
			audioSource.clip = GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/shellWhistle");
			audioSource.volume = Mathf.Sqrt(GameSettings.SHIP_VOLUME);// * 0.85f;
			audioSource.dopplerLevel = 0.02f;
			
			explosion = GameDatabase.Instance.GetModel("BDArmory/Models/explosion/explosion");
			explosion.SetActive(true);
			
			bulletTrail = gameObject.AddComponent<LineRenderer>();
			bulletTrail.SetVertexCount(2);
			bulletTrail.SetPosition(0, transform.position);
			bulletTrail.SetPosition(1, transform.position);
			bulletTrail.SetWidth(tracerStartWidth, tracerEndWidth);
			bulletTrail.material = new Material(Shader.Find("KSP/Particles/Additive"));
			bulletTrail.material.mainTexture = GameDatabase.Instance.GetTexture("BDArmory/Textures/bullet", false);
			bulletTrail.material.SetColor("_TintColor", projectileColor);
			
			
			rigidbody.useGravity = false;
			
		}
		
		void FixedUpdate()
		{
			
			if(bulletDrop && FlightGlobals.RefFrameIsRotating)
			{
				rigidbody.velocity += FlightGlobals.getGeeForceAtPosition(transform.position) * Time.fixedDeltaTime;
			}
			
			
			if(tracerLength == 0)
			{
				bulletTrail.SetPosition(0, transform.position+(rigidbody.velocity * Time.fixedDeltaTime)-(FlightGlobals.ActiveVessel.rigidbody.velocity*Time.fixedDeltaTime));
			}
			else
			{
				bulletTrail.SetPosition(0, transform.position + (rigidbody.velocity.normalized * tracerLength));	
			}
			
			bulletTrail.SetPosition(1, transform.position);
			
			if(!audioSource.isPlaying)
			{
				audioSource.Play();	
			}
			if(Time.time - startTime > bulletLifeTime)
			{
				GameObject.Destroy(gameObject);
			}
			currPosition = gameObject.transform.position;
			float dist = (currPosition-prevPosition).magnitude;
			Ray ray = new Ray(prevPosition, currPosition-prevPosition);
			RaycastHit hit;
			if(Physics.Raycast(ray, out hit, dist, 557057))
			{
				
				Part hitPart =  null;
				try{
					hitPart = Part.FromGO(hit.rigidbody.gameObject);
				}catch(NullReferenceException){}
				
				if(hitPart!=null)
				{
					float destroyChance = (rigidbody.mass/hitPart.crashTolerance) * (rigidbody.velocity-hit.rigidbody.velocity).magnitude * 8000;
					if(instakill)
					{
						destroyChance = 100;	
					}
					Debug.Log ("Hit! chance of destroy: "+destroyChance);
					if(UnityEngine.Random.Range (0f,100f)<destroyChance)
					{
						if(hitPart.vessel != sourceVessel) hitPart.explode();
					}
				}
				
				//hitting a Building
				DestructibleBuilding hitBuilding = null;
				try{
					hitBuilding = hit.collider.gameObject.GetComponentUpwards<DestructibleBuilding>();
				}
				catch(NullReferenceException){}
				if(hitBuilding!=null && hitBuilding.IsIntact)
				{
					float damageToBuilding = rigidbody.mass * rigidbody.velocity.sqrMagnitude * 0.018f;
					hitBuilding.AddDamage(damageToBuilding);
					if(hitBuilding.Damage > hitBuilding.impactMomentumThreshold) hitBuilding.Demolish();
					if(BDArmorySettings.DRAW_DEBUG_LINES) Debug.Log("CannonShell hit destructible building! Damage: "+(damageToBuilding).ToString("0.00")+ ", total Damage: "+hitBuilding.Damage);
				}
			
				ExplosionFX.CreateExplosion(hit.point, 1, radius, blastPower, sourceVessel);
				
				GameObject.Destroy(gameObject); //destroy bullet on collision
			}
			
			prevPosition = currPosition;
		}
		
	
	}
}

