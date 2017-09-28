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
        private static Dictionary<Part, double> _damagePartsDictionary = new Dictionary<Part, double>();

        //TODO: Add setting
        private static float maxDamageMultiplier = 100f;

        public static Dictionary<Part, double> DamagePartsDictionary
        {
            get { return _damagePartsDictionary; }
            set { _damagePartsDictionary = value; }
        }

        void Start()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                GameEvents.onVesselCreate.Add(SetMaxDamage);
                GameEvents.onVesselLoaded.Add(SetMaxDamage);
                GameEvents.onPartDestroyed.Add(RemovePartFromDictionary);
            }
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
        void SetMaxDamage(Vessel vessel)
        {
            UpdateMaxTemp(vessel);

            Debug.Log("SetMaxDamage: Max part resist = " + _damagePartsDictionary.Keys.Max(x => x.maxTemp));
        }

        private static void UpdateMaxTemp(Vessel vessel)
        {
            using (List<Part>.Enumerator parts = vessel.parts.GetEnumerator())
            {
                while (parts.MoveNext())
                {
                    if (parts.Current == null) continue;

                    if (_damagePartsDictionary.ContainsKey(parts.Current)) continue;

                    _damagePartsDictionary.Add(parts.Current, 0);

                    parts.Current.skinMaxTemp = 10 *
                                                (float)Math.Pow(
                                                    parts.Current.mass * Mathf.Clamp(parts.Current.crashTolerance, 1, 100) * maxDamageMultiplier *
                                                    1000, (1.0 / 3.0)) + 1000;


                    parts.Current.maxTemp = 10 *
                                            (float)Math.Pow(
                                                parts.Current.mass * Mathf.Clamp(parts.Current.crashTolerance, 1, 100) * maxDamageMultiplier * 1000,
                                                (1.0 / 3.0)) + 1000;

                    // Part is flammable, max damage reduced
                    if (parts.Current.Resources.Where(x => x.resourceName.Contains("Liquid") ||
                                                         x.resourceName.Contains("Ox") ||
                                                         x.resourceName.Contains("Ker")).Any(y => y.amount > 0))
                    {
                        parts.Current.skinMaxTemp = Mathf.Max((float)(parts.Current.skinMaxTemp / 10f), 2000f);
                        parts.Current.maxTemp = Mathf.Max((float)(parts.Current.maxTemp / 10f), 2000f);
                    }

                }
            }
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
                    double damage = dictionaryEnumerator.Current.Value;
                    p.skinTemperature = damage;
                    p.temperature = damage;
                }
            }
        }

        void OnDestroy()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                GameEvents.onVesselCreate.Remove(SetMaxDamage);
                GameEvents.onVesselLoaded.Remove(SetMaxDamage);
                GameEvents.onPartDestroyed.Remove(RemovePartFromDictionary);
            }
        }

        public static void SetDamageToPart(Part part, double damage)
        {

            DebugUtils.DisplayDebugMessage("Set Damage to Part " + part.name + " =" + damage);

              if (!_damagePartsDictionary.ContainsKey(part))
            {
                UpdateMaxTemp(part.vessel);
            }
            _damagePartsDictionary[part] = damage;
        }

        public static void AddDamageToPart(Part part, double damage)
        {
            DebugUtils.DisplayDebugMessage("Add Damage to Part " + part.name + " =" + damage);

            if (!_damagePartsDictionary.ContainsKey(part))
            {
                UpdateMaxTemp(part.vessel);
            }

            _damagePartsDictionary[part] += damage;
        }
    }
}
