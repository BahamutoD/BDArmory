using System;
using UnityEngine;
using System.Collections.Generic;

namespace BahaTurret
{
	public class CMFlare : MonoBehaviour
	{
		public float acquireDice = 0;
		
		public Vessel sourceVessel;
		Vector3 relativePos;
		
		List<KSPParticleEmitter> pEmitters = new List<KSPParticleEmitter>();
		List<BDAGaplessParticleEmitter> gaplessEmitters = new List<BDAGaplessParticleEmitter>();
		
		Light[] lights;
		float startTime;
		
		public Vector3 startVelocity;
		
		public bool alive = true;
		
		Vector3 upDirection;

		Rigidbody rb;

		void Start()
		{
			BDArmorySettings.numberOfParticleEmitters++;
			
			rb = gameObject.AddComponent<Rigidbody>();

			acquireDice = UnityEngine.Random.Range(0f,100f);
			
			foreach(var pe in gameObject.GetComponentsInChildren<KSPParticleEmitter>())
			{
				if(pe.useWorldSpace)	
				{
					BDAGaplessParticleEmitter gpe = pe.gameObject.AddComponent<BDAGaplessParticleEmitter>();
					gpe.rb = rb;
					gaplessEmitters.Add (gpe);
					gpe.emit = true;
				}
				else
				{
					pEmitters.Add(pe);	
					pe.emit = true;
				}
			}
			lights = gameObject.GetComponentsInChildren<Light>();
			startTime = Time.time;
		
			rb.velocity = startVelocity;
			rb.useGravity = false;
			rb.mass = 0.001f;

			//ksp force applier
			gameObject.AddComponent<KSPForceApplier>().drag = 0.4f;


			BDArmorySettings.Flares.Add(this.gameObject);
			
			if(sourceVessel!=null)
			{
				relativePos = transform.position-sourceVessel.transform.position;
			}

			upDirection = -FlightGlobals.getGeeForceAtPosition(transform.position).normalized;
		}
		
		void FixedUpdate()
		{

			transform.rotation = Quaternion.LookRotation(rb.velocity, upDirection);


			Vector3 downForce = Vector3.zero;
			//downforce

			if(sourceVessel != null)
			{
				downForce = (Mathf.Clamp((float)sourceVessel.srfSpeed, 0.1f, 150)/150) * Mathf.Clamp01(20/Vector3.Distance(sourceVessel.transform.position,transform.position)) * 20 * -upDirection;
			}


			
			//turbulence
			foreach(var pe in gaplessEmitters)
			{
				if(pe && pe.pEmitter)
				{
					try{
					pe.pEmitter.worldVelocity = 2*ParticleTurbulence.flareTurbulence + downForce;	
					}
					catch(NullReferenceException)
					{
						Debug.LogWarning("CMFlare NRE setting worldVelocity");
					}

					try
					{
					if(FlightGlobals.ActiveVessel && FlightGlobals.ActiveVessel.atmDensity <= 0)
					{
						pe.emit = false;
					}
					}
					catch(NullReferenceException)
					{
						Debug.LogWarning ("CMFlare NRE checking density");
					}
				}
			}




			//floatingOrigin fix
			if(sourceVessel!=null)
			{
				if(((transform.position-sourceVessel.transform.position)-relativePos).sqrMagnitude > 800 * 800)
				{
					transform.position = sourceVessel.transform.position+relativePos + (rb.velocity * Time.fixedDeltaTime);
				}

				relativePos = transform.position-sourceVessel.transform.position;
			}
			//

			

			/*
			if(Time.time -startTime > 0.3f && gameObject.collider==null)
			{
				gameObject.AddComponent<SphereCollider>();	
			}
			*/
			

			if(Time.time - startTime > 4) //stop emitting after 4 seconds
			{
				alive = false;
				BDArmorySettings.Flares.Remove(gameObject);
				
				foreach(var pe in pEmitters)
				{
					pe.emit = false;
				}
				foreach(var gpe in gaplessEmitters)
				{
					gpe.emit = false;	
				}
				foreach(var lgt in lights)
				{
					lgt.enabled = false;		
				}
			}



			if(Time.time - startTime > 15) //delete object after x seconds
			{
				BDArmorySettings.numberOfParticleEmitters--;
				Destroy(gameObject);	
			}

		}
		
		
		
		
	}
}

