using System;
using System.Collections;
using System.Collections.Generic;
using BDArmory.FX;
using BDArmory.Misc;
using BDArmory.UI;
using UnityEngine;

namespace BDArmory.CounterMeasure
{
    public class CMChaff : MonoBehaviour
    {
        KSPParticleEmitter pe;
        public Vessel sourceVessel;
        Vector3 relativePos;

        const float drag = 5;

        Vector3d geoPos;
        Vector3 velocity;
        CelestialBody body;

        Vector3 upDirection;
        public bool alive = true;
        float startTime;

        float lifeTime = 5;

        public void Emit(Vector3 position, Vector3 velocity)
        {
            transform.position = position;
            this.velocity = velocity;
            gameObject.SetActive(true);
        }

        void OnEnable()
        {
            if (!pe)
            {
               
                pe = gameObject.GetComponentInChildren<KSPParticleEmitter>();

                pe.emit = true;
                lifeTime = pe.maxEnergy;
                var main = pe.ps.main;

                main.emitterVelocityMode = ParticleSystemEmitterVelocityMode.Transform;
                main.maxParticles = 1;
            }

            body = FlightGlobals.currentMainBody;
            if (!body)
            {
                gameObject.SetActive(false);
                return;
            }
            upDirection = VectorUtils.GetUpDirection(transform.position);
            startTime = Time.time;
        }

        void FixedUpdate()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            if (velocity != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(velocity, upDirection);
            }


            //Particle effects
            Vector3 downForce = Vector3.zero;
            //downforce

            if (sourceVessel != null)
            {
                downForce = (Mathf.Clamp((float)sourceVessel.srfSpeed, 0.1f, 150) / 150) *
                            Mathf.Clamp01(20 / Vector3.Distance(sourceVessel.transform.position, transform.position)) * 20 *
                            -upDirection;
            }

 
            pe.worldVelocity = 2 * ParticleTurbulence.flareTurbulence + downForce;
           

            //floatingOrigin fix
            if (sourceVessel != null)
            {
                if (((transform.position - sourceVessel.transform.position) - relativePos).sqrMagnitude > 800 * 800)
                {
                    transform.position = sourceVessel.transform.position + relativePos;
                }

                relativePos = transform.position - sourceVessel.transform.position;
            }
            //


            if (Time.time - startTime > lifeTime) //stop emitting after lifeTime seconds
            {
                alive = false;

                this.pe.emit = false;
            }


            if (Time.time - startTime > lifeTime + 11) //disable object after x seconds
            {
                BDArmorySetup.numberOfParticleEmitters--;
                gameObject.SetActive(false);
                return;
            }


            //physics
            //atmospheric drag (stock)
            float simSpeedSquared = velocity.sqrMagnitude;
            Vector3 currPos = transform.position;
            float mass = 0.001f;
            float drag = 1f;
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
    }
}