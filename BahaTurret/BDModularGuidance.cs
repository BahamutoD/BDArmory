using System;
using UnityEngine;

namespace BahaTurret
{
	public class BDModularGuidance : PartModule
	{
		
		public GameObject target = null;
		
		//float timeFired = 0;
		public bool hasFired = false;
		bool guidanceActive = false;
		bool hasFiredEngine = false;
		
		Vector3 targetPosition;
		Vector3 targetDirection;
		Vessel targetVessel;
		bool targetInView = false;
		float prevDistance = -1;
		bool checkMiss = false;
		
		float startTime = -1;
		
		LineRenderer LR;
		
		Transform vesselTransform;
		
		public override void OnStart (PartModule.StartState state)
		{
			part.force_activate();
			vesselTransform = part.FindModelTransform("vesselTransform");
		}
		
		public override void OnFixedUpdate ()
		{
			if(hasFired && startTime == -1)
			{
				startTime = Time.time;	
			}
			
			if(hasFired && Time.time-startTime > 0.6f && !hasFiredEngine)
			{
				Debug.Log("===========BDMM Guidance Started==============");
				Part p = null;
				foreach(var pt in vessel.FindPartModulesImplementing<ModuleCommand>())
				{
					if(!pt.part.FindModuleImplementing<BDModularGuidance>()) p = pt.part;
				}
				if (p!=null)vessel.SetReferenceTransform(p);
				hasFiredEngine = true;
				guidanceActive = true;
				vessel.ActionGroups.groups[3] = true; //enable rcs
				//vessel.ActionGroups.groups[4] = true; //enable sas
				foreach(var engine in vessel.FindPartModulesImplementing<ModuleEngines>())
				{
					engine.part.force_activate();
					engine.Activate();
					
				}
				foreach(var engine in vessel.FindPartModulesImplementing<ModuleEnginesFX>())
				{
					engine.part.force_activate();
					engine.Activate();	
				}
				vessel.OnFlyByWire += new FlightInputCallback(GuidanceSteer);
			}
			
			if(hasFired && target!=null && guidanceActive)
			{
				WarnTarget();
				
				if(Vector3.Distance(target.transform.position, transform.position) < BDArmorySettings.PHYSICS_RANGE){
					
					targetPosition = target.transform.position + target.rigidbody.velocity*Time.fixedDeltaTime;
					
					//target CoM
					Part p = null;
					Vector3 targetCoMPos;
					try
					{
						p = Part.FromGO(target);	
					}
					catch(NullReferenceException){}
					if(p!=null)
					{
						targetCoMPos = p.vessel.findWorldCenterOfMass();
						targetPosition = targetCoMPos+target.rigidbody.velocity*Time.fixedDeltaTime;
					}
					float targetViewAngle = Vector3.Angle(transform.forward, targetPosition-transform.position);
					targetInView = (targetViewAngle < 20);
					//LookForCountermeasure();
					
					float targetDistance = Vector3.Distance(targetPosition, transform.position+rigidbody.velocity*Time.fixedDeltaTime);
					if(prevDistance == -1)
					{
						prevDistance = targetDistance;
					}
					
					if(targetDistance > 10 && targetInView) //guide towards where the target is going to be
					{
						targetPosition = targetPosition + target.rigidbody.velocity*(1/((target.rigidbody.velocity-vessel.rigidbody.velocity).magnitude/targetDistance));
						
					}
					
					//Control goes here
					
					
					
					////
					
					
					
					if(targetDistance < 500)
					{
						checkMiss = true;	
					}
					
					if(checkMiss && prevDistance-targetDistance < 0) //and moving away from target??
					{
							guidanceActive = false;
					}
					
					prevDistance = targetDistance;
					
					if(BDArmorySettings.DRAW_DEBUG_LINES)
					{
						if(!gameObject.GetComponent<LineRenderer>())
						{
							LR = gameObject.AddComponent<LineRenderer>();
							LR.material = new Material(Shader.Find("KSP/Emissive/Diffuse"));
							LR.material.SetColor("_EmissiveColor", Color.red);
						}else
						{
							LR = gameObject.GetComponent<LineRenderer>();
						}
						LR.SetVertexCount(4);
						
						LR.SetPosition(0, transform.position + rigidbody.velocity*Time.fixedDeltaTime);
						LR.SetPosition(1, transform.position + rigidbody.velocity*Time.fixedDeltaTime + (vessel.ctrlState.yaw *10)*transform.right);
						LR.SetPosition(2, transform.position + rigidbody.velocity*Time.fixedDeltaTime);
						LR.SetPosition(3, transform.position + rigidbody.velocity*Time.fixedDeltaTime + (vessel.ctrlState.pitch *10)*transform.up);
						
						
					}
					
					
				}
				else
				{
					Debug.Log ("Missile guidance fail. Target out of range or unloaded");
					guidanceActive = false;
				}
			}
					
		}
		
		void WarnTarget()
		{
			if(targetVessel == null)
			{
				if(FlightGlobals.ActiveVessel.gameObject == target) targetVessel = FlightGlobals.ActiveVessel;	
			}
			
			if(targetVessel!=null)
			{
				foreach(var wpm in targetVessel.FindPartModulesImplementing<MissileFire>())
				{
					wpm.MissileWarning();
					break;
				}
			}
		}
		
		public void GuidanceSteer(FlightCtrlState s)
		{
			if(guidanceActive)
			{
				targetDirection = (targetPosition-transform.position).normalized;
						
				Quaternion velRotation = Quaternion.LookRotation(rigidbody.velocity, transform.up);
				Vector3 offset = Quaternion.Inverse(velRotation) * (targetPosition-transform.position);
				s.yaw = Mathf.Clamp(offset.x/10, -1, 1);
				s.pitch = Mathf.Clamp(offset.y/10, -1, 1);
				s.mainThrottle = 1;
			}
		}
	}
}

