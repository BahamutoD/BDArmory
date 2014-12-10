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
		
		void Start()
		{
			pEmitter = gameObject.GetComponent<KSPParticleEmitter>();	
			pEmitter.emit = false;
		}
		
		void FixedUpdate()
		{
			if(emit)
			{
				maxDistance = (pEmitter.minSize/3) + (Mathf.Clamp((BDArmorySettings.numberOfParticleEmitters-1), 0, 20)*0.07f);
				
				Vector3 velocity = (part == null) ? rb.velocity : part.rigidbody.velocity;
				Vector3 originalLocalPosition = gameObject.transform.localPosition;
				Vector3 originalPosition = gameObject.transform.position;
				Vector3 startPosition = gameObject.transform.position + (velocity * Time.fixedDeltaTime);
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
			Vector3 velocity = (part == null) ? rb.velocity : part.rigidbody.velocity;
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

