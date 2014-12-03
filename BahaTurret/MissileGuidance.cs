using System;
using System.Collections.Generic;
using UnityEngine;

namespace BahaTurret
{
	public class MissileGuidance
	{
		
		public static Vector3 GetAirToGroundTarget(Vector3 targetPosition, Vessel missileVessel, Vessel targetVessel)
		{
			float distanceToTarget = Vector3.Distance(targetVessel.transform.position, missileVessel.transform.position);
			float missileRadarAlt = GetRadarAltitude(missileVessel);
			Vector3 upDirection = -FlightGlobals.getGeeForceAtPosition(targetPosition).normalized;
			Vector3 missileSurfacePosition = missileVessel.transform.position - (missileRadarAlt*upDirection);
			Vector3 directionFromTarget = (missileSurfacePosition - targetPosition).normalized;
			
			if(missileVessel.srfSpeed < 50 && missileVessel.verticalSpeed < 5)
			{
				return missileVessel.transform.position + (5*missileVessel.transform.forward) + (20 * upDirection);	
			}
			
			float angle = 90-Vector3.Angle (upDirection, missileVessel.transform.position-targetPosition);
			
			Vector3 finalTarget = targetPosition +(Mathf.Clamp((distanceToTarget-350)*0.12f, 0, 600) * upDirection);
			
			float velocityAngle = Vector3.Angle(targetPosition-missileVessel.transform.position, missileVessel.rigidbody.velocity);
			
			if(velocityAngle < 10)
			{
				return finalTarget;
			}
			else
			{
				return targetPosition;	
			}
			
		}
		
		
		public static float GetRadarAltitude(Vessel vessel)
		{
			float radarAlt = Mathf.Clamp((float)(vessel.mainBody.GetAltitude(vessel.findWorldCenterOfMass())-vessel.terrainAltitude), 0, (float)vessel.altitude);
			return radarAlt;
		}
	}
}

