using System;
using UnityEngine;
using System.Collections;

namespace BahaTurret
{
	public class BDAParticleSelfDestruct : MonoBehaviour
	{
		KSPParticleEmitter pEmitter;
		BDAGaplessParticleEmitter gpe;

		void Awake()
		{
			pEmitter = gameObject.GetComponent<KSPParticleEmitter>();
			gpe = gameObject.GetComponent<BDAGaplessParticleEmitter>();
		}

		void Start()
		{
			if(pEmitter.pe.particleCount == 0)
			{
				Destroy(gameObject);
			}
			else
			{
				StartCoroutine(SelfDestructRoutine());
			}
		}

		IEnumerator SelfDestructRoutine()
		{
			pEmitter.emit = false;
			if(gpe)
			{
				gpe.emit = false;
			}
			yield return new WaitForSeconds(pEmitter.maxEnergy);
			Destroy(gameObject);
			yield break;
		}
	}
}

