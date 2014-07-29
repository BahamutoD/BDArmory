using System;
using UnityEngine;

namespace BahaTurret
{
	public class BahaTurretBullet : MonoBehaviour
	{
		public float startTime;
		public float bulletLifeTime = 3;
		public Vessel sourceVessel;
		public Color lightColor = Misc.ParseColor255("255, 235, 145, 255");
		
		
		public Vector3 prevPosition;
		public Vector3 currPosition;
		
		public bool instakill = false;
		
		
		
		void Start()
		{
			startTime = Time.time;
			prevPosition = gameObject.transform.position;
			
			Light light = gameObject.AddComponent<Light>();
			light.type = LightType.Point;
			light.color = lightColor;
			light.range = 8;
			light.intensity = 1;
		}
		
		void FixedUpdate()
		{
			if(Time.time - startTime > bulletLifeTime)
			{
				GameObject.Destroy(gameObject);
			}
			if(Time.time - startTime > 0.01f)
			{
				light.intensity = 0;	
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
				
				//hit effects
				if(BDArmorySettings.BULLET_HITS)
				{
					BulletHitFX.CreateBulletHit(hit.point, hit.normal);
				}
				
				GameObject.Destroy(gameObject); //destroy bullet on collision
			}
			
			prevPosition = currPosition;
		}
		
	}
}

