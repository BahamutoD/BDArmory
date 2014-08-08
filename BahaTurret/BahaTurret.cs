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
		public float shellScale = 0.66f;
		
		[KSPField(isPersistant = false)]
		public bool onlyFireInRange = true;
		
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
		public string projectileColor = "255, 0, 0, 255";
		
		[KSPField(isPersistant = false)]
		public float cannonShellRadius = 30;
		[KSPField(isPersistant = false)]
		public float cannonShellPower = 8;
		
		
		[KSPField(isPersistant = false)]
		public float maxHeat = 3600;
		[KSPField(isPersistant = false)]
		public float heatPerShot = 75;
		[KSPField(isPersistant = false)]
		public float heatLoss = 250;
		//
		
		
		public float heat = 0;
		public bool isOverheated = false;
		
		[KSPField(isPersistant = false)]
		public string fireSoundPath = "BDArmory/Parts/50CalTurret/sounds/shot";
		[KSPField(isPersistant = false)]
		public string overheatSoundPath = "BDArmory/Parts/50CalTurret/sounds/turretOverheat";
		[KSPField(isPersistant = false)]
		public string chargeSoundPath = "BDArmory/Parts/laserTest/sounds/charge";
		[KSPField(isPersistant = false)]
		public string deployAnimName = "";
		[KSPField(isPersistant = false)]
		public string fireAnimName = "fireAnim";
		[KSPField(isPersistant = false)]
		public bool hasFireAnimation = true;
		[KSPField(isPersistant = false)]
		public bool spinDownAnimation = false;
		private bool spinningDown = false;
		
		[KSPField(isPersistant = false)]
		public bool oneShotSound = true;
		[KSPField(isPersistant = false)]
		public float soundRepeatTime = 1;

		
		
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
		private GameObject shell;
		
		private int numberOfGuns = 0;
		
		private float muzzleFlashVelocity = 4;
		
		private VInfoBox heatGauge = null;
		
		private bool wasFiring = false;
		
		
		
		
		
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
			if(deployAnimName!="")
			{
				deployStates = SetUpAnimation(deployAnimName, this.part);
			}
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
				bulletTrail.material.SetColor("_EmissiveColor", Misc.ParseColor255(projectileColor));
				
				bulletTrail.time = 0.02f;
				
				shell = GameDatabase.Instance.GetModel("BDArmory/Models/shell/model");
				shell.name = "shell";
				shell.transform.position = Vector3.zero;
				shell.transform.localScale = 0.001f * Vector3.one;
				shell.SetActive(true);
				
			}
			else if(weaponType == "cannon")
			{	
				bullet = new GameObject("Bullet");
				bullet.SetActive(true);
				Rigidbody bulletRB = bullet.AddComponent<Rigidbody>();
				if(!bulletDrop) bulletRB.useGravity = false;
				bullet.rigidbody.mass = bulletMass;
				TrailRenderer bulletTrail = bullet.AddComponent<TrailRenderer>();
				bulletTrail.startWidth = 0.8f;
				bulletTrail.endWidth = 0.01f;
				bulletTrail.material = new Material(Shader.Find ("KSP/Emissive/Diffuse"));
				bulletTrail.material.SetColor("_EmissiveColor", Misc.ParseColor255(projectileColor));
				
				bulletTrail.time = 0.02f;
				
				shell = GameDatabase.Instance.GetModel("BDArmory/Models/shell/model");
				shell.name = "shell";
				shell.transform.position = Vector3.zero;
				shell.transform.localScale = 0.001f * Vector3.one;
				shell.SetActive(true);
				
				
			}
			else if(weaponType == "laser")
			{
				chargeSound = GameDatabase.Instance.GetAudioClip(chargeSoundPath);
				if(HighLogic.LoadedSceneIsFlight)
				{
					audioSource.clip = fireSound;
				}
				foreach(Transform tf in part.FindModelTransforms("fireTransform"))
				{
					LineRenderer lr = tf.gameObject.AddComponent<LineRenderer>();
					lr.material = new Material(Shader.Find ("KSP/Emissive/Diffuse"));
					lr.material.SetColor("_EmissiveColor", Misc.ParseColor255(projectileColor));
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
			
			if(!vessel.IsControllable)
			{
				turretEnabled = false;
			}
			
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
				audioSource.Stop ();
				wasFiring = false;
				audioSource2.volume = Mathf.Sqrt (GameSettings.SHIP_VOLUME);
				audioSource2.PlayOneShot(overheatSound);
			}
			if(heat < maxHeat/3 && isOverheated) //reset on cooldown
			{
				isOverheated = false;
				heat = 0;
			}
			//
			
			
			
			//finding number of guns for fire volume reduction  
			//TODO: make central counter so this check doesn't happen every frame
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
			if(deployAnimName!="")
			{
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
			}
			else
			{
				deployed = turretEnabled;	
			}
			//aim+shooting
			if(deployed && (TimeWarp.WarpMode!=TimeWarp.Modes.HIGH || TimeWarp.CurrentRate == 1))
			{
				//if(vessel.isActiveVessel)
				//{
					Aim ();	
					CheckTarget ();
					if(Input.GetKey(BDArmorySettings.FIRE_KEY) && inTurretRange && !MapView.MapIsEnabled)
					{
						if(weaponType == "ballistic" || weaponType == "cannon") Fire ();
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
						if(!oneShotSound && wasFiring)
							{
								audioSource.Stop ();
								wasFiring = false;
								audioSource2.volume = Mathf.Sqrt (GameSettings.SHIP_VOLUME);
								audioSource2.PlayOneShot(overheatSound);	
							}
						
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
					
					
				//}else
				//{
				//	turretEnabled = false;
				//}
			}
			else
			{
				
				if(weaponType == "laser")
				{
					audioSource.Stop ();	
				}	
			}
			
			if(!oneShotSound &&  !Input.GetKey(BDArmorySettings.FIRE_KEY) && wasFiring)
			{
				wasFiring = false;
				audioSource.Stop ();
				audioSource2.volume = Mathf.Sqrt(GameSettings.SHIP_VOLUME);
				audioSource2.PlayOneShot(overheatSound);
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
				bool effectsShot = false;
				Transform[] fireTransforms = part.FindModelTransforms("fireTransform");
				foreach(Transform tf in fireTransforms)
				{
					if(part.RequestResource(ammoName, requestResourceAmount)>0 || BDArmorySettings.INFINITE_AMMO)
					{
						spinningDown = false;
						
						float randomY = UnityEngine.Random.Range(-20f, 20f)/accuracy;
						float randomZ = UnityEngine.Random.Range(-20f, 20f)/accuracy;
						
						//recoil
						gameObject.rigidbody.AddForce (pitchTransform.rotation*new Vector3(-bulletVelocity, -randomY, -randomZ).normalized*(bulletVelocity*bulletMass), ForceMode.Impulse);
						
						if(!effectsShot)
						{
							//sound
							if(oneShotSound)
							{
								audioSource.dopplerLevel = 0;
								audioSource.bypassListenerEffects = true;
								audioSource.volume = 1*(Mathf.Sqrt(GameSettings.SHIP_VOLUME))/Mathf.Sqrt(numberOfGuns);
								audioSource.PlayOneShot(fireSound);
							}
							else
							{
								wasFiring = true;
								if(!audioSource.isPlaying)
								{
									audioSource.clip = fireSound;
									audioSource.dopplerLevel = 0;
									audioSource.bypassListenerEffects = true;
									audioSource.volume = 1*(Mathf.Sqrt(GameSettings.SHIP_VOLUME))/Mathf.Sqrt(numberOfGuns);
									audioSource.loop = false;
									audioSource.Play();	
								}
								else
								{
									if (audioSource.time >= fireSound.length)
									{
										audioSource.time = soundRepeatTime;	
									}
								}
							}
							
							//animation
							if(hasFireAnimation)
							{
								foreach(AnimationState anim in fireStates)
								{
									fireAnimSpeed = Mathf.Clamp ((roundsPerMinute*anim.length)/60, 1, 5);
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
							
							//shell ejection
							if(BDArmorySettings.EJECT_SHELLS)
							{
								foreach(Transform sTf in part.FindModelTransforms("shellEject"))
								{
									GameObject ejectedShell = GameObject.Instantiate(shell, sTf.position + rigidbody.velocity*(Time.fixedDeltaTime), sTf.rotation) as GameObject;
									ejectedShell.transform.localScale = Vector3.one * shellScale;
									ShellCasing shellComponent = ejectedShell.AddComponent<ShellCasing>();
									shellComponent.initialV = rigidbody.velocity;
									
								}
							}
							effectsShot = true;
						}
						
						
						//firing bullet
						
						GameObject firedBullet = GameObject.Instantiate(bullet, tf.position, tf.rotation) as GameObject;
						Vector3 firedVelocity = pitchTransform.rotation*new Vector3(bulletVelocity,randomY,randomZ).normalized*bulletVelocity;
						firedBullet.transform.position -= firedVelocity * Time.fixedDeltaTime;
						firedBullet.transform.position += rigidbody.velocity * Time.fixedDeltaTime;
						firedBullet.rigidbody.AddForce(this.rigidbody.velocity + firedVelocity, ForceMode.VelocityChange);
						if(weaponType == "ballistic")
						{
							firedBullet.AddComponent<BahaTurretBullet>();
							firedBullet.GetComponent<BahaTurretBullet>().sourceVessel = this.vessel;
						}
						if(weaponType == "cannon")
						{
							firedBullet.AddComponent<CannonShell>();
							firedBullet.GetComponent<CannonShell>().sourceVessel = this.vessel;
							firedBullet.GetComponent<CannonShell>().blastPower = cannonShellPower;
							firedBullet.GetComponent<CannonShell>().radius = cannonShellRadius;
						}
						
						
						if(BDArmorySettings.INSTAKILL)
						{
							firedBullet.GetComponent<BahaTurretBullet>().instakill = true;	
						}
						
						
						
						//heat
						heat += heatPerShot;
					}
					else
					{
						spinningDown = true;
						if(!oneShotSound && wasFiring)
						{
							audioSource.Stop ();
							wasFiring = false;
							audioSource2.volume = Mathf.Sqrt (GameSettings.SHIP_VOLUME);
							audioSource2.PlayOneShot(overheatSound);	
						}
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
			if(inTurretRange && !isOverheated && (part.RequestResource(ammoName, chargeAmount)>=chargeAmount || BDArmorySettings.INFINITE_AMMO))
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
						if(Time.time-timeCheck > 60/1200 && BDArmorySettings.BULLET_HITS)
						{
							BulletHitFX.CreateBulletHit(hit.point, hit.normal);	
						}
						try
						{
							Part p = Part.FromGO(hit.rigidbody.gameObject);
							if(p.vessel!=this.vessel)
							{
								p.temperature += laserDamage*TimeWarp.fixedDeltaTime;
								
								if(BDArmorySettings.INSTAKILL) p.explode();
								
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
	
	
}

