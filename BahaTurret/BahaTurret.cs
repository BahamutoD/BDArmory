using System;
using System.Collections.Generic;
using UnityEngine;

namespace BahaTurret
{
	public class BahaTurret : PartModule
	{
		public bool turretEnabled = false;  //user initiated
		
		public bool deployed = false;  //actual status
		
		public AnimationState[] deployStates;
		private AnimationState[] fireStates;
		private float fireAnimSpeed = 1;
		
		
		private Transform pitchTransform;
		private Transform yawTransform;
		private Vector3 yawAxis;
		private Vector3 pitchAxis;
		private float timeCheck = 0;
		
		
		//Gun specs
		[KSPField(isPersistant = false)]
		public float minPitch = -5;
		[KSPField(isPersistant = false)]
		public float maxPitch = 80;
		[KSPField(isPersistant = false)]
		public float yawRange = -1;
		[KSPField(isPersistant = false)]
		public float rotationSpeed = 2;
		[KSPField(isPersistant = false)]
		public float maxTargetingRange = 2000;
		[KSPField(isPersistant = false)]
		public float roundsPerMinute = 850;
		[KSPField(isPersistant = false)]
		public float accuracy = 4;
		[KSPField(isPersistant = false)]
		public float bulletMass = 5.40133e-5f;
		[KSPField(isPersistant = false)]
		public float bulletVelocity = 860;
		[KSPField(isPersistant = false)]
		public string ammoName = "50CalAmmo";
		[KSPField(isPersistant = false)]
		public bool onlyFireInRange = true;
		[KSPField(isPersistant = false)]
		public bool instakill = false;
		[KSPField(isPersistant = false)]
		public float requestResourceAmount = 1;
		[KSPField(isPersistant = false)]
		public bool bulletDrop = true;
		[KSPField(isPersistant = false)]
		public string weaponType = "ballistic";
		[KSPField(isPersistant = false)]
		public float laserDamage = 10000;
		[KSPField(isPersistant = false)]
		public bool autoLockCapable = false;
		
		
		
		
		[KSPField(isPersistant = false)]
		public float maxHeat = 3600;
		[KSPField(isPersistant = false)]
		public float heatPerShot = 75;
		[KSPField(isPersistant = false)]
		public float heatLoss = 250;
		
		public float heat = 0;
		public bool isOverheated = false;
		
		//
		
		[KSPField(isPersistant = false)]
		public string fireKey = "mouse 0"; //need to make this static with global settings config
		[KSPField(isPersistant = false)]
		public string fireSoundPath = "BDArmory/Parts/50CalTurret/sounds/shot";
		[KSPField(isPersistant = false)]
		public string overheatSoundPath = "BDArmory/Parts/50CalTurret/sounds/turretOverheat";
		[KSPField(isPersistant = false)]
		public string chargeSoundPath = "BDArmory/Parts/laserTest/sounds/charge";
		[KSPField(isPersistant = false)]
		public string fireAnimName = "fireAnim";
		[KSPField(isPersistant = false)]
		public bool hasFireAnimation = true;
		[KSPField(isPersistant = false)]
		public bool spinDownAnimation = false;
		private bool spinningDown = false;
		
		
		[KSPField(isPersistant = false)]
		public string yawTransformName = "aimRotate";
		[KSPField(isPersistant = false)]
		public string pitchTransformName = "aimPitch";
		
		
		AudioClip fireSound;
		AudioClip overheatSound;
		AudioClip chargeSound;
		AudioSource audioSource;
		AudioSource audioSource2;
		
		private bool turretZeroed = true;  //for knowing if the turret is ready to retract
		private bool inTurretRange = false; //for cutting off fire when target out of FOV
		private float targetDistance = 0;
		private Vector3 targetPosition;
		private Vessel targetVessel;
		private Part hitPart;
		private GameObject bullet;
		private int numberOfGuns = 0;
		
		private float muzzleFlashVelocity = 4;
		
		private VInfoBox heatGauge = null;
		
		
		
		[KSPAction("Toggle Turret")]
		public void AGToggle(KSPActionParam param)
		{
			toggle ();
		}
		
