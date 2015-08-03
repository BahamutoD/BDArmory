using System;
using UnityEngine;
using System.Collections.Generic;

namespace BahaTurret
{
	public class CMFlare : MonoBehaviour
	{
		public Vessel sourceVessel;
		Vector3 relativePos;
		
		List<KSPParticleEmitter> pEmitters;// = new List<KSPParticleEmitter>();
		List<BDAGaplessParticleEmitter> gaplessEmitters;// = new List<BDAGaplessParticleEmitter>();
		
		Light[] lights;
		float startTime;
		
		public Vector3 startVelocity;
		
		public bool alive = true;
		
		Vector3 upDirection;

		public Vector3 velocity;

		public float thermal; //heat value
		float minThermal = BDArmorySettings.FLARE_THERMAL * 0.55f;

		float lifeTime = 6;

		void OnEnable()
		{

			thermal = BDArmorySettings.FLARE_THERMAL * UnityEngine.Random.Range(0.75f, 1.25f);

			if(gaplessEmitters == null || pEmitters == null)
			{
				gaplessEmitters = new List<BDAGaplessParticleEmitter>();
			
				pEmitters = new List<KSPParticleEmitter>();

				foreach(var pe in gameObject.GetComponentsInChildren<KSPParticleEmitter>())
				{
					if(pe.useWorldSpace)	
					{
						BDAGaplessParticleEmitter gpe = pe.gameObject.AddComponent<BDAGaplessParticleEmitter>();
						gaplessEmitters.Add (gpe);
						gpe.emit = true;
					}
					else
					{
						pEmitters.Add(pe);	
						pe.emit = true;
					}
				}
			}

			foreach(var emitter in gaplessEmitters)
			{
				emitter.emit = true;
			}

			foreach(var emitter in pEmitters)
			{
				emitter.emit = true;
			}

			BDArmorySettings.numberOfParticleEmitters++;


			if(lights == null)
			{
				lights = gameObject.GetComponentsInChildren<Light>();
			}

			foreach(var lgt in lights)
			{
				lgt.enabled = true;		
			}

			startTime = Time.time;
		
			//ksp force applier
			//gameObject.AddComponent<KSPForceApplier>().drag = 0.4f;


			BDArmorySettings.Flares.Add(this);
			
			if(sourceVessel!=null)
			{
				relativePos = transform.position-sourceVessel.transform.position;
			}

			upDirection = (transform.position - FlightGlobals.currentMainBody.transform.position).normalized;

			velocity = startVelocity;
		}
		
		void FixedUpdate()
		{
			if(!gameObject.activeInHierarchy)
			{
				return;
			}

			transform.rotation = Quaternion.LookRotation(velocity, upDirection);


			//Particle effects
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
					try
					{
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
			//


			//thermal decay
			thermal = Mathf.MoveTowards(thermal, minThermal, ((BDArmorySettings.FLARE_THERMAL-minThermal)/lifeTime)*Time.fixedDeltaTime);


			//floatingOrigin fix
			if(sourceVessel!=null)
			{
				if(((transform.position-sourceVessel.transform.position)-relativePos).sqrMagnitude > 800 * 800)
				{
					transform.position = sourceVessel.transform.position+relativePos;
				}

				relativePos = transform.position-sourceVessel.transform.position;
			}
			//

	
			

			if(Time.time - startTime > lifeTime) //stop emitting after lifeTime seconds
			{
				alive = false;
				BDArmorySettings.Flares.Remove(this);
				
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



			if(Time.time - startTime > lifeTime+11) //disable object after x seconds
			{
				BDArmorySettings.numberOfParticleEmitters--;
				gameObject.SetActive(false);
				return;
			}


			//physics
			//atmospheric drag (stock)
			float simSpeedSquared = velocity.sqrMagnitude;
			Vector3 currPos = transform.position;
			float mass = 0.001f;
			float drag = 1f;
			Vector3 dragForce = (0.008f * mass) * drag * 0.5f * simSpeedSquared * (float) FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(currPos), FlightGlobals.getExternalTemperature(), FlightGlobals.currentMainBody) * velocity.normalized;
			
			velocity -= (dragForce/mass)*Time.fixedDeltaTime;
			//
			
			//gravity
			if(FlightGlobals.RefFrameIsRotating) velocity += FlightGlobals.getGeeForceAtPosition(transform.position) * Time.fixedDeltaTime;

			transform.position += velocity * Time.fixedDeltaTime;

		}
		
		
		
		
	}
}

