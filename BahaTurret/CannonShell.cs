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
		
		
		public Vector3 prevPosition;
		public Vector3 currPosition;
		
		public bool instakill = false;
		
		private AudioSource audioSource;
		private GameObject explosion;
		
		LineRenderer bulletTrail;
		
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
			bulletTrail.SetWidth(0.8f, 0.01f);
			bulletTrail.material = new Material(Shader.Find("KSP/Particles/Additive"));
			bulletTrail.material.mainTexture = GameDatabase.Instance.GetTexture("BDArmory/Textures/bullet", false);
			bulletTrail.material.SetColor("_TintColor", projectileColor);
			
		}
		
		void FixedUpdate()
		{
			bulletTrail.SetPosition(0, transform.position+(rigidbody.velocity * Time.fixedDeltaTime)-(FlightGlobals.ActiveVessel.rigidbody.velocity*Time.fixedDeltaTime));
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
				
				ExplosionFX.CreateExplosion(hit.point, 1, radius, blastPower);
				
				GameObject.Destroy(gameObject); //destroy bullet on collision
			}
			
			prevPosition = currPosition;
		}
		
	
	}
}

