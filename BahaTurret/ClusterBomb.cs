using System;
using System.Collections.Generic;
using UnityEngine;

namespace BahaTurret
{
	public class ClusterBomb : PartModule
	{
		
		List<GameObject> submunitions;
		List<GameObject> fairings;
		MissileLauncher missileLauncher;

		bool deployed = false;
		
		
		
		
		[KSPField(isPersistant = false)]
		public float deployDelay = 2.5f;
		
		
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Deploy Altitude"),
        	UI_FloatRange(minValue = 100f, maxValue = 1000f, stepIncrement = 10f, scene = UI_Scene.Editor)]
		public float deployAltitude = 400;
		
		[KSPField(isPersistant = false)]
		public float submunitionMaxSpeed = 10;
		
		
		[KSPField(isPersistant = false)]
		public bool swapCollidersOnDeploy = true;
		
		
		
		
		public override void OnStart (PartModule.StartState state)
		{
			submunitions = new List<GameObject>();
			foreach(var sub in part.FindModelTransforms("submunition"))
			{
				submunitions.Add(sub.gameObject);
				sub.gameObject.AddComponent<Rigidbody>();
				sub.rigidbody.isKinematic = true;
				sub.rigidbody.mass = part.mass / part.FindModelTransforms("submunition").Length;
			}
			
			fairings = new List<GameObject>();
			foreach(var fairing in part.FindModelTransforms("fairing"))
			{
				fairings.Add(fairing.gameObject);
				fairing.gameObject.AddComponent<Rigidbody>();
				fairing.rigidbody.isKinematic = true;
				fairing.rigidbody.mass = 0.05f;
			}
			
			missileLauncher = part.GetComponent<MissileLauncher>();
			//missileLauncher.deployTime = deployDelay;
		}
		
		public override void OnFixedUpdate ()
		{
			if(missileLauncher!=null && missileLauncher.timeIndex > deployDelay && !deployed && AltitudeTrigger())
			{
				DeploySubmunitions();
			}
		}
		
		void DeploySubmunitions()
		{
			missileLauncher.sfAudioSource.PlayOneShot(GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/flare"));
			FXMonger.Explode(part, transform.position+rigidbody.velocity*Time.fixedDeltaTime, 0.1f);
			
			deployed = true;
			if(swapCollidersOnDeploy)
			{
				foreach(var col in part.GetComponentsInChildren<Collider>())	
				{
					col.enabled = !col.enabled;	
				}
			}
			
			missileLauncher.sfAudioSource.priority = 999;
			missileLauncher.explosionSize = 3;
			
			foreach(var sub in submunitions)
			{
				sub.transform.parent = null;
				Vector3 direction = (sub.transform.position - part.transform.position).normalized;
				sub.rigidbody.isKinematic = false;
				sub.rigidbody.velocity = rigidbody.velocity + (UnityEngine.Random.Range(submunitionMaxSpeed/10, submunitionMaxSpeed) * direction);
				
				sub.AddComponent<Submunition>();
				sub.GetComponent<Submunition>().enabled = true;
				sub.GetComponent<Submunition>().deployed = true;
				sub.GetComponent<Submunition>().sourceVessel = missileLauncher.sourceVessel;
				sub.GetComponent<Submunition>().blastForce = missileLauncher.blastPower;
				sub.GetComponent<Submunition>().blastRadius = missileLauncher.blastRadius;
				sub.AddComponent<KSPForceApplier>();
			}
			
			foreach(var fairing in fairings)
			{
				Vector3 direction = (fairing.transform.position - part.transform.position).normalized;
				fairing.rigidbody.isKinematic = false;
				fairing.rigidbody.velocity = rigidbody.velocity + ((submunitionMaxSpeed+2) * direction);
				fairing.AddComponent<KSPForceApplier>();
				fairing.GetComponent<KSPForceApplier>().drag = 0.2f;
				fairing.AddComponent<ClusterBombFairing>();
				fairing.GetComponent<ClusterBombFairing>().deployed = true;
				fairing.GetComponent<ClusterBombFairing>().sourceVessel = vessel;
			}
			part.maximum_drag = 0.2f;
			part.minimum_drag = 0.2f;
			part.mass = part.mass/submunitions.Count;
		}
		
		
		bool AltitudeTrigger()
		{
			double asl = vessel.mainBody.GetAltitude(vessel.findWorldCenterOfMass());
			double radarAlt = asl - vessel.terrainAltitude;
			
			return (radarAlt < deployAltitude || asl < deployAltitude) && vessel.verticalSpeed < 0;
		}
		
	}
	
	
	public class Submunition : MonoBehaviour
	{
		public bool deployed = false;
		public float blastRadius;
		public float blastForce;
		public Vessel sourceVessel;
		Vector3 currPosition;
		Vector3 prevPosition;
		Vector3 relativePos;
		
