using System;
using UnityEngine;

namespace BahaTurret
{
	public class BDAdjustableRail : PartModule
	{
		
		[KSPField(isPersistant = true)]
		public float railHeight = 0;
		
		
		[KSPField(isPersistant = true)]
		public float railLength = 1;
		
		
		Transform railLengthTransform;
		Transform railHeightTransform;
		
		public override void OnStart (PartModule.StartState state)
		{
			railLengthTransform = part.FindModelTransform("Rail");
			railHeightTransform = part.FindModelTransform("RailSleeve");
			
			railLengthTransform.localScale = new Vector3(1, railLength, 1);
			railHeightTransform.localPosition = new Vector3(0,railHeight,0);
		}
		
		[KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Height ++", active = true)]
		public void IncreaseHeight()
		{
			railHeight = Mathf.Clamp(railHeight-0.02f, -.16f, 0);
			railHeightTransform.localPosition = new Vector3(0,railHeight,0);
			
			foreach(Part sym in part.symmetryCounterparts)
			{
				sym.FindModuleImplementing<BDAdjustableRail>().UpdateHeight(railHeight);
			}
		}
		
		[KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Height --", active = true)]
		public void DecreaseHeight()
		{
			railHeight = Mathf.Clamp(railHeight+0.02f, -.16f, 0);
			railHeightTransform.localPosition = new Vector3(0,railHeight,0);
			
			foreach(Part sym in part.symmetryCounterparts)
			{
				sym.FindModuleImplementing<BDAdjustableRail>().UpdateHeight(railHeight);
			}
		}
		
		[KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Length ++", active = true)]
		public void IncreaseLength()
		{
			railLength = Mathf.Clamp(railLength+0.2f, 0.4f, 2f);
			railLengthTransform.localScale = new Vector3(1, railLength, 1);
			foreach(Part sym in part.symmetryCounterparts)
			{
				sym.FindModuleImplementing<BDAdjustableRail>().UpdateLength(railLength);
			}
		}
		
		[KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Length --", active = true)]
		public void DecreaseLength()
		{
			railLength = Mathf.Clamp(railLength-0.2f, 0.4f, 2f);
			railLengthTransform.localScale = new Vector3(1, railLength, 1);
			foreach(Part sym in part.symmetryCounterparts)
			{
				sym.FindModuleImplementing<BDAdjustableRail>().UpdateLength(railLength);
			}
		}
		
		public void UpdateHeight(float height)
		{
			railHeight = height;
			railHeightTransform.localPosition = new Vector3(0,railHeight,0);	
		}
		
		public void UpdateLength(float length)
		{
			railLength = length;
			railLengthTransform.localScale = new Vector3(1, railLength, 1);
		}
	}
}

