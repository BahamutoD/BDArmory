using System;
using System.Collections.Generic;
using BDArmory.Core.Extension;
using BDArmory.Misc;
using BDArmory.Parts;
using BDArmory.UI;
using UnityEngine;
using System.Text;

namespace BDArmory.Control
{
	interface IBDAICommandable
	{
		MissileFire weaponManager { get; }
		Vessel vessel { get; }
		string currentStatus { get; }

		void ReleaseCommand();
		void CommandFollow(ModuleWingCommander leader, int followerIndex);
		void CommandAG(KSPActionGroup ag);
		void CommandFlyTo(Vector3 gpsCoords);
		void CommandAttack(Vector3 gpsCoords);
		void CommandTakeOff();

		Vector3d commandGPS { get; }
		PilotCommands currentCommand { get; }

	}

	public enum PilotCommands { Free, Attack, Follow, FlyTo }
}
