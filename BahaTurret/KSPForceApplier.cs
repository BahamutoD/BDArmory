using System;
using UnityEngine;

namespace BahaTurret
{
	public class KSPForceApplier : MonoBehaviour
	{
		public float drag = 0.02f;

		Rigidbody rb;

		void Start()
		{
			rb = GetComponent<Rigidbody>();
		}
		
		void FixedUpdate()
		{
			if(rb!=null && !rb.isKinematic)
			{
				rb.useGravity = false;
				
				//atmospheric drag (stock)
				float simSpeedSquared = rb.velocity.sqrMagnitude;
				Vector3 currPos = transform.position;
				Vector3 dragForce = (0.008f * rb.mass) * drag * 0.5f * simSpeedSquared * (float) FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(currPos), FlightGlobals.getExternalTemperature(), FlightGlobals.currentMainBody) * rb.velocity.normalized;
				
				rb.velocity -= (dragForce/rb.mass)*Time.fixedDeltaTime;
				//
				
				//gravity
				if(FlightGlobals.RefFrameIsRotating) rb.velocity += FlightGlobals.getGeeForceAtPosition(transform.position) * Time.fixedDeltaTime;
			}
		}
	}
}