		float startTime;
		
		void Start()
		{
			startTime = Time.time;
			relativePos = transform.position-sourceVessel.transform.position;
			currPosition = transform.position;
			prevPosition = transform.position;
		}
		
		void FixedUpdate()
		{
			if(deployed)
			{
				if(Time.time-startTime > 30)
				{
					Destroy(gameObject);
					return;
				}
				
				//floatingOrigin fix
				if(sourceVessel!=null && Vector3.Distance(transform.position-sourceVessel.transform.position, relativePos) > 800)
				{
					transform.position = sourceVessel.transform.position+relativePos + (rigidbody.velocity * Time.fixedDeltaTime);
				}
				if(sourceVessel!=null) relativePos = transform.position-sourceVessel.transform.position;
				//
				
				currPosition = transform.position;
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
						if(BDArmorySettings.INSTAKILL)
						{
							destroyChance = 100;	
						}
						Debug.Log ("Hit part: "+hitPart.name+", chance of destroy: "+destroyChance);
						if(UnityEngine.Random.Range (0f,100f)<destroyChance)
						{
							hitPart.temperature = hitPart.maxTemp + 100;
						}
					}
					if(hitPart==null || (hitPart!=null && hitPart.vessel!=sourceVessel))
					{
						Detonate(hit.point);
					}
				}
				else if(FlightGlobals.getAltitudeAtPos(currPosition)<=0)
				{
					Detonate(currPosition);
				}
				
			}
		}
		
		void Detonate(Vector3 pos)
		{
			ExplosionFX.CreateExplosion(pos, 3, blastRadius, blastForce, sourceVessel, FlightGlobals.getUpAxis());
			GameObject.Destroy(gameObject); //destroy bullet on collision
		}
		
		
	}
	
	public class ClusterBombFairing : MonoBehaviour
	{
		public bool deployed = false;
		
		public Vessel sourceVessel;
		Vector3 currPosition;
		Vector3 prevPosition;
		Vector3 relativePos;
		float startTime;
		
		void Start()
		{
			startTime = Time.time;
			currPosition = transform.position;
			prevPosition = transform.position;
			relativePos = transform.position-sourceVessel.transform.position;
		}
		
		void FixedUpdate()
		{
			if(deployed)
			{
				//floatingOrigin fix
				if(sourceVessel!=null && Vector3.Distance(transform.position-sourceVessel.transform.position, relativePos) > 800)
				{
					transform.position = sourceVessel.transform.position+relativePos + (rigidbody.velocity * Time.fixedDeltaTime);
				}
				if(sourceVessel!=null) relativePos = transform.position-sourceVessel.transform.position;
				//
				
				currPosition = transform.position;
				float dist = (currPosition-prevPosition).magnitude;
				Ray ray = new Ray(prevPosition, currPosition-prevPosition);
				RaycastHit hit;
				if(Physics.Raycast(ray, out hit, dist, 557057))
				{
					GameObject.Destroy(gameObject);
				}
				else if(FlightGlobals.getAltitudeAtPos(currPosition)<=0)
				{
					GameObject.Destroy(gameObject);
				}
				else if(Time.time - startTime > 20)
				{
					GameObject.Destroy(gameObject);
				}
			}
		}
		
	
		
	}
}