		[KSPEvent(guiActive = true, guiName = "Toggle Turret", active = true)]
		public void toggle()
		{
			turretEnabled = !turretEnabled;	
		}
		
		
		
		
		public override void OnStart (PartModule.StartState state)
		{
			deployStates = SetUpAnimation("deploy", this.part);
			
			if(hasFireAnimation)
			{
				fireStates = SetUpAnimation (fireAnimName, this.part);
				foreach(AnimationState anim in fireStates)
				{
					anim.enabled = false;	
				}
			}
			
			foreach(Transform mtf in part.FindModelTransforms("muzzleTransform"))
			{
				KSPParticleEmitter pEmitter = mtf.gameObject.GetComponent<KSPParticleEmitter>();
				muzzleFlashVelocity = pEmitter.worldVelocity.z;	
			}
			
			
			pitchTransform = part.FindModelTransform(pitchTransformName);
			yawTransform = part.FindModelTransform(yawTransformName);
			yawAxis = new Vector3(0,0,1);
			pitchAxis = new Vector3(0,-1,0);
			hitPart = null;
			
			fireSound = GameDatabase.Instance.GetAudioClip(fireSoundPath);
			overheatSound = GameDatabase.Instance.GetAudioClip(overheatSoundPath);
			audioSource = gameObject.AddComponent<AudioSource>();
			audioSource.bypassListenerEffects = true;
			audioSource.minDistance = 1;
			audioSource.maxDistance = 1000;
			
			audioSource2 = gameObject.AddComponent<AudioSource>();
			audioSource2.bypassListenerEffects = true;
			audioSource2.minDistance = 1;
			audioSource2.maxDistance = 1000;
			
			if(weaponType == "ballistic")
			{
				bullet = new GameObject("Bullet");
				bullet.SetActive(true);
				Rigidbody bulletRB = bullet.AddComponent<Rigidbody>();
				if(!bulletDrop) bulletRB.useGravity = false;
				bullet.rigidbody.mass = bulletMass;
				TrailRenderer bulletTrail = bullet.AddComponent<TrailRenderer>();
				bulletTrail.startWidth = 0.05f;
				bulletTrail.endWidth = 0.005f;
				bulletTrail.material = new Material(Shader.Find ("KSP/Emissive/Diffuse"));
				bulletTrail.material.SetColor("_EmissiveColor", XKCDColors.OrangeyYellow);
				
				bulletTrail.time = 0.02f;
			}
			
			if(weaponType == "laser")
			{
				chargeSound = GameDatabase.Instance.GetAudioClip(chargeSoundPath);
				audioSource.clip = fireSound;
				foreach(Transform tf in part.FindModelTransforms("fireTransform"))
				{
					LineRenderer lr = tf.gameObject.AddComponent<LineRenderer>();
					lr.material = new Material(Shader.Find ("KSP/Emissive/Diffuse"));
					lr.material.SetColor("_EmissiveColor", Color.red);
					lr.castShadows = false;
					lr.receiveShadows = false;
					lr.SetWidth(.1f, .1f);
					lr.SetVertexCount(2);
					lr.SetPosition(0, tf.position);
					lr.SetPosition(1, tf.position);
				}
				
				
			}
			
			
			part.force_activate();
			
		}
		
