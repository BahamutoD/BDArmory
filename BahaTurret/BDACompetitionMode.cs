using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BahaTurret
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class BDACompetitionMode : MonoBehaviour
	{
		public static BDACompetitionMode Instance;

		void Awake()
		{
			if(Instance)
			{
				Destroy(Instance);
			}

			Instance = this;
		}

		void OnGUI()
		{
			if(competitionStarting)
			{
				GUIStyle cStyle = new GUIStyle(HighLogic.Skin.label);
				cStyle.fontStyle = FontStyle.Bold;
				cStyle.fontSize = 22;
				cStyle.alignment = TextAnchor.UpperCenter;
				Rect cLabelRect = new Rect(0, Screen.height / 6, Screen.width, 100);


				GUIStyle cShadowStyle = new GUIStyle(cStyle);
				Rect cShadowRect = new Rect(cLabelRect);
				cShadowRect.x += 2;
				cShadowRect.y += 2;
				cShadowStyle.normal.textColor = new Color(0, 0, 0, 0.75f);

				GUI.Label(cShadowRect, competitionStatus, cShadowStyle);
				GUI.Label(cLabelRect, competitionStatus, cStyle);
			}
		}

		//Competition mode
		public bool competitionStarting = false;
		string competitionStatus = "";
		Coroutine competitionRoutine = null;
		public void StartCompetitionMode(float distance)
		{
			if(!competitionStarting)
			{
				competitionRoutine = StartCoroutine(DogfightCompetitionModeRoutine(distance/2));
			}
		}

		public void StopCompetition()
		{
			if(competitionRoutine != null)
			{
				StopCoroutine(competitionRoutine);
			}

			competitionStarting = false;
		}

		IEnumerator DogfightCompetitionModeRoutine(float distance)
		{
			competitionStarting = true;
			competitionStatus = "Competition: Pilots are taking off.";
			Dictionary<BDArmorySettings.BDATeams, List<BDModulePilotAI>> pilots = new Dictionary<BDArmorySettings.BDATeams, List<BDModulePilotAI>>();
			pilots.Add(BDArmorySettings.BDATeams.A, new List<BDModulePilotAI>());
			pilots.Add(BDArmorySettings.BDATeams.B, new List<BDModulePilotAI>());
			foreach(var v in BDATargetManager.LoadedVessels)
			{
				if(!v || !v.loaded) continue;
				BDModulePilotAI pilot = null;
				foreach(var p in v.FindPartModulesImplementing<BDModulePilotAI>())
				{
					pilot = p;
					break;
				}

				if(!pilot || !pilot.weaponManager) continue;

				pilots[BDATargetManager.BoolToTeam(pilot.weaponManager.team)].Add(pilot);
				pilot.ActivatePilot();
				pilot.standbyMode = false;
				if(pilot.weaponManager.guardMode)
				{
					pilot.weaponManager.ToggleGuardMode();
				}
			}

			//clear target database so pilots don't attack yet
			BDATargetManager.ClearDatabase();

			if(pilots[BDArmorySettings.BDATeams.A].Count == 0 || pilots[BDArmorySettings.BDATeams.B].Count == 0)
			{
				Debug.Log("Unable to start competition mode - one or more teams is empty");
				competitionStatus = "Competition: Failed!  One or more teams is empty.";
				yield return new WaitForSeconds(2);
				competitionStarting = false;
				yield break;
			}

			BDModulePilotAI aLeader = pilots[BDArmorySettings.BDATeams.A][0];
			BDModulePilotAI bLeader = pilots[BDArmorySettings.BDATeams.B][0];

			aLeader.weaponManager.wingCommander.CommandAllFollow();
			bLeader.weaponManager.wingCommander.CommandAllFollow();


			//wait till the leaders are airborne
			while(aLeader && bLeader && (aLeader.vessel.LandedOrSplashed || bLeader.vessel.LandedOrSplashed))
			{
				yield return null;
			}

			if(!aLeader || !bLeader)
			{
				StopCompetition();
			}

			competitionStatus = "Competition: Sending pilots to start position.";
			Vector3 aDirection = Vector3.ProjectOnPlane(aLeader.vessel.CoM - bLeader.vessel.CoM, aLeader.vessel.upAxis).normalized;
			Vector3 bDirection = Vector3.ProjectOnPlane(bLeader.vessel.CoM - aLeader.vessel.CoM, bLeader.vessel.upAxis).normalized;

			Vector3 center = (aLeader.vessel.CoM + bLeader.vessel.CoM) / 2f;
			Vector3 aDestination = center + (aDirection * (distance+1250f));
			Vector3 bDestination = center + (bDirection * (distance+1250f));
			aDestination = VectorUtils.WorldPositionToGeoCoords(aDestination, FlightGlobals.currentMainBody);
			bDestination = VectorUtils.WorldPositionToGeoCoords(bDestination, FlightGlobals.currentMainBody);

			aLeader.CommandFlyTo(aDestination);
			bLeader.CommandFlyTo(bDestination);

			Vector3 centerGPS = VectorUtils.WorldPositionToGeoCoords(center, FlightGlobals.currentMainBody);

			//wait till everyone is in position
			bool waiting = true;
			while(waiting)
			{
				waiting = false;

				if(!aLeader || !bLeader)
				{
					StopCompetition();
				}

				if(Vector3.Distance(aLeader.transform.position, bLeader.transform.position) < distance*1.95f)
				{
					waiting = true;
				}
				else
				{
					foreach(var t in pilots.Keys)
					{
						foreach(var p in pilots[t])
						{
							if(p.currentCommand == BDModulePilotAI.PilotCommands.Follow && Vector3.Distance(p.vessel.CoM, p.commandLeader.vessel.CoM) > 1000f)
							{
								competitionStatus = "Competition: Waiting for teams to get in position.";
								waiting = true;
							}
						}
					}
				}

				yield return null;
			}

			//start the match
			foreach(var t in pilots.Keys)
			{
				foreach(var p in pilots[t])
				{
					if(!p) continue;

					//enable guard mode
					if(!p.weaponManager.guardMode)
					{
						p.weaponManager.ToggleGuardMode();
					}

					//report all vessels
					if(BDATargetManager.BoolToTeam(p.weaponManager.team) == BDArmorySettings.BDATeams.B)
					{
						BDATargetManager.ReportVessel(p.vessel, aLeader.weaponManager);
					}
					else
					{
						BDATargetManager.ReportVessel(p.vessel, bLeader.weaponManager);
					}

					//release command
					p.ReleaseCommand();
					p.defaultOrbitCoords = centerGPS;
				}
			}
			competitionStatus = "Competition starting!  Good luck!";
			yield return new WaitForSeconds(2);
			competitionStarting = false;

		}
	}
}

