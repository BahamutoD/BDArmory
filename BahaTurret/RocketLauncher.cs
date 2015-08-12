using System;
using UnityEngine;
using System.Collections.Generic;

namespace BahaTurret
{
	public class RocketLauncher : PartModule, IBDWeapon
	{
		public bool hasRocket = true;

		[KSPField]
		public string shortName = string.Empty;
		
		[KSPField(isPersistant = false)]
		public string rocketType;
		
		[KSPField(isPersistant = false)]
		public string rocketModelPath;
		
		
		
		[KSPField(isPersistant = false)]
		public float rocketMass;
		
		[KSPField(isPersistant = false)]
		public float thrust;
		
		[KSPField(isPersistant = false)]
		public float thrustTime;
		
		[KSPField(isPersistant = false)]
		public float blastRadius;
		
		[KSPField(isPersistant = false)]
		public float blastForce;
		
		[KSPField(isPersistant = false)]
		public bool descendingOrder = true;
		
		[KSPField(isPersistant = false)]
		public string explModelPath = "BDArmory/Models/explosion/explosion";
		
		
		[KSPField(isPersistant = false)]
		public string explSoundPath = "BDArmory/Sounds/explode1";

		public bool drawAimer = false;
		
		Vector3 rocketPrediction = Vector3.zero;
		Texture2D aimerTexture;
		
		Transform[] rockets;
		
		AudioSource sfAudioSource;
		
		double lastRocketsLeft = 0;
		
		//weapon interface
		public Part GetPart()
		{
			return part;
		}
		public string GetShortName()
		{
			return shortName;
		}
		public WeaponClasses GetWeaponClass()
		{
			return WeaponClasses.Rocket;
		}

		[KSPAction("Fire")]
		public void AGFire(KSPActionParam param)
		{
			FireRocket();	
		}
		
		[KSPEvent(guiActive = true, guiName = "Fire", active = true)]
		public void GuiFire()
		{
			FireRocket();	
		}

		[KSPEvent(guiActive = true, guiName = "Jettison", active = true, guiActiveEditor = false)]
		public void Jettison()
		{
			part.decouple();
			if(BDArmorySettings.Instance.ActiveWeaponManager!=null) BDArmorySettings.Instance.ActiveWeaponManager.UpdateList();
		}
		
		
		public override void OnStart (PartModule.StartState state)
		{
			if(HighLogic.LoadedSceneIsFlight)
			{
				part.force_activate();
				
				aimerTexture = BDArmorySettings.Instance.greenPointCircleTexture;// GameDatabase.Instance.GetTexture("BDArmory/Textures/grayCircle", false);
				
				sfAudioSource = gameObject.AddComponent<AudioSource>();
				sfAudioSource.minDistance = 1;
				sfAudioSource.maxDistance = 2000;
				sfAudioSource.dopplerLevel = 0;
				sfAudioSource.priority = 230;
				
				MakeRocketArray();
				UpdateRocketScales();

				if (shortName == string.Empty)
				{
					shortName = part.partInfo.title;
				}

				UpdateAudio();
				BDArmorySettings.OnVolumeChange += UpdateAudio;
			}
		}

		void UpdateAudio()
		{
			if(sfAudioSource)
			{
				sfAudioSource.volume = BDArmorySettings.BDARMORY_WEAPONS_VOLUME;
			}
		}

		void OnDestroy()
		{
			BDArmorySettings.OnVolumeChange -= UpdateAudio;
		}
		
		public override void OnFixedUpdate ()
		{
			SimulateTrajectory();
			
			if(GetRocketResource().amount != lastRocketsLeft)
			{
				UpdateRocketScales();
				lastRocketsLeft = GetRocketResource().amount;
			}
			
		}
		
