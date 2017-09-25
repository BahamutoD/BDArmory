using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using BDArmory.Core.Extension;
using BDArmory.Core.Utils;
using UnityEngine;

namespace BDArmory.Core.Behaviour
{

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class TemperatureDamageSystem : MonoBehaviour
    {
        private static Dictionary<Part, float> _damagePartsDictionary = new Dictionary<Part, float>();

        //TODO: Add setting
        private static float maxDamageMultiplier = 100f;

        public static Dictionary<Part, float> DamagePartsDictionary
        {
            get { return _damagePartsDictionary; }
            set { _damagePartsDictionary = value; }
        }

        void Start()
        {
            GameEvents.onVesselCreate.Add(SetMaxDamage);
            GameEvents.onVesselLoaded.Add(SetMaxDamage);
            GameEvents.onPartDestroyed.Add(RemovePartFromDictionary);
        }

        private void RemovePartFromDictionary(Part part)
        {
            if (_damagePartsDictionary.ContainsKey(part))
            {
                _damagePartsDictionary.Remove(part);
            }
        }

        /// <summary>
        /// This method will set the max temperature for each part proportional to its dry mass
        /// </summary>
        /// <param name="vessel"></param>
        private static void SetMaxDamage(Vessel vessel)
        {
            using (List<Part>.Enumerator parts = vessel.parts.GetEnumerator())
            {
                while (parts.MoveNext())
                {
                    if (parts.Current == null) continue;

                    if (_damagePartsDictionary.ContainsKey(parts.Current)) continue;

                    _damagePartsDictionary.Add(parts.Current, 0);

                    parts.Current.skinMaxTemp = 10 *
                                                (float) Math.Pow(
                                                    parts.Current.mass * Mathf.Clamp(parts.Current.crashTolerance,1,100) * maxDamageMultiplier *
                                                    1000, (1.0 / 3.0)) + 1000;


                    parts.Current.maxTemp = 10 *
                                            (float) Math.Pow(
                                                parts.Current.mass * Mathf.Clamp(parts.Current.crashTolerance, 1, 100) * maxDamageMultiplier * 1000,
                                                (1.0 / 3.0)) + 1000;
                } 
            }

            Debug.Log("SetMaxDamage: Max part resist = " + _damagePartsDictionary.Keys.Max( x => x.maxTemp));
        }

        void FixedUpdate()
        {
            UpdateDamageToParts();
        }

        /// <summary>
        /// This method updates the damage/temperature based on the last damage value of the dictionary
        /// </summary>
        private static void UpdateDamageToParts()
        {
            using (var dictionaryEnumerator = _damagePartsDictionary.GetEnumerator())
            {
                while (dictionaryEnumerator.MoveNext())
                {
                    if (dictionaryEnumerator.Current.Key == null) continue;

                    Part p = dictionaryEnumerator.Current.Key;
                    float damage = dictionaryEnumerator.Current.Value;
                    p.skinTemperature = damage;
                    p.temperature = damage;
                }
            }
        }

        void OnDestroy()
        {
            GameEvents.onVesselCreate.Remove(SetMaxDamage);
            GameEvents.onVesselLoaded.Remove(SetMaxDamage);
            GameEvents.onPartDestroyed.Remove(RemovePartFromDictionary);
        }

        public static void SetDamageToPart(Part part, double damage)
        {

            DebugUtils.DisplayDebugMessage("Set Damage to Part " + part.name + " =" + damage);

              if (!_damagePartsDictionary.ContainsKey(part))
            {
                SetMaxDamage(part.vessel);
            }
            _damagePartsDictionary[part] = (float)damage;
        }

        public static void AddDamageToPart(Part part, double damage)
        {
            DebugUtils.DisplayDebugMessage("Add Damage to Part " + part.name + " =" + damage);

            if (!_damagePartsDictionary.ContainsKey(part))
            {
                SetMaxDamage(part.vessel);
            }

            _damagePartsDictionary[part] += (float)damage;
        }
    }
}
