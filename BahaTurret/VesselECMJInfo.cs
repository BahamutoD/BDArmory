using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BahaTurret
{
	[RequireComponent(typeof(Vessel))]
	public class VesselECMJInfo : MonoBehaviour
	{
		List<ModuleECMJammer> jammers;
		public Vessel vessel;

		bool jEnabled;
		public bool jammerEnabled
		{
			get
			{
				return jEnabled;
			}
		}

		float jStrength;
		public float jammerStrength
		{
			get
			{
				return jStrength;
			}
		}

		float lbs;
		public float lockBreakStrength
		{
			get
			{
				return lbs;
			}
		}

		float rcsr;
		public float rcsReductionFactor
		{
			get
			{
				return rcsr;
			}
		}


	

		void Awake()
		{
			jammers = new List<ModuleECMJammer>();
			vessel = GetComponent<Vessel>();

			GameEvents.onVesselCreate.Add(OnVesselCreate);
			GameEvents.onPartJointBreak.Add(OnPartJointBreak);
			GameEvents.onPartDie.Add(OnPartDie);
		}

		void OnDestroy()
		{
			GameEvents.onVesselCreate.Remove(OnVesselCreate);
			GameEvents.onPartJointBreak.Remove(OnPartJointBreak);
			GameEvents.onPartDie.Remove(OnPartDie);
		}

		void OnPartDie(Part p)
		{
			StartCoroutine(DelayedCleanJammerListRoutine());
		}

		void OnVesselCreate(Vessel v)
		{
			StartCoroutine(DelayedCleanJammerListRoutine());
		}

		void OnPartJointBreak(PartJoint j)
		{
			StartCoroutine(DelayedCleanJammerListRoutine());
		}

		public void AddJammer(ModuleECMJammer jammer)
		{
			if(!jammers.Contains(jammer))
			{
				jammers.Add(jammer);
			}
				
			UpdateJammerStrength();
		}

		public void RemoveJammer(ModuleECMJammer jammer)
		{
			jammers.Remove(jammer);

			UpdateJammerStrength();
		}

		void UpdateJammerStrength()
		{
			jEnabled = jammers.Count > 0;

			if(!jammerEnabled)
			{
				jStrength = 0;
			}

			float totaljStrength = 0;
			float totalLBstrength = 0;
			float jSpamFactor = 1;
			float lbreakFactor = 1;

			float rcsrTotalMass = 0;
			float rcsrTotal = 0;
			float rcsrCount = 0;

			foreach(var jammer in jammers)
			{
				if(jammer.signalSpam)
				{
					totaljStrength += jSpamFactor * jammer.jammerStrength;
					jSpamFactor *= 0.75f;
				}
				if(jammer.lockBreaker)
				{
					totalLBstrength += lbreakFactor * jammer.lockBreakerStrength;
					lbreakFactor *= 0.65f;
				}
				if(jammer.rcsReduction)
				{
					rcsrTotalMass += jammer.part.mass;
					rcsrTotal += jammer.rcsReductionFactor;
					rcsrCount++;
				}
			}

			lbs = totalLBstrength;
			jStrength = totaljStrength;

			if(rcsrCount > 0)
			{
				float rcsrAve = rcsrTotal / rcsrCount;
				float massFraction = rcsrTotalMass / vessel.GetTotalMass();
				rcsr = Mathf.Clamp(1-(rcsrAve * massFraction), 0.15f, 1);
			}
			else
			{
				rcsr = 1;
			}
		}

		public void DelayedCleanJammerList()
		{
			StartCoroutine(DelayedCleanJammerListRoutine());
		}

		IEnumerator DelayedCleanJammerListRoutine()
		{
			yield return null;
			yield return null;
			CleanJammerList();
		}

		void CleanJammerList()
		{
			vessel = GetComponent<Vessel>();

			if(!vessel)
			{
				Destroy(this);
			}
			jammers.RemoveAll(j => j == null);
			jammers.RemoveAll(j => j.vessel != vessel);

			foreach(var jam in vessel.FindPartModulesImplementing<ModuleECMJammer>())
			{
				if(jam.jammerEnabled)
				{
					AddJammer(jam);
				}
			}


			UpdateJammerStrength();
		}

		void OnGUI()
		{
			if(BDArmorySettings.DRAW_DEBUG_LABELS && vessel.isActiveVessel)
			{
				GUI.Label(new Rect(400, 500, 200, 200), "Total jammer strength: " + jammerStrength);
			}
		}
	}
}

