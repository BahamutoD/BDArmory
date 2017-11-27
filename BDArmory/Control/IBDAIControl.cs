using UnityEngine;

namespace BDArmory.Control
{
	public interface IBDAIControl
	{
		#region PartModule
		Vessel vessel { get; }
		Transform transform { get; }
		#endregion

		MissileFire weaponManager { get; }

		void ActivatePilot();
		void DeactivatePilot();
		void TogglePilot();

		bool pilotEnabled { get; }

		bool IsValidDirectFireTarget(Vessel target);
		bool CanEngage();

		#region WingCommander
		string currentStatus { get; }

		void ReleaseCommand();
		void CommandFollow(ModuleWingCommander leader, int followerIndex);
		void CommandAG(KSPActionGroup ag);
		void CommandFlyTo(Vector3 gpsCoords);
		void CommandAttack(Vector3 gpsCoords);
		void CommandTakeOff();

		Vector3d commandGPS { get; }
		PilotCommands currentCommand { get; }
		bool isLeadingFormation { get; set; }
		ModuleWingCommander commandLeader { get; }
		#endregion
	}

	public enum PilotCommands { Free, Attack, Follow, FlyTo }
}