		public override void OnFixedUpdate()
		{
			//laser
			if(weaponType == "laser")
			{
				foreach(Transform tf in part.FindModelTransforms("fireTransform"))
				{
					LineRenderer lr = tf.gameObject.GetComponent<LineRenderer>();
					lr.SetPosition(0, tf.position);
					lr.SetPosition(1, tf.position);
				}
			}
			
			//heat
			if(heat > maxHeat/3)
			{
				if(heatGauge == null)
				{
					heatGauge = InitHeatGauge(part);
				}
				heatGauge.SetValue(heat, maxHeat/3, maxHeat);
			}
			else if(heatGauge != null && heat < maxHeat/4)
			{
				part.stackIcon.ClearInfoBoxes();
				heatGauge = null;
			}
			
			heat = Mathf.Clamp(heat - heatLoss * TimeWarp.fixedDeltaTime, 0, Mathf.Infinity);
			if(heat>maxHeat && !isOverheated)
			{
				isOverheated = true;
				audioSource2.PlayOneShot(overheatSound);
			}
			if(heat < maxHeat/3 && isOverheated) //reset on cooldown
			{
				isOverheated = false;
				heat = 0;
			}
			//
			
			
			
			//finding number of guns for fire volume reduction
			numberOfGuns = 0;
			foreach(BahaTurret bt in vessel.FindPartModulesImplementing<BahaTurret>())
			{
				if(bt.deployed)
				{
					numberOfGuns++;	
				}
			}	
			if(numberOfGuns<1)
			{
				numberOfGuns = 1;	
			}
			//
			
			
			//animation handling
			foreach(AnimationState anim in deployStates)
			{
				//animation clamping
				if(anim.normalizedTime>1)
				{
					anim.speed = 0;
					anim.normalizedTime = 1;
				}
				if(anim.normalizedTime < 0)
				{
					anim.speed = 0;
					anim.normalizedTime = 0;
				}
				
				//deploying
				if(turretEnabled)
				{
					if(anim.normalizedTime<1 && anim.speed<1)
					{
						anim.speed = 1;	
						deployed = false;
					}
					if(anim.normalizedTime == 1)
					{
						deployed = true;
						anim.enabled = false;
					}
				}
				
				//retracting
				if(!turretEnabled)
				{
					deployed = false;
					ReturnTurret();
				
					if(turretZeroed)
					{
						anim.enabled = true;
						if(anim.normalizedTime > 0 && anim.speed > -1)
						{
							anim.speed = -1;	
						}
					}
				}
			}
			
			//aim+shooting
			if(deployed && (TimeWarp.WarpMode!=TimeWarp.Modes.HIGH || TimeWarp.CurrentRate == 1))
			{
				if(vessel.isActiveVessel)
				{
					Aim ();	
					CheckTarget ();
					if(Input.GetKey(fireKey) && inTurretRange)
					{
						if(weaponType == "ballistic") Fire ();
						else if(weaponType == "laser")
						{
							if(!FireLaser ())
							{
								audioSource.Stop ();	
							}
						}
					}
					else
					{
						if(spinDownAnimation) spinningDown = true;
						if(weaponType == "laser") audioSource.Stop ();
						
					}
					
					if(spinningDown && spinDownAnimation)
					{
						foreach(AnimationState anim in fireStates)
						{
							if(anim.normalizedTime>1) anim.normalizedTime = 0;
							anim.speed = fireAnimSpeed;
							fireAnimSpeed = Mathf.Lerp(fireAnimSpeed, 0, 0.04f);
						}
					}
					
					
				}else
				{
					turretEnabled = false;
				}
			}
			else
			{
				if(weaponType == "laser")
				{
					audioSource.Stop ();	
				}	
			}
			
			
		}
		
		
		private void Aim()
		{
			inTurretRange = true;
			
			Vector3 target = Vector3.zero;
			Vector3 targetYawOffset;
			Vector3 targetPitchOffset;
			
			//auto target tracking
			if(autoLockCapable)
			{
				targetVessel = null;
				try
				{
					targetVessel = vessel.targetObject.GetVessel();
				}catch(NullReferenceException){}
				if(targetVessel!=null)
				{
					target = targetVessel.transform.position;
				}
			}
			//
			
			if (target == Vector3.zero)  //if no target from vessel Target, use mouse aim
			{
				Vector3 mouseAim = new Vector3(Input.mousePosition.x/Screen.width, Input.mousePosition.y/Screen.height, 0);
				Ray ray = FlightCamera.fetch.mainCamera.ViewportPointToRay(mouseAim);
				
				RaycastHit hit;
				
				
				if(Physics.Raycast(ray, out hit, maxTargetingRange, 557057))
				{
					target = hit.point;
					try{
						Part p = Part.FromGO(hit.rigidbody.gameObject);
						if(p.vessel == this.vessel)
						{
							target = ray.direction * maxTargetingRange + FlightCamera.fetch.mainCamera.transform.position;		
						}
					}catch(NullReferenceException){}
					
				}else
				{
					target = ray.direction * maxTargetingRange + FlightCamera.fetch.mainCamera.transform.position;	
				}
			}
			targetDistance = (target - part.transform.position).magnitude;
			
			targetYawOffset = yawTransform.position - target;
			targetYawOffset = Quaternion.Inverse(yawTransform.rotation) * targetYawOffset; //sets offset relative to the turret's rotation
			targetYawOffset = Quaternion.AngleAxis(90, yawAxis) * targetYawOffset; //fix difference in coordinate system.
			
			targetPitchOffset = pitchTransform.position - target;
			targetPitchOffset = Quaternion.Inverse(pitchTransform.rotation) * targetPitchOffset;
			targetPitchOffset = Quaternion.AngleAxis(180, pitchAxis) * targetPitchOffset;
			
			
			float rotationSpeedYaw = Mathf.Clamp (Mathf.Abs (10*targetYawOffset.x/Mathf.Clamp (targetDistance,10, maxTargetingRange)), 0, rotationSpeed);
			float rotationSpeedPitch = Mathf.Clamp (Mathf.Abs (10*targetPitchOffset.z/Mathf.Clamp (targetDistance,10, maxTargetingRange)), 0, rotationSpeed);
			
			targetPosition = target;
			
			if(TimeWarp.WarpMode!=TimeWarp.Modes.HIGH || TimeWarp.CurrentRate == 1)
			{
				//yaw movement
				if(yawRange >= 0)
				{
					float minYaw = -(yawRange/2);
					float maxYaw = yawRange/2;
					if(targetYawOffset.x > 0 && yawTransform.localRotation.Roll () < maxYaw*Mathf.Deg2Rad)
					{
						yawTransform.localRotation *= Quaternion.AngleAxis(rotationSpeedYaw, yawAxis);
					}
					else if(targetYawOffset.x < 0 && yawTransform.localRotation.Roll () > minYaw*Mathf.Deg2Rad)
					{
						yawTransform.localRotation *= Quaternion.AngleAxis(-rotationSpeedYaw, yawAxis);
					}
					else
					{
						if(onlyFireInRange)
						{
							inTurretRange = false;	//don't fire because turret can't reach target.
						}
					}
					
				}
				else{
					if(targetYawOffset.x > 0)
					{
						yawTransform.localRotation *= Quaternion.AngleAxis(rotationSpeedYaw, yawAxis);
					}
					if(targetYawOffset.x < 0)
					{
						yawTransform.localRotation *= Quaternion.AngleAxis(-rotationSpeedYaw, yawAxis);
					}
				}
			}
			
			//pitch movement
			if(targetPitchOffset.z > 0 && pitchTransform.localRotation.Yaw ()>-maxPitch*Mathf.Deg2Rad)
			{
				pitchTransform.localRotation *= Quaternion.AngleAxis(rotationSpeedPitch, pitchAxis);
			}
			else if(targetPitchOffset.z < 0 && pitchTransform.localRotation.Yaw () < - minPitch*Mathf.Deg2Rad)
			{
				pitchTransform.localRotation *= Quaternion.AngleAxis(-rotationSpeedPitch, pitchAxis);
			}
			else
			{
				if(onlyFireInRange)
				{
					inTurretRange = false;	//don't fire because turret can't reach target.
				}
			}
		}
		