		public void FireRocket()
		{
			PartResource rocketResource = GetRocketResource();
			
			if(rocketResource == null)
			{
				Debug.Log (part.partName+" doesn't carry the rocket resource it was meant to");	
				return;
			}
			
			int rocketsLeft = (int) Math.Floor(rocketResource.amount);
			
			if(rocketsLeft >= 1)
			{
				Transform currentRocketTfm = rockets[rocketsLeft-1];
				
				GameObject rocketObj = GameDatabase.Instance.GetModel(rocketModelPath);
				rocketObj = (GameObject) Instantiate(rocketObj, currentRocketTfm.position+(part.rb.velocity*Time.fixedDeltaTime), transform.rotation);
				rocketObj.transform.rotation = part.transform.rotation;
				rocketObj.transform.localScale = part.rescaleFactor * Vector3.one;
				currentRocketTfm.localScale = Vector3.zero;
				Rocket rocket = rocketObj.AddComponent<Rocket>();
				rocket.explModelPath = explModelPath;
				rocket.explSoundPath = explSoundPath;
				rocket.spawnTransform = currentRocketTfm;
				rocket.mass = rocketMass;
				rocket.blastForce = blastForce;
				rocket.blastRadius = blastRadius;
				rocket.thrust = thrust;
				rocket.thrustTime = thrustTime;
				if(vessel.targetObject!=null) rocket.targetVessel = vessel.targetObject.GetVessel();
				rocket.sourceVessel = vessel;
				rocketObj.SetActive(true);
				rocketObj.transform.SetParent(transform);
				
				sfAudioSource.PlayOneShot(GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/launch"));
				rocketResource.amount--;
				
				lastRocketsLeft = rocketResource.amount;
			}
		}
		
		
		void SimulateTrajectory()
		{
			if(BDArmorySettings.AIM_ASSIST && BDArmorySettings.DRAW_AIMERS && drawAimer && vessel.isActiveVessel && vessel.altitude < 5000)
			{
				float simTime = 0;
				Transform fireTransform = part.transform;
				Vector3 pointingDirection = fireTransform.forward;
				Vector3 simVelocity = part.rb.velocity;
				Vector3 simCurrPos = fireTransform.position + (part.rb.velocity*Time.fixedDeltaTime);
				Vector3 simPrevPos = fireTransform.position + (part.rb.velocity*Time.fixedDeltaTime);
				Vector3 simStartPos = fireTransform.position + (part.rb.velocity*Time.fixedDeltaTime);
				bool simulating = true;
				float simDeltaTime = 0.02f;
				List<Vector3> pointPositions = new List<Vector3>();
				pointPositions.Add(simCurrPos);
				
				while(simulating)
				{
					float atmosMultiplier = Mathf.Clamp01 (2.5f*(float)FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(simCurrPos), FlightGlobals.getExternalTemperature(), FlightGlobals.currentMainBody));
					
					RaycastHit hit;
					simVelocity += FlightGlobals.getGeeForceAtPosition(simCurrPos) * simDeltaTime;
					if(simTime > 0.04f && simTime < thrustTime)
					{
						simDeltaTime = 0.2f;
						if(simTime < 0.5f) pointingDirection = Vector3.RotateTowards(pointingDirection, simVelocity, atmosMultiplier * (0.5f*(simTime)) * 50*simDeltaTime * Mathf.Deg2Rad, 0);
						else pointingDirection = Vector3.Lerp(pointingDirection, simVelocity.normalized, atmosMultiplier/2.5f);
						simVelocity += thrust/rocketMass * simDeltaTime * pointingDirection;
					}
					simCurrPos += simVelocity * simDeltaTime;
					pointPositions.Add(simCurrPos);
					if(simTime > 0.1f && Physics.Raycast(simPrevPos,simCurrPos-simPrevPos, out hit, Vector3.Distance(simPrevPos,simCurrPos), 557057))
					{
						rocketPrediction = hit.point;
						simulating = false;
						break;
					}
					else if(FlightGlobals.getAltitudeAtPos(simCurrPos)<0)
					{
						rocketPrediction = simCurrPos;
						simulating = false;
						break;
					}
					
					
					simPrevPos = simCurrPos;
					
					if((simStartPos-simCurrPos).sqrMagnitude>4000*4000)
					{
						rocketPrediction = simStartPos + (simCurrPos-simStartPos).normalized*2500;
						simulating = false;
					}
					simTime += simDeltaTime;
				}
				
				
				if(BDArmorySettings.DRAW_DEBUG_LINES && BDArmorySettings.DRAW_AIMERS)
				{
					Vector3[] pointsArray = pointPositions.ToArray();
					if(gameObject.GetComponent<LineRenderer>()==null)
					{
						LineRenderer lr = gameObject.AddComponent<LineRenderer>();
						lr.SetWidth(.1f, .1f);
						lr.SetVertexCount(pointsArray.Length);
						for(int i = 0; i<pointsArray.Length; i++)
						{
							lr.SetPosition(i, pointsArray[i]);	
						}
					}
					else
					{
						LineRenderer lr = gameObject.GetComponent<LineRenderer>();
						lr.enabled = true;
						lr.SetVertexCount(pointsArray.Length);
						for(int i = 0; i<pointsArray.Length; i++)
						{
							lr.SetPosition(i, pointsArray[i]);	
						}	
					}
				}
				else
				{
					if(gameObject.GetComponent<LineRenderer>()!=null)
					{
						gameObject.GetComponent<LineRenderer>().enabled = false;	
					}
				}
			}
			
			//for straight aimer
			else if(BDArmorySettings.DRAW_AIMERS && drawAimer && vessel.isActiveVessel)
			{
				RaycastHit hit;
				float distance = 2500;
				if(Physics.Raycast(transform.position,transform.forward, out hit, distance, 557057))
				{
					rocketPrediction = hit.point;
				}
				else
				{
					rocketPrediction = transform.position+(transform.forward*distance);	
				}
			}
				
		}
		
