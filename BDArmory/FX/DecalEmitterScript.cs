using UnityEngine;

namespace BDArmory.FX
{
    public class DecalEmitterScript : MonoBehaviour
    {
        private readonly float _maxCombineDistance = 0.6f;

        private readonly float _shrinkRateFlame = 2.25f;

        private readonly float _shrinkRateSmoke = 2.25f;
        private GameObject _destroyer;

        private float _destroyTimerStart;

        private float _highestEnergy;

        public void Start()
        {
            foreach (var pe in gameObject.GetComponentsInChildren<KSPParticleEmitter>())
            {
                var color = pe.material.color;
                color.a = color.a / 2;
                pe.material.SetColor("_TintColor", color);
                pe.force = -FlightGlobals.getGeeForceAtPosition(transform.position) / 3;
                if (!(pe.maxEnergy > _highestEnergy)) continue;
                _destroyer = pe.gameObject;
                _highestEnergy = pe.maxEnergy;
                EffectBehaviour.AddParticleEmitter(pe);
            }
        }

        public void FixedUpdate()//pe is particle emitter
        {
            if (_destroyTimerStart != 0 && Time.time - _destroyTimerStart > _highestEnergy)
            {
                Destroy(gameObject);
            }

            foreach (var pe in gameObject.GetComponentsInChildren<KSPParticleEmitter>())
            {
                var shrinkRate = pe.gameObject.name.Contains("smoke") ? _shrinkRateSmoke : _shrinkRateFlame;
                pe.maxSize = Mathf.MoveTowards(pe.maxSize, 0, shrinkRate * Time.fixedDeltaTime);
                pe.minSize = Mathf.MoveTowards(pe.minSize, 0, shrinkRate * Time.fixedDeltaTime);
                if (pe.maxSize < 0.1f && pe.gameObject == _destroyer && _destroyTimerStart == 0)
                {
                    _destroyTimerStart = Time.time;
                }

                var lightComponent = pe.gameObject.GetComponent<Light>();

                if (lightComponent != null)
                {
                    lightComponent.intensity = Random.Range(0f, pe.maxSize / 6);
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var pe in gameObject.GetComponentsInChildren<KSPParticleEmitter>())
            {
                EffectBehaviour.RemoveParticleEmitter(pe);
            }
        }
    }
}
