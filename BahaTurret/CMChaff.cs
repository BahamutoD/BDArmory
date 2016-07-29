using System;
using System.Collections;
using UnityEngine;

namespace BahaTurret
{
	public class CMChaff : MonoBehaviour
	{
		KSPParticleEmitter pe;


		const float drag = 5;

		Vector3d geoPos;
		Vector3 velocity;
		CelestialBody body;

		public void Emit(Vector3 position, Vector3 velocity)
		{
			transform.position = position;
			this.velocity = velocity;
			gameObject.SetActive(true);
		}

		void OnEnable()
		{
			if(!pe)
			{
				pe = gameObject.GetComponentInChildren<KSPParticleEmitter>();
			}

			body = FlightGlobals.currentMainBody;
			if(!body)
			{
				gameObject.SetActive(false);
				return;
			}

			StartCoroutine(LifeRoutine());
		}

		IEnumerator LifeRoutine()
		{
			geoPos = VectorUtils.WorldPositionToGeoCoords(transform.position, body);

			pe.EmitParticle();

			float startTime = Time.time;
			while(Time.time - startTime < pe.maxEnergy)
			{
				transform.position = body.GetWorldSurfacePosition(geoPos.x, geoPos.y, geoPos.z);
				velocity += FlightGlobals.getGeeForceAtPosition(transform.position)*Time.fixedDeltaTime;
				Vector3 dragForce = (0.008f) * drag * 0.5f * velocity.sqrMagnitude * (float) FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(transform.position), FlightGlobals.getExternalTemperature(), body) * velocity.normalized;
				velocity -= (dragForce)*Time.fixedDeltaTime;
				transform.position += velocity * Time.fixedDeltaTime;
				geoPos = VectorUtils.WorldPositionToGeoCoords(transform.position, body);
				yield return new WaitForFixedUpdate();
			}

			gameObject.SetActive(false);
		}
	}
}

