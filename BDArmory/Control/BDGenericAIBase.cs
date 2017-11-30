using System;
using System.Collections.Generic;
using System.Linq;
using BDArmory.Core.Extension;
using BDArmory.Misc;
using BDArmory.UI;
using UnityEngine;
using System.Text;

namespace BDArmory.Control
{
	public abstract class BDGenericAIBase : PartModule
	{
		[KSPField(isPersistant = true)]
		public bool pilotEnabled { get; protected set; }

		public MissileFire weaponManager { get; protected set; }

		#region WingCommander

		public void CommandAG(KSPActionGroup ag)
		{
			if (!pilotEnabled) return;
			vessel.ActionGroups.ToggleGroup(ag);
		}

		#endregion
	}
}
