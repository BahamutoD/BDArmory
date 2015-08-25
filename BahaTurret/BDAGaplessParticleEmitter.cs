using System;
using UnityEngine;

namespace BahaTurret
{
	public class BDAGaplessParticleEmitter : MonoBehaviour
	{
		public KSPParticleEmitter pEmitter;	
		
		private float maxDistance = 0.6f;
		
		public bool emit = false;
		
		public Part part = null;
		
		public Rigidbody rb;

		Vector3 internalVelocity;
		Vector3 lastPos;

		bool useInternalV = false;

		Vector3 velocity
		{
			get
			{
				if(rb)
				{
					return rb.velocity;
				}
				else if(part)
				{
					return part.rb.velocity;
				}
				else
				{
					useInternalV = true;
					return internalVelocity;
				}
			}
		}
		
		void Start()
		{
			pEmitter = gameObject.GetComponent<KSPParticleEmitter>();	
			pEmitter.emit = false;
		}

		void OnEnable()
		{
			lastPos = transform.position;
		}
		
		void FixedUpdate()
		{
			if(!part && !rb)
			{
				internalVelocity = (transform.position-lastPos)/Time.fixedDeltaTime;
				lastPos = transform.position;
				if(emit && internalVelocity.sqrMagnitude > 562500)
				{
					return; //dont bridge gap if floating origin shifted
				}

			}

			if(emit)
			{
				maxDistance = Mathf.Clamp((pEmitter.minSize/3), 0.5f, 5) + (Mathf.Clamp((BDArmorySettings.numberOfParticleEmitters-1), 0, 20)*0.07f);

				Vector3 originalLocalPosition = gameObject.transform.localPosition;
				Vector3 originalPosition = gameObject.transform.position;

				Vector3 startPosition = gameObject.transform.position;
				if(useInternalV)
				{
					startPosition -= (velocity*Time.fixedDeltaTime);
				}
				else
				{
					startPosition += (velocity * Time.fixedDeltaTime);
				}
				float originalGapDistance = Vector3.Distance(originalPosition, startPosition);
				float intermediateSteps = originalGapDistance/maxDistance;
				
				pEmitter.EmitParticle();
				gameObject.transform.position = Vector3.MoveTowards(gameObject.transform.position, startPosition, maxDistance);
				for(int i = 1; i < intermediateSteps-1; i++)
				{
					pEmitter.EmitParticle();
					gameObject.transform.position = Vector3.MoveTowards(gameObject.transform.position, startPosition, maxDistance);
				}
				gameObject.transform.localPosition = originalLocalPosition;
			}
		}
		
		public void EmitParticles()
		{
			Vector3 originalLocalPosition = gameObject.transform.localPosition;
			Vector3 originalPosition = gameObject.transform.position;
			Vector3 startPosition = gameObject.transform.position + (velocity * Time.fixedDeltaTime);
			float originalGapDistance = Vector3.Distance(originalPosition, startPosition);
			float intermediateSteps = originalGapDistance/maxDistance;
			
			//gameObject.transform.position = startPosition;
			for(int i = 0; i < intermediateSteps; i++)
			{
				pEmitter.EmitParticle();
				gameObject.transform.position = Vector3.MoveTowards(gameObject.transform.position, startPosition, maxDistance);
			}
			gameObject.transform.localPosition = originalLocalPosition;
		}
	}
}

