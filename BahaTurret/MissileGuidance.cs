using System;
using System.Collections.Generic;
using UnityEngine;

namespace BahaTurret
{
	public class MissileGuidance
	{
		
		public static Vector3 GetAirToGroundTarget(Vector3 targetPosition, Vessel missileVessel, Vessel targetVessel)
		{
			float distanceToTarget = Vector3.Distance(targetPosition, missileVessel.transform.position);
			Vector3 upDirection = -FlightGlobals.getGeeForceAtPosition(targetPosition).normalized;
			
			
			if(missileVessel.srfSpeed < 50 && missileVessel.verticalSpeed < 5)//gain altitude if launching from stationary
			{
				return missileVessel.transform.position + (5*missileVessel.transform.forward) + (20 * upDirection);	
			}
			
			Vector3 finalTarget = targetPosition +(Mathf.Clamp(Mathf.Clamp(distanceToTarget-350, 0, 600)*0.12f, 0, 600) * upDirection);
			
 			float velocityAngle = Vector3.Angle(targetPosition-missileVessel.transform.position, missileVessel.srf_velocity);
 			if(velocityAngle < 10)
			{
				return finalTarget;
			}
			else
			{
				return targetPosition;	
			}
			
		}

		public static Vector3 GetCruiseTarget(Vector3 targetPosition, Vessel missileVessel, Vessel targetVessel, float radarAlt)
		{
			Vector3 upDirection = -FlightGlobals.getGeeForceAtPosition(missileVessel.GetWorldPos3D()).normalized;
			float currentRadarAlt = GetRadarAltitude(missileVessel);
			float distanceSqr = (targetPosition-(missileVessel.transform.position-(currentRadarAlt*upDirection))).sqrMagnitude;

			float agmThreshDist = 3500;

			Vector3 planarDirectionToTarget = Misc.ProjectOnPlane(targetPosition-missileVessel.transform.position, missileVessel.transform.position, upDirection).normalized;

			if(distanceSqr < agmThreshDist*agmThreshDist)
			{
				return GetAirToGroundTarget(targetPosition, missileVessel, targetVessel);
			}
			else
			{


				if(missileVessel.srfSpeed < 50 && missileVessel.verticalSpeed < 5) //gain altitude if launching from stationary
				{
					return missileVessel.transform.position + (5*missileVessel.transform.forward) + (40 * upDirection);	
				}





				Vector3 tRayDirection = (Misc.ProjectOnPlane(missileVessel.rigidbody.velocity, missileVessel.transform.position, upDirection).normalized * 10) - (10*upDirection);
				Ray terrainRay = new Ray(missileVessel.transform.position, tRayDirection);
				RaycastHit rayHit;
				if(Physics.Raycast(terrainRay, out rayHit, 8000, 1<<15))
				{

					float minAlt = radarAlt*.85f;
					float maxAlt = radarAlt*1.15f;

					if(Vector3.Project(rayHit.point-missileVessel.transform.position, upDirection).sqrMagnitude < minAlt*minAlt || missileVessel.altitude < minAlt)
					{
						return missileVessel.transform.position + (10*planarDirectionToTarget) + (1.5f * upDirection);	
					}
					else if(Vector3.Project(rayHit.point-missileVessel.transform.position, upDirection).sqrMagnitude > maxAlt*maxAlt || (FlightGlobals.getAltitudeAtPos(rayHit.point) < 0 && missileVessel.altitude > maxAlt))
					{
						return missileVessel.transform.position + (10*planarDirectionToTarget) - (1 * upDirection);	
					}
					else
					{
						return missileVessel.transform.position + planarDirectionToTarget;
					}
				}
				else
				{
					return missileVessel.transform.position + (10*planarDirectionToTarget) - (3 * upDirection);	
				}

			}

		}
		
		
		public static float GetRadarAltitude(Vessel vessel)
		{
			float radarAlt = Mathf.Clamp((float)(vessel.mainBody.GetAltitude(vessel.findWorldCenterOfMass())-vessel.terrainAltitude), 0, (float)vessel.altitude);
			return radarAlt;
		}
	}
}