		//returns turret to default position when turned off
		private void ReturnTurret()
		{
			float returnSpeed = Mathf.Clamp (rotationSpeed, 0.1f, 6f);
			bool yawReturned = false;
			bool pitchReturned = false;
			turretZeroed = false;
			float yaw = yawTransform.localRotation.Roll() * Mathf.Rad2Deg;
			float pitch = pitchTransform.localRotation.Yaw () * Mathf.Rad2Deg;
			
			//Debug.Log ("Pitch: "+pitch*Mathf.Rad2Deg+", Yaw: "+yaw*Mathf.Rad2Deg);
			
			if(yaw > 1 || yaw < -1)
			{
				if(yaw > 0)
				{
					yawTransform.localRotation *= Quaternion.AngleAxis(-Mathf.Clamp(Mathf.Abs (yaw)/2, 0.01f, returnSpeed), yawAxis);
				}
				if(yaw < 0)
				{
					yawTransform.localRotation *= Quaternion.AngleAxis(Mathf.Clamp(Mathf.Abs (yaw)/2, 0.01f, returnSpeed), yawAxis);
				}
			}
			else
			{
				yawReturned = true;
			}
			
			if(pitch > 1 || pitch < -1)
			{
				if(pitch > 0)
				{
					pitchTransform.localRotation *= Quaternion.AngleAxis(Mathf.Clamp(Mathf.Abs (pitch)/2, 0.01f, returnSpeed), pitchAxis);
				}
				if(pitch < 0)
				{
					pitchTransform.localRotation *= Quaternion.AngleAxis(-Mathf.Clamp(Mathf.Abs (pitch)/2, 0.01f, returnSpeed), pitchAxis);
				}
			}
			else
			{
				pitchReturned = true;
			}
			if(yawReturned && pitchReturned)
			{
				yawTransform.localRotation = Quaternion.Euler(0,0,0);
				pitchTransform.localRotation = Quaternion.Euler(0,0,0);
				turretZeroed = true;
			}
			
		}
		
		
		private void Fire()
		{
			
			float timeGap = (60/roundsPerMinute) * TimeWarp.CurrentRate;
			
			if(Time.time-timeCheck > timeGap && !isOverheated)
			{
				Transform[] fireTransforms = part.FindModelTransforms("fireTransform");
				foreach(Transform tf in fireTransforms)
				{
					if(part.RequestResource(ammoName, requestResourceAmount)>0)
					{
						spinningDown = false;
						
						GameObject firedBullet = GameObject.Instantiate(bullet, tf.position, tf.rotation) as GameObject;
						
						
						
						float randomY = UnityEngine.Random.Range(-20f, 20f)/accuracy;
						float randomZ = UnityEngine.Random.Range(-20f, 20f)/accuracy;
						
						//recoil
						gameObject.rigidbody.AddForce (pitchTransform.rotation*new Vector3(-bulletVelocity, -randomY, -randomZ).normalized*(bulletVelocity*bulletMass), ForceMode.Impulse);
						
						
						//sound
						audioSource.dopplerLevel = 0;
						audioSource.bypassListenerEffects = true;
						audioSource.volume = 1/Mathf.Sqrt(numberOfGuns);
						audioSource.PlayOneShot(fireSound);
						
						//animation
						if(hasFireAnimation)
						{
							foreach(AnimationState anim in fireStates)
							{
								fireAnimSpeed = (roundsPerMinute*anim.length)/60;
								anim.enabled = true;
								anim.normalizedTime = 0;
								anim.speed = fireAnimSpeed;
							}
						}
						
						//muzzle flash
						foreach(Transform mtf in part.FindModelTransforms("muzzleTransform"))
						{
							KSPParticleEmitter pEmitter = mtf.gameObject.GetComponent<KSPParticleEmitter>();
							//pEmitter.worldVelocity = rigidbody.velocity/1.1f;
							pEmitter.worldVelocity = mtf.rotation * new Vector3(0,0,muzzleFlashVelocity);
							pEmitter.EmitParticle();
						}
						
						firedBullet.AddComponent<BahaTurretBullet>();
						firedBullet.GetComponent<BahaTurretBullet>().sourceVessel = this.vessel;
						if(instakill)
						{
							firedBullet.GetComponent<BahaTurretBullet>().instakill = true;	
						}
						
						//firing bullet
						Vector3 firedVelocity = pitchTransform.rotation*new Vector3(bulletVelocity,randomY,randomZ).normalized*bulletVelocity;
						firedBullet.transform.position -= firedVelocity * Time.fixedDeltaTime;
						firedBullet.transform.position += rigidbody.velocity * Time.fixedDeltaTime;
						firedBullet.rigidbody.AddForce(this.rigidbody.velocity + firedVelocity, ForceMode.VelocityChange);
						
						//heat
						heat += heatPerShot;
					}
					else
					{
						spinningDown = true;
					}
				}
				
				timeCheck = Time.time;
			}
			else
			{
				spinningDown = true;	
			}
		}
		
		
		private bool FireLaser()
		{
			float chargeAmount = requestResourceAmount * TimeWarp.fixedDeltaTime;
			if(inTurretRange && !isOverheated && part.RequestResource(ammoName, chargeAmount)>=chargeAmount)
			{
				if(!audioSource.isPlaying)
				{
					audioSource.PlayOneShot (chargeSound);
					audioSource.Play();
					audioSource.loop = true;
					
				}
				foreach(Transform tf in part.FindModelTransforms("fireTransform"))
				{
					
					LineRenderer lr = tf.gameObject.GetComponent<LineRenderer>();
					lr.SetPosition(0, tf.position + rigidbody.velocity*Time.fixedDeltaTime);
					lr.SetPosition(1, (tf.forward*maxTargetingRange)+tf.position);
					Vector3 rayDirection = tf.forward;
					
					Vector3 targetDirection = Vector3.zero;  //autoTrack enhancer
					if(targetVessel!=null)
					{
						targetDirection = targetVessel.transform.position - tf.position;
					}
					if(targetVessel!=null && autoLockCapable && Vector3.Angle(rayDirection, targetDirection) < 10)
					{
						rayDirection = targetDirection;
					}
					
					Ray ray = new Ray(tf.position, rayDirection);
					RaycastHit hit;
					if(Physics.Raycast(ray, out hit, maxTargetingRange, 557057))
					{
						lr.SetPosition(1, hit.point);
						if(Time.time-timeCheck > 60/2000)
						{
							FXMonger.Explode(part, hit.point, 0.1f);	
						}
						try
						{
							Part p = Part.FromGO(hit.rigidbody.gameObject);
							if(p.vessel!=this.vessel)
							{
								p.temperature += laserDamage*TimeWarp.fixedDeltaTime;
							}
						}
						catch(NullReferenceException){}
					
					}
				
					
					
				}
				heat += heatPerShot * TimeWarp.CurrentRate;
				
				
				return true;
			}
			else
			{
				return false;
			}
		}
		
		
		//checks if you're about to shoot yourself (prevents it)
		private void CheckTarget()
		{
			Vector3 targetDirection = pitchTransform.right;
			Ray ray = new Ray(pitchTransform.position, targetDirection);
			RaycastHit hit;
			if(Physics.Raycast(ray, out hit, maxTargetingRange, 557057))
			{
				try{
					Part p = Part.FromGO(hit.rigidbody.gameObject);
					hitPart = p;
					if(p.vessel == this.vessel)
					{
						inTurretRange = false;
					}
				}
				catch(NullReferenceException)
				{}
				
			}
			else
			{
				hitPart = null;
			}
			
		}
		
