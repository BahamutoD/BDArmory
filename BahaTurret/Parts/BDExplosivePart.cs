using System;
using UnityEngine;

namespace BahaTurret
{
	public class BDExplosivePart : PartModule
	{

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "Blast Radius"),
            UI_Label(affectSymCounterparts = UI_Scene.All, controlEnabled = true, scene = UI_Scene.All)]
        public float blastRadius = 50;


        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "Blast Power"),
        UI_Label(affectSymCounterparts = UI_Scene.All, controlEnabled = true, scene = UI_Scene.All)]
        public float blastPower = 25;

        [KSPField]
		public float blastHeat = -1;

		[KSPAction("Detonate")]
		public void DetonateAG(KSPActionParam param)
		{
			Detonate ();
		}

	    public bool Armed { get; set; } = true;
	
        private double previousMass = -1;
		/*
		[KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Proxy Detonate")]
		public bool proximityDetonation = false;
		*/

		//public GameObject target = null;
		
	//	bool hasFired = false;
		bool hasDetonated = false;
		
		public override void OnStart (StartState state)
		{
		    if (HighLogic.LoadedSceneIsFlight)
		    {
		        part.OnJustAboutToBeDestroyed += Detonate;
                part.force_activate();
		    }
		    
		    CalculateBlast();
		}

        public void Update()
        {
            if (HighLogic.LoadedSceneIsEditor)
                OnUpdateEditor();
        }

	    private void OnUpdateEditor()
	    {
            CalculateBlast();
        }

	    private void CalculateBlast()
	    {
	        if (!part.Resources.Contains("HighExplosive")) return;

            if (part.Resources["HighExplosive"].amount == previousMass) return;
           
	        var explosiveMass = part.Resources["HighExplosive"].amount;   

	        blastPower = (float)Math.Round(explosiveMass / 1.5f, 0);
            blastRadius = (float) (15 * Math.Pow(blastPower, (1.0 / 3.0)));

            //blastRadius = 1.75f * blastPower;

            previousMass = part.Resources["HighExplosive"].amount;
	    }
		
		public void Detonate()
		{
			if(!hasDetonated && Armed && this.part.vessel.speed > 10)
			{
				hasDetonated = true;
				if(part!=null) part.temperature = part.maxTemp + 100;
			    Vector3 position = part.vessel.CoM;
				ExplosionFX.CreateExplosion(position, blastRadius, blastPower, blastHeat, vessel, FlightGlobals.getUpAxis(), "BDArmory/Models/explosion/explosionLarge", "BDArmory/Sounds/explode1");
			}
		}
	}
}

