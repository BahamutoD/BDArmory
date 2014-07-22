using System;
using UnityEngine;

namespace BahaTurret
{
	public class CannonShell : MonoBehaviour
	{
		public float startTime;
		public float bulletLifeTime = 8;
		public Vessel sourceVessel;
		
		
		
		public float radius = 40;
		
		
		public Vector3 prevPosition;
		public Vector3 currPosition;
		
		public bool instakill = false;
		
		private AudioSource audioSource;
		private GameObject explosion;
		
		void Start()
		{
			startTime = Time.time;
			prevPosition = gameObject.transform.position;
			
			Light light = gameObject.AddComponent<Light>();
			light.type = LightType.Point;
			light.range = 15;
			light.intensity = 8;
			
			audioSource = gameObject.AddComponent<AudioSource>();
			audioSource.minDistance = 0.01f;
			audioSource.maxDistance = 75;
			audioSource.clip = GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/shellWhistle");
			audioSource.volume = Mathf.Sqrt(GameSettings.SHIP_VOLUME);// * 0.85f;
			audioSource.dopplerLevel = 0.02f;
			
			explosion = GameDatabase.Instance.GetModel("BDArmory/Models/explosion/explosion");
			explosion.SetActive(true);
			
		}
		
		void FixedUpdate()
		{
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
				
				Detonate (hit.point);
				
				//hit effects
				if(BDArmorySettings.BULLET_HITS)
				{
					//Part dummyPart = new Part();
					//FXMonger.Explode (dummyPart, hit.point, 50000f);
					GameObject newExplosion = (GameObject) GameObject.Instantiate(explosion, hit.point, Quaternion.identity);
					newExplosion.AddComponent<ExplosionFX>();
				}
				
				GameObject.Destroy(gameObject); //destroy bullet on collision
			}
			
			prevPosition = currPosition;
		}
		
		public void Detonate(Vector3 position)
		{
			//Debug.Log ("===========Missile detonating============");
			
			Collider[] colliders = Physics.OverlapSphere(transform.position, radius, 557057);
			foreach(Collider col in colliders)
			{
				Rigidbody rb = col.gameObject.GetComponentUpwards<Rigidbody>();
				if(rb!=null)
				{
					rb.AddExplosionForce(10, position, radius, 0, ForceMode.Impulse);
				}
			}
			
			                                 
		}
	
	}
}

