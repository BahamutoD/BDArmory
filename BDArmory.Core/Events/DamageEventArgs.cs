using System;
using BDArmory.Core.Enum;

namespace BDArmory.Core.Events
{
    [Serializable]
    public class DamageEventArgs : EventArgs
    {
        public int VesselId { get; set; }
        public int PartId { get; set; }
        public float Damage { get; set; }
        public float Armor { get; set; }
        public DamageOperation Operation { get; set; }
    }
}