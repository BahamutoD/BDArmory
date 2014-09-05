using System;
using UnityEngine;

namespace BahaTurret
{
	public class CMFlare : MonoBehaviour
	{
		
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
		}
		
		void FixedUpdate()
		{
			if(useGravity) Forces ();
			
			
			if(Time.time -startTime > 0.3f && gameObject.collider==null)
			{
				gameObject.AddComponent<SphereCollider>();	
			}
			
			
			if(Time.time - startTime > 3) //stop emitting after 3 seconds
			{
				alive = false;
				
				foreach(var pe in pEmitters)
				{
					pe.emit = false;
				}
				foreach(var lgt in lights)
				{
					lgt.intensity = 0;		
				}
			}
			
			if(Time.time - startTime > 8) //delete object after 6 seconds
			{
				BDArmorySettings.Flares.Remove(this.gameObject);
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