		void OnGUI()
		{
			if(drawAimer && vessel.isActiveVessel && BDArmorySettings.DRAW_AIMERS && !MapView.MapIsEnabled)
			{
				float size = 30;
				
				Vector3 aimPosition = FlightCamera.fetch.mainCamera.WorldToViewportPoint(rocketPrediction);
				
				Rect drawRect = new Rect(aimPosition.x*Screen.width-(0.5f*size), (1-aimPosition.y)*Screen.height-(0.5f*size), size, size);
				float cameraAngle = Vector3.Angle(FlightCamera.fetch.GetCameraTransform().forward, rocketPrediction-FlightCamera.fetch.mainCamera.transform.position);
				if(cameraAngle<90) GUI.DrawTexture(drawRect, aimerTexture);
				
			}
			
		}
		
		void MakeRocketArray()
		{
			Transform rocketsTransform = part.FindModelTransform("rockets");
			int numOfRockets = rocketsTransform.childCount;
			rockets = new Transform[numOfRockets];
				
			for(int i = 0; i < numOfRockets; i++)
			{
				string rocketName = rocketsTransform.GetChild(i).name;
				int rocketIndex = int.Parse(rocketName.Substring(7))-1;
				rockets[rocketIndex] = rocketsTransform.GetChild(i);
			}
			
			if(!descendingOrder) Array.Reverse(rockets);
		}
		
		
		public PartResource GetRocketResource()
		{
			foreach(var res in part.Resources.list)
			{
				if(res.resourceName == rocketType) return res;	
			}
			
			return null;
			
		}
		
		void UpdateRocketScales()
		{
			PartResource rocketResource = GetRocketResource();
			double rocketsLeft = Math.Floor(rocketResource.amount);
			double rocketsMax = rocketResource.maxAmount;
			for(int i = 0; i <rocketsMax; i++)
			{
				if (i<rocketsLeft) rockets[i].localScale = Vector3.one;
				else rockets[i].localScale = Vector3.zero;
			}
		}
		
	}
	
	
	
	public class Rocket : MonoBehaviour
	{
		public Transform spawnTransform;
		public Vessel targetVessel = null;
		public Vessel sourceVessel;
		public Vector3 startVelocity;
		public float mass;
		public float thrust;
		public float thrustTime;
		public float blastRadius;
		public float blastForce;
		public string explModelPath;
		public string explSoundPath;
		float startTime;
		AudioSource audioSource;
		
		Vector3 prevPosition;
		Vector3 currPosition;
		
		
		Vector3 relativePos;
		
		float stayTime = 0.04f;
		float lifeTime = 10;
		
		//bool isThrusting = true;

		Rigidbody rb;
		
		
		KSPParticleEmitter[] pEmitters;
		
		void Start()
		{
			BDArmorySettings.numberOfParticleEmitters++;
			
			rb = gameObject.AddComponent<Rigidbody>();
			pEmitters = gameObject.GetComponentsInChildren<KSPParticleEmitter>();
			
			foreach(var pe in pEmitters)
			{
				if(FlightGlobals.getStaticPressure(transform.position)==0 && pe.useWorldSpace) 
				{
					pe.emit = false;
				}
				else if(pe.useWorldSpace)
				{
					BDAGaplessParticleEmitter gpe = pe.gameObject.AddComponent<BDAGaplessParticleEmitter>();
					gpe.rb = rb;
					gpe.emit = true;
				}
			}
			
			prevPosition = transform.position;
			currPosition = transform.position;
			startTime = Time.time;
			
			rb.mass = mass;
			rb.isKinematic = true;
			//rigidbody.velocity = startVelocity;
			if(!FlightGlobals.RefFrameIsRotating) rb.useGravity = false;
			
			audioSource = gameObject.AddComponent<AudioSource>();
			audioSource.loop = true;
			audioSource.minDistance = 1;
			audioSource.maxDistance = 2000;
			audioSource.dopplerLevel = 0.5f;
			audioSource.volume = 0.9f * BDArmorySettings.BDARMORY_WEAPONS_VOLUME;
			audioSource.pitch = 1.4f;
			audioSource.clip = GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/rocketLoop");
			
			rb.useGravity = false;
		}
		
