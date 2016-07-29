using System;
using UnityEngine;

namespace BahaTurret
{
	public class BDExplosivePart : PartModule
	{
		
		[KSPField(isPersistant = false)]
		public float blastRadius = 50;
		[KSPField(isPersistant = false)]
		public float blastPower = 25;

		[KSPField]
		public float blastHeat = -1;

		[KSPAction("Detonate")]
		public void DetonateAG(KSPActionParam param)
		{
			Detonate ();
		}
	

		/*
		[KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Proxy Detonate")]
		public bool proximityDetonation = false;
		*/

		//public GameObject target = null;
		
	//	bool hasFired = false;
		bool hasDetonated = false;
		
		public override void OnStart (PartModule.StartState state)
		{
			part.OnJustAboutToBeDestroyed += new Callback(Detonate);
			part.force_activate();
			
		}
		
		public override void OnFixedUpdate()
		{
			/*
			if(hasFired && proximityDetonation && Vector3.Distance(target.transform.position, transform.position+rigidbody.velocity*Time.fixedDeltaTime) < blastRadius/2)
			{
				Detonate();
			}
			*/
		}
		
		public void Detonate()
		{
			if(!hasDetonated)
			{
				hasDetonated = true;
				if(part!=null) part.temperature = part.maxTemp + 100;
				Vector3 position = transform.position+part.rb.velocity*Time.fixedDeltaTime;
				ExplosionFX.CreateExplosion(position, blastRadius, blastPower, blastHeat, vessel, FlightGlobals.getUpAxis(), "BDArmory/Models/explosion/explosionLarge", "BDArmory/Sounds/explode1");
			}
		}
	}
}

