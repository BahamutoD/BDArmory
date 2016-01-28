using System;
using UnityEngine;

namespace BahaTurret
{
	[RequireComponent(typeof(Vessel))]
	public class VesselChaffInfo : MonoBehaviour
	{
		Vessel vessel;

		const float chaffMax = 500;
		const float chaffSubtractor = 120;
		const float speedRegenMult = 0.6f;
		const float minRegen = 40;
		const float maxRegen = 500;
		const float minMult = 0.03f;
		float chaffScalar = 500;

		void Awake()
		{
			vessel = GetComponent<Vessel>();
			if(!vessel)
			{
				Debug.Log("VesselChaffInfo was added to an object with no vessel component");
				Destroy(this);
				return;
			}
		}

		public float GetChaffMultiplier()
		{
			return Mathf.Clamp(chaffScalar/chaffMax, minMult, 1f);
		}

		public void Chaff()
		{
			chaffScalar = Mathf.Clamp(chaffScalar - chaffSubtractor, 0, chaffMax);
		}

		void FixedUpdate()
		{
			chaffScalar = Mathf.MoveTowards(chaffScalar, chaffMax, Mathf.Clamp(speedRegenMult*(float)vessel.srfSpeed, minRegen, maxRegen) * Time.fixedDeltaTime);
		}

		void OnGUI()
		{
			if(BDArmorySettings.DRAW_DEBUG_LABELS && vessel.isActiveVessel)
			{
				GUI.Label(new Rect(600, 600, 200, 200), "Chaff multiplier: " + GetChaffMultiplier());
			}
		}
	}
}

