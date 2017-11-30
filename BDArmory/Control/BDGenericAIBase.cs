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

		protected Transform vesselTransform => vessel.ReferenceTransform;

		protected Vector3d assignedPosition;

		public Vector3d assignedPositionGeo
		{
			get
			{
				return VectorUtils.GetWorldSurfacePostion(assignedPosition, vessel.mainBody);
			}
			protected set
			{
				assignedPosition = VectorUtils.WorldPositionToGeoCoords(value, vessel.mainBody);
			}
		}

		//wing commander
		public ModuleWingCommander commandLeader { get; protected set; }
		protected PilotCommands command;
		public bool isLeadingFormation { get; set; }
		public string currentStatus { get; protected set; } = "Free";
		protected int commandFollowIndex;

		public PilotCommands currentCommand => command;
		public Vector3d commandGPS => assignedPosition;


		public abstract void ActivatePilot();

		#region WingCommander

		public virtual void ReleaseCommand()
		{
			if (!vessel || command == PilotCommands.Free) return;
			if (command == PilotCommands.Follow && commandLeader)
			{
				List<IBDAIControl>.Enumerator pilots = commandLeader.vessel.FindPartModulesImplementing<IBDAIControl>().GetEnumerator();
				while (pilots.MoveNext())
				{
					if (pilots.Current == null) continue;
					pilots.Current.isLeadingFormation = false;
				}
				pilots.Dispose();
				commandLeader = null;
			}
			Debug.Log(vessel.vesselName + " was released from command.");
			command = PilotCommands.Free;

			assignedPositionGeo = vesselTransform.position;
		}

		public void CommandFollow(ModuleWingCommander leader, int followerIndex)
		{
			if (!pilotEnabled) return;
			if (leader == vessel || followerIndex < 0) return;

			Debug.Log(vessel.vesselName + " was commanded to follow.");
			command = PilotCommands.Follow;
			commandLeader = leader;
			commandFollowIndex = followerIndex;
			List<IBDAIControl>.Enumerator pilots = commandLeader.vessel.FindPartModulesImplementing<IBDAIControl>().GetEnumerator();
			while (pilots.MoveNext())
			{
				if (pilots.Current == null) continue;
				pilots.Current.isLeadingFormation = true;
			}
			pilots.Dispose();
		}

		public virtual void CommandAG(KSPActionGroup ag)
		{
			if (!pilotEnabled) return;
			vessel.ActionGroups.ToggleGroup(ag);
		}

		public virtual void CommandFlyTo(Vector3 gpsCoords)
		{
			if (!pilotEnabled) return;

			Debug.Log(vessel.vesselName + " was commanded to go to.");
			assignedPosition = gpsCoords;
			command = PilotCommands.FlyTo;
		}

		public virtual void CommandAttack(Vector3 gpsCoords)
		{
			if (!pilotEnabled) return;

			Debug.Log(vessel.vesselName + " was commanded to attack.");
			assignedPosition = gpsCoords;
			command = PilotCommands.Attack;
		}

		public virtual void CommandTakeOff()
		{
			ActivatePilot();
		}

		#endregion
	}
}