		//overheat gauge
		static public VInfoBox InitHeatGauge(Part p)  //thanks DYJ
        {
            VInfoBox v = p.stackIcon.DisplayInfo();

            v.SetMsgBgColor(XKCDColors.DarkRed);
            v.SetMsgTextColor(XKCDColors.Orange);
            v.SetMessage("Overheat");
            v.SetProgressBarBgColor(XKCDColors.DarkRed);
            v.SetProgressBarColor(XKCDColors.Orange);

            return v;
        }
		
		
		
		//Animation Setup
		public static AnimationState[] SetUpAnimation(string animationName, Part part)  //Thanks Majiir!
        {
            var states = new List<AnimationState>();
            foreach (var animation in part.FindModelAnimators(animationName))
            {
                var animationState = animation[animationName];
                animationState.speed = 0;
                animationState.enabled = true;
                animationState.wrapMode = WrapMode.ClampForever;
                animation.Blend(animationName);
                states.Add(animationState);
            }
            return states.ToArray();
        }
		
	}
	
	
//==============================================================================
	
	
	public class BahaTurretBullet : MonoBehaviour
	{
		public float startTime;
		public float bulletLifeTime = 3;
		public Vessel sourceVessel;
		
		
		public Vector3 prevPosition;
		public Vector3 currPosition;
		
		public bool instakill = false;
		
		void Start()
		{
			startTime = Time.time;
			prevPosition = gameObject.transform.position;
		}
		
		void FixedUpdate()
		{
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
				Part dummyPart = new Part();
				FXMonger.Explode (dummyPart, hit.point, 0.01f);
				
				GameObject.Destroy(gameObject); //destroy bullet on collision
			}
			
			prevPosition = currPosition;
		}
		
	}
}

