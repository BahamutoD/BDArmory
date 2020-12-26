using UnityEngine;

namespace BDArmory.FX
{
    public class DecalGaplessParticleEmitter : MonoBehaviour
    {
        public bool Emit;
        public float MaxDistance = 1.1f;
        public Part part;
        public KSPParticleEmitter PEmitter;
        public Rigidbody rb;
        Vector3 internalVelocity;
        Vector3 lastPos;

        Vector3 velocity
        {
            get
            {
                if (rb)
                {
                    return rb.velocity;
                }
                else if (part)
                {
                    return part.rb.velocity;
                }
                else
                {
                    return internalVelocity;
                }
            }
        }

        private void Start()
        {
            PEmitter = gameObject.GetComponent<KSPParticleEmitter>();
            PEmitter.emit = false;

            if (part != null)
            {
                //Debug.Log("Part " + Part.partName + "'s explosionPotential: " + Part.explosionPotential);
            }

            MaxDistance = PEmitter.minSize / 3;
        }

        void OnEnable()
        {
            lastPos = transform.position;
        }

        private void FixedUpdate()
        {
            if (!part && !rb)
            {
                internalVelocity = (transform.position - lastPos) / Time.fixedDeltaTime;
                lastPos = transform.position;
                if (PEmitter.emit && internalVelocity.sqrMagnitude > 562500)
                {
                    return; //dont bridge gap if floating origin shifted
                }
            }

            if (!Emit) return;

            //var velocity = part?.GetComponent<Rigidbody>().velocity ?? rb.velocity;
            var originalLocalPosition = gameObject.transform.localPosition;
            var originalPosition = gameObject.transform.position;
            var startPosition = gameObject.transform.position + velocity * Time.fixedDeltaTime;
            var originalGapDistance = Vector3.Distance(originalPosition, startPosition);
            var intermediateSteps = originalGapDistance / MaxDistance;

            PEmitter.EmitParticle();
            gameObject.transform.position = Vector3.MoveTowards(
                gameObject.transform.position,
                startPosition,
                MaxDistance);
            for (var i = 1; i < intermediateSteps; i++)
            {
                PEmitter.EmitParticle();
                gameObject.transform.position = Vector3.MoveTowards(
                    gameObject.transform.position,
                    startPosition,
                    MaxDistance);
            }
            gameObject.transform.localPosition = originalLocalPosition;
        }

        public void EmitParticles()
        {
            var velocity = part?.GetComponent<Rigidbody>().velocity ?? rb.velocity;
            var originalLocalPosition = gameObject.transform.localPosition;
            var originalPosition = gameObject.transform.position;
            var startPosition = gameObject.transform.position + velocity * Time.fixedDeltaTime;
            var originalGapDistance = Vector3.Distance(originalPosition, startPosition);
            var intermediateSteps = originalGapDistance / MaxDistance;

            //gameObject.transform.position = startPosition;
            for (var i = 0; i < intermediateSteps; i++)
            {
                PEmitter.EmitParticle();
                gameObject.transform.position = Vector3.MoveTowards(
                    gameObject.transform.position,
                    startPosition,
                    MaxDistance);
            }
            gameObject.transform.localPosition = originalLocalPosition;
        }
    }
}
