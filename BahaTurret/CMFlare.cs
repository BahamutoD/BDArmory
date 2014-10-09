using System;
using UnityEngine;

namespace BahaTurret
{
	public class CMFlare : MonoBehaviour
	{
		public float acquireDice;
		
		public Vessel sourceVessel;
		Vector3 relativePos;
		
		KSPParticleEmitter[] pEmitters;
		Light[] lights;
		float startTime;
		
		public Vector3 startVelocity;
		
		public bool alive = true;
		
		
		void Start()
		{
			acquireDice = UnityEngine.Random.Range(0f,100f);
			
			pEmitters = gameObject.GetComponentsInChildren<KSPParticleEmitter>();
			lights = gameObject.GetComponentsInChildren<Light>();
			startTime = Time.time;
			foreach(var pe in pEmitters)
			{
				pe.emit = true;	
			}
			gameObject.AddComponent<Rigidbody>();
			rigidbody.useGravity = false;
			rigidbody.velocity = startVelocity;
			if(!FlightGlobals.RefFrameIsRotating)
			{
				rigidbody.useGravity = false;
			}
			else
			{
				gameObject.AddComponent<KSPForceApplier>();
				gameObject.GetComponent<KSPForceApplier>().drag = 0.4f;
				rigidbody.useGravity = false;
			}
			
			rigidbody.mass = 0.001f;
			
			BDArmorySettings.Flares.Add(this.gameObject);
			
			if(sourceVessel!=null) relativePos = transform.position-sourceVessel.transform.position;
		}
		
		void FixedUpdate()
		{
			
			transform.rotation = Quaternion.LookRotation(rigidbody.velocity, FlightGlobals.getUpAxis());
			
			//downforce
			Vector3 downForce;
			if(sourceVessel != null) downForce = (Mathf.Clamp((float)sourceVessel.srfSpeed, 0.1f, 150)/150) * Mathf.Clamp01(20/Vector3.Distance(sourceVessel.transform.position,transform.position)) * 20 * -FlightGlobals.getUpAxis();
			else downForce = Vector3.zero;
			
			//turbulence
			foreach(var pe in pEmitters)
			{
				if(pe.useWorldSpace) 
				{
					pe.worldVelocity = 2*ParticleTurbulence.flareTurbulence + downForce;	
					if(FlightGlobals.getStaticPressure(transform.position) == 0) pe.emit = false;
				}
			}
			
			
			
			
			
			
			//floatingOrigin fix
			if(sourceVessel!=null && Vector3.Distance(transform.position-sourceVessel.transform.position, relativePos) > 800)
			{
				transform.position = sourceVessel.transform.position+relativePos + (rigidbody.velocity * Time.fixedDeltaTime);
			}
			if(sourceVessel!=null) relativePos = transform.position-sourceVessel.transform.position;
			//
			
			
			//if(useGravity) Forces ();
			
			
			if(Time.time -startTime > 0.3f && gameObject.collider==null)
			{
				gameObject.AddComponent<SphereCollider>();	
			}
			
			
			if(Time.time - startTime > 4) //stop emitting after 3 seconds
			{
				alive = false;
				BDArmorySettings.Flares.Remove(this.gameObject);
				
				foreach(var pe in pEmitters)
				{
					pe.emit = false;
				}
				foreach(var lgt in lights)
				{
					lgt.intensity = 0;		
				}
			}
			
			if(Time.time - startTime > 15) //delete object after x seconds
			{
				
				Destroy(gameObject);	
			}
			
		}
		
		
		
		
	}
}

