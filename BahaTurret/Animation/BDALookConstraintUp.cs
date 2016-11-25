using System;
using UnityEngine;

namespace BahaTurret
{
    public class BDALookConstraintUp : PartModule
    {
        [KSPField(isPersistant = false)] public string targetName;

        [KSPField(isPersistant = false)] public string rotatorsName;


        Transform target;
        Transform rotator;


        public void Start()
        {
            target = part.FindModelTransform(targetName);
            rotator = part.FindModelTransform(rotatorsName);
        }

        public void FixedUpdate()
        {
            Vector3 upAxisV = rotator.up;

            rotator.LookAt(target, upAxisV);
        }
    }
}