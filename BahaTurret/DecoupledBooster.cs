using System;
using System.Collections;
using UnityEngine;

namespace BahaTurret
{
	public class DecoupledBooster : MonoBehaviour
	{
		Rigidbody rb;

		IEnumerator SelfDestructRoutine()
		{
			foreach(var col in gameObject.GetComponentsInChildren<Collider>())
			{
				col.enabled = false;
			}
			yield return new WaitForSeconds(5);
			Destroy(gameObject);
		}

		public void DecoupleBooster(Vector3 startVelocity, float ejectSpeed)
		{
			transform.parent = null;

			rb = gameObject.AddComponent<Rigidbody>();
			gameObject.AddComponent<KSPForceApplier>();
			rb.velocity = startVelocity;
			rb.velocity += ejectSpeed * transform.forward;

			StartCoroutine(SelfDestructRoutine());
		}
	}
}

