using UnityEngine;

namespace BDArmory.Misc
{
    public struct ViewScanResults
    {
        public bool foundMissile;
        public bool foundHeatMissile;
        public bool foundRadarMissile;
        public bool foundAGM;
        public bool firingAtMe;
        public float missileThreatDistance;
        public Vector3 threatPosition;
        public Vessel threatVessel;
        public MissileFire threatWeaponManager;
    }
}