		void FixedUpdate()
		{
			//floatingOrigin fix
			if(sourceVessel!=null && (transform.position-sourceVessel.transform.position-relativePos).sqrMagnitude > 800*800)
			{
				transform.position = sourceVessel.transform.position+relativePos + (rb.velocity * Time.fixedDeltaTime);
			}
			if(sourceVessel!=null) relativePos = transform.position-sourceVessel.transform.position;
			//
			
			if(FlightGlobals.RefFrameIsRotating)
			{
				rb.velocity += FlightGlobals.getGeeForceAtPosition(transform.position) * Time.fixedDeltaTime;
			}
			

			if(BDArmorySettings.GameIsPaused)
			{
				if(audioSource.isPlaying)
				{
					audioSource.Stop();
				}
			}
			else
			{
				if(!audioSource.isPlaying)
				{
					audioSource.Play();	
				}
			}
			
			
			//guidance and attitude stabilisation scales to atmospheric density.
			float atmosMultiplier = Mathf.Clamp01 (2.5f*(float)FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(transform.position), FlightGlobals.getExternalTemperature(), FlightGlobals.currentMainBody));
			
			//model transform. always points prograde
			transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(rb.velocity, transform.up), atmosMultiplier * (0.5f*(Time.time-startTime)) * 50*Time.fixedDeltaTime);
			if(!FlightGlobals.RefFrameIsRotating && Time.time-startTime > 0.5f)
			{
				transform.rotation = Quaternion.Lerp (transform.rotation, Quaternion.LookRotation(rb.velocity), atmosMultiplier/2.5f);
				
			}
			//
			if(Time.time - startTime < stayTime && transform.parent!=null)
			{
				transform.rotation = transform.parent.rotation;		
				transform.position = spawnTransform.position;//+(transform.parent.rigidbody.velocity*Time.fixedDeltaTime);
			}
			
			if(Time.time - startTime < thrustTime && Time.time-startTime > stayTime)
			{
				float random = UnityEngine.Random.Range(-.2f,.2f);
				float random2 = UnityEngine.Random.Range(-.2f,.2f);
				rb.AddForce((thrust * transform.forward) + (random * transform.right) + (random2 * transform.up));
			}
			
			if(Time.time-startTime > stayTime && transform.parent!=null)
			{
				startVelocity = transform.parent.rigidbody.velocity;
				transform.parent = null;	
				rb.isKinematic = false;
				rb.velocity = startVelocity;
			}
			
			if(Time.time - startTime > thrustTime)
			{
				//isThrusting = false;
				foreach(var pEmitter in pEmitters)
				{
					if(pEmitter.useWorldSpace)
					{
						pEmitter.minSize = Mathf.MoveTowards(pEmitter.minSize, 0.1f, 0.05f);
						pEmitter.maxSize = Mathf.MoveTowards(pEmitter.maxSize, 0.2f, 0.05f);
					}
					else
					{
						pEmitter.minSize = Mathf.MoveTowards(pEmitter.minSize, 0, 0.1f);
						pEmitter.maxSize = Mathf.MoveTowards(pEmitter.maxSize, 0, 0.1f);
						if(pEmitter.maxSize == 0)
						{
							pEmitter.emit = false;	
						
						}
					}
					
				}
			}

			if(Time.time - startTime > 0.1f+stayTime)
			{
				//audioSource.pitch = SoundUtil.getDopplerPitchFactor(rigidbody.velocity, transform.position)*1.4f;
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
					
					
					if(hitPart==null || (hitPart!=null && hitPart.vessel!=sourceVessel))
					{
						Detonate(hit.point);
					}
				}
				else if(FlightGlobals.getAltitudeAtPos(transform.position)<0)
				{
					Detonate(transform.position);
				}
			}
			else if(FlightGlobals.getAltitudeAtPos(currPosition)<=0)
			{
				Detonate(currPosition);
			}
			prevPosition = currPosition;
			
			if(Time.time - startTime > lifeTime)
			{
				Detonate (transform.position);	
			}
			
			//proxy detonation
			if(targetVessel!=null && (transform.position-targetVessel.transform.position).sqrMagnitude < 0.5f*blastRadius*blastRadius)
			{
				Detonate(transform.position);	
			}
			
		}
		
		void Detonate(Vector3 pos)
		{
			BDArmorySettings.numberOfParticleEmitters--;
			
			ExplosionFX.CreateExplosion(pos, blastRadius, blastForce, sourceVessel, rb.velocity.normalized, explModelPath, explSoundPath);
			GameObject.Destroy(gameObject); //destroy rocket on collision
		}
		
		
		
		
		
		
		
	}
}

