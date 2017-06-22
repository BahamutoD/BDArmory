using System.Collections;
using System.Collections.Generic;
using UniLinq;
using UnityEngine;

namespace BDArmory.Misc
{
    public class DecoupledBooster : MonoBehaviour
    {
        Rigidbody rb;

        IEnumerator SelfDestructRoutine()
        {
            IEnumerator<Collider> col = gameObject.GetComponentsInChildren<Collider>().Cast<Collider>().GetEnumerator();
            while (col.MoveNext() )
            {
                if (col.Current == null) continue;
                col.Current.enabled = false;
            }
            col.Dispose();
            yield return new WaitForSeconds(5);
            Destroy(gameObject);
        }

        public void DecoupleBooster(Vector3 startVelocity, float ejectSpeed)
        {
            transform.parent = null;

            rb = gameObject.AddComponent<Rigidbody>();
            gameObject.AddComponent<KSPForceApplier>();
            rb.velocity = startVelocity;
            rb.velocity += ejectSpeed*transform.forward;

            StartCoroutine(SelfDestructRoutine());
        }
    }
}