using System;
using UnityEngine;


namespace BahaTurret
{
	public class BombTargeter : PartModule
	{
		
		public bool targeterEnabled = false;
		
		public override void OnStart (PartModule.StartState state)
		{
			
		}
		
		public override void OnFixedUpdate ()
		{
			if(targeterEnabled)
			{
				
			}
		}
	}
}

