using System;

namespace BahaTurret
{
	public struct BDInputInfo
	{
		public string description;
		public string inputString;

		public BDInputInfo(string description)
		{
			this.description = description;
			this.inputString = string.Empty;
		}

		public BDInputInfo(string inputString, string description)
		{
			this.inputString = inputString;
			this.description = description;
		}
	}
}

