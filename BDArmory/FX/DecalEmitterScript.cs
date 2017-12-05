using UnityEngine;

namespace BDArmory.FX
{
    public class DecalEmitterScript : MonoBehaviour
    {
        private readonly float _maxCombineDistance = 0.6f;

        private readonly float _shrinkRateFlame = 2f;

        private readonly float _shrinkRateSmoke = 2f;
        private GameObject _destroyer;

        private float _destroyTimerStart;

        private float _highestEnergy;

        public void Start()
        {
            foreach (var otherFlame in DecalEmitters.FlameObjects)
            {
                if (
                    !((gameObject.transform.position - otherFlame.transform.position).sqrMagnitude
                      < _maxCombineDistance * _maxCombineDistance)) continue;

                Destroy(gameObject);
                return;
            }

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
            DecalEmitters.FlameObjects.Add(gameObject);
        }

        public void FixedUpdate()//pe is particle emitter
        {
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

            if (_destroyTimerStart != 0 && Time.time - _destroyTimerStart > _highestEnergy)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            foreach (var pe in gameObject.GetComponentsInChildren<KSPParticleEmitter>())
            {
                EffectBehaviour.RemoveParticleEmitter(pe);
            }

            if (DecalEmitters.FlameObjects.Contains(gameObject))
            {
                DecalEmitters.FlameObjects.Remove(gameObject);
            }
        }
    }
}
