using System;
using UnityEngine;

namespace BahaTurret
{
	public class CMFlare : MonoBehaviour
	{
		
		
		public Vessel sourceVessel;
		Vector3 relativePos;
		
		KSPParticleEmitter[] pEmitters;
		Light[] lights;
		float startTime;
		bool useGravity;
		public Vector3 startVelocity;
		
		public bool alive = true;
		
		
		void Start()
		{
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
				useGravity = false;
				rigidbody.useGravity = false;
			}
			else
			{
				useGravity = true;	
				rigidbody.useGravity = false;
			}
			
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
			
			//turbulence?
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
			
			
			if(useGravity) Forces ();
			
			
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
		
		void Forces()
		{
			if(FlightGlobals.ActiveVessel!=null)
			{
				rigidbody.AddForce(FlightGlobals.getGeeForceAtPosition(FlightGlobals.ActiveVessel.GetWorldPos3D()), ForceMode.Acceleration);
				
				rigidbody.AddForce(0.3f * -rigidbody.velocity * (float)FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(FlightGlobals.ActiveVessel.GetWorldPos3D())));
			}
			else
			{
				rigidbody.AddForce(FlightGlobals.getGeeForceAtPosition(transform.position), ForceMode.Acceleration);	
			}
		}
		
		
	}
}

