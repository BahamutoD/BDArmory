using System;
using BDArmory.Core.Enum;
using BDArmory.Core.Extension;
using BDArmory.Core.Utils;
using BDArmory.FX;
using BDArmory.UI;
using UnityEngine;

namespace BDArmory.Parts
{
	public class BDExplosivePart : PartModule
	{
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "TNT mass equivalent"),
        UI_Label(affectSymCounterparts = UI_Scene.All, controlEnabled = true, scene = UI_Scene.All)]
        public float tntMass = 1;

	    [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Blast Radius"),
	     UI_Label(affectSymCounterparts = UI_Scene.All, controlEnabled = true, scene = UI_Scene.All)]
	    public float blastRadius = 10;

      
        [KSPAction("Arm")]
        public void ArmAG(KSPActionParam param)
        {
            Armed = true;
        }

		[KSPAction("Detonate")]
		public void DetonateAG(KSPActionParam param)
		{
		    Detonate();
		}

        

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Detonate", active = true)]
	    public void DetonateEvent()
	    {
            Detonate();
        }

	    public bool Armed { get; set; } = true;

        private double previousMass = -1;
		
		bool hasDetonated;
		
		public override void OnStart (StartState state)
		{
		    if (HighLogic.LoadedSceneIsFlight)
		    {
		        part.OnJustAboutToBeDestroyed += DetonateIfPossible;
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
	        if (part.Resources.Contains("HighExplosive"))
	        {
	            if (part.Resources["HighExplosive"].amount == previousMass) return;

	            tntMass = (float) (part.Resources["HighExplosive"].amount * part.Resources["HighExplosive"].info.density * 1000) * 1.5f;
      
	            previousMass = part.Resources["HighExplosive"].amount;
            }

	        blastRadius = BlastPhysicsUtils.CalculateBlastRange(tntMass);
        }
		
		public void DetonateIfPossible()
		{
			if(!hasDetonated && Armed && part.vessel.speed > 10)
			{
			    ExplosionFx.CreateExplosion(part.transform.position, tntMass,
			        "BDArmory/Models/explosion/explosionLarge", "BDArmory/Sounds/explode1", true, 0, part);
                hasDetonated = true;
			}
		}

	    private void Detonate()
	    {
	        part.Destroy();
            ExplosionFx.CreateExplosion(part.transform.position, tntMass,
	            "BDArmory/Models/explosion/explosionLarge", "BDArmory/Sounds/explode1",true, 0, part);

        }

	    public float GetBlastRadius()
	    {
	        CalculateBlast();
	        return blastRadius;
	    }
	}
}

