using System.Collections;
using UnityEngine;

namespace BDArmory.CounterMeasure
{
    public class CMSmoke : MonoBehaviour
    {
        public Vector3 velocity;

        void OnEnable()
        {
            StartCoroutine(SmokeRoutine());
        }

        IEnumerator SmokeRoutine()
        {
            yield return new WaitForSeconds(10);

            gameObject.SetActive(false);
        }

        void FixedUpdate()
        {
            //physics
            //atmospheric drag (stock)
            float simSpeedSquared = velocity.sqrMagnitude;
            Vector3 currPos = transform.position;
            float mass = 0.01f;
            float drag = 5f;
            Vector3 dragForce = (0.008f * mass) * drag * 0.5f * simSpeedSquared *
                                (float)
                                FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(currPos),
                                    FlightGlobals.getExternalTemperature(), FlightGlobals.currentMainBody) *
                                velocity.normalized;

            velocity -= (dragForce / mass) * Time.fixedDeltaTime;
            //

            //gravity
            if (FlightGlobals.RefFrameIsRotating)
                velocity += FlightGlobals.getGeeForceAtPosition(transform.position) * Time.fixedDeltaTime;

            transform.position += velocity * Time.fixedDeltaTime;
        }

        public static bool RaycastSmoke(Ray ray)
        {
            if (!CMDropper.smokePool)
            {
                return false;
            }

            for (int i = 0; i < CMDropper.smokePool.size; i++)
            {
                Transform smokeTf = CMDropper.smokePool.GetPooledObject(i).transform;
                if (smokeTf.gameObject.activeInHierarchy)
                {
                    Plane smokePlane = new Plane((ray.origin - smokeTf.position).normalized, smokeTf.position);
                    float enter;
                    if (smokePlane.Raycast(ray, out enter))
                    {
                        float dist = (ray.GetPoint(enter) - smokeTf.position).sqrMagnitude;
                        if (dist < 16 * 16)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
