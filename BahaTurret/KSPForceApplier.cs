using System;
using UnityEngine;

namespace BahaTurret
{
	public class KSPForceApplier : MonoBehaviour
	{
		public float drag = 0.02f;
		
		
		void FixedUpdate()
		{
			if(rigidbody!=null && !rigidbody.isKinematic)
			{
				rigidbody.useGravity = false;
				
				//atmospheric drag (stock)
				float simSpeedSquared = rigidbody.velocity.sqrMagnitude;
				Vector3 currPos = transform.position;
				Vector3 dragForce = (0.008f * rigidbody.mass) * drag * 0.5f * simSpeedSquared * (float) FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(currPos)) * rigidbody.velocity.normalized;
				
				rigidbody.velocity -= (dragForce/rigidbody.mass)*Time.fixedDeltaTime;
				//
				
				//gravity
				if(FlightGlobals.RefFrameIsRotating) rigidbody.velocity += FlightGlobals.getGeeForceAtPosition(transform.position) * Time.fixedDeltaTime;
			}
		}
	}
}

