using System;

namespace BahaTurret
{
	public static class BDATooltips
	{
		public static string WM_RIPPLE
		{
			get
			{
				return "The ripple function allows you to fire missiles, rockets, or bombs at the configured rate by holding down the main fire key (" + BDInputSettingsFields.WEAP_FIRE_KEY.inputString + ")";
			}
		}


	}
}

