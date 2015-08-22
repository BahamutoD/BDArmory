using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace BahaTurret
{
	public class ModuleRadar : PartModule
	{
		[KSPField]
		public bool canLock = true;
		public bool locked = false;

		[KSPField]
		public bool canScan = true;

		[KSPField]
		public bool canRecieveRadarData = false;
		public ModuleRadar linkedRadar;

		[KSPField(isPersistant = true)]
		public bool linked = false;

		bool unlinkNullRadar = false;

		public bool linkWindowOpen = false;
		float numberOfAvailableLinks = 0;
		public Rect linkWindowRect = new Rect(0,0,0,0);
		float linkRectWidth = 200;
		float linkRectEntryHeight = 26;
			
		[KSPField(isPersistant = true)]
		public string linkedVesselID;
		
		[KSPField]
		public string rangeIncrements = "5000,10000,20000";
		public float[] rIncrements;

		[KSPField(isPersistant = true)]		
		public int rangeIndex = 99;

		public float maxRange;

		[KSPField]
		public bool omnidirectional = true;

		[KSPField]
		public float directionalFieldOfView = 90;

		[KSPField]
		public float boresightFOV = 10;


		[KSPField]
		public float scanRotationSpeed = 120; //in degrees per second

		[KSPField]
		public float lockRotationSpeed = 120;

		[KSPField]
		public float lockRotationAngle = 4;

		[KSPField]
		public string rotationTransformName = string.Empty;
		Transform rotationTransform;


		[KSPField(isPersistant = true)]
		public bool radarEnabled = false;

		[KSPField]
		public float minSignalThreshold = 90;

		[KSPField]
		public float minLockedSignalThreshold = 90;

		[KSPField]
		public bool canTrackWhileScan = false;

		[KSPField]
		public int rwrThreatType = 0;
		public RadarWarningReceiver.RWRThreatTypes rwrType = RadarWarningReceiver.RWRThreatTypes.SAM;

		//contacts
		TargetSignatureData[] contacts;
		TargetSignatureData[] attemptedLocks;
		public TargetSignatureData lockedTarget = TargetSignatureData.noTarget;

		//GUI
		bool drawGUI = false;
		public static Rect radarWindowRect;
		public static bool radarRectInitialized = false;
		float radarScreenSize = 360;
		float windowBorder = 10;
		float headerHeight = 12;
		float controlsHeight = 58;
		Vector2 pingSize = new Vector2(16,8);
		float signalPersistTime;
		Texture2D rollIndicatorTexture = GameDatabase.Instance.GetTexture(BDArmorySettings.textureDir + "radarRollIndicator", false);
		public static Texture2D omniBgTexture = GameDatabase.Instance.GetTexture(BDArmorySettings.textureDir + "omniRadarTexture", false);
		Texture2D radialBgTexture = GameDatabase.Instance.GetTexture(BDArmorySettings.textureDir + "radialRadarTexture", false);
		Texture2D scanTexture = GameDatabase.Instance.GetTexture(BDArmorySettings.textureDir + "omniRadarScanTexture", false);
		Texture2D lockIcon = GameDatabase.Instance.GetTexture(BDArmorySettings.textureDir + "lockedRadarIcon", false);
		Texture2D radarContactIcon = GameDatabase.Instance.GetTexture(BDArmorySettings.textureDir + "radarContactIcon", false);
		Texture2D friendlyContactIcon = GameDatabase.Instance.GetTexture(BDArmorySettings.textureDir + "friendlyContactIcon", false);
		float lockIconSize = 24;
		GUIStyle distanceStyle;
		GUIStyle lockStyle;
		GUIStyle radarTopStyle;

		//scanning
		[KSPField]
		public bool showDirectionWhileScan = false;
		[KSPField(isPersistant = true)]
		public float currentAngle = 0;
		float currentAngleLock = 0;
		Transform referenceTransform;
		float radialScanDirection = 1;


		//radar selector
		bool showSelector = false;
		Vector2 selectorPos = Vector2.zero;


		public bool boresightScan = false;

		public List<ModuleRadar> availableRadarLinks;

		//locking
		[KSPField]
		public float lockAttemptFOV = 2;
		float lockScanAngle = 0;

		public bool slaveTurrets = false;

		public MissileLauncher lastMissile;

		public ModuleTurret lockingTurret;

		public MissileFire weaponManager;

		[KSPEvent(active = true, guiActive = true, guiActiveEditor = false)]
		public void Toggle()
		{
			if(radarEnabled)
			{
				DisableRadar();
			}
			else
			{
				EnableRadar();
			}
		}

		public void EnableRadar()
		{
			foreach(var radar in vessel.FindPartModulesImplementing<ModuleRadar>())
			{
				if(radar!=this)
				{
					radar.DisableRadar();
				}
			}

			radarEnabled = true;
			foreach(var mf in vessel.FindPartModulesImplementing<MissileFire>())
			{
				mf.radar = this;
				weaponManager = mf;
				break;
			}
		}

		public void DisableRadar()
		{
			radarEnabled = false;

			foreach(var mf in vessel.FindPartModulesImplementing<MissileFire>())
			{
				mf.radar = null;
				break;
			}

		}
		

		public override void OnStart (StartState state)
		{
			base.OnStart (state);

			if(HighLogic.LoadedSceneIsFlight)
			{
				RadarUtils.SetupRadarCamera();

				distanceStyle = new GUIStyle();
				distanceStyle.normal.textColor = new Color(0,1,0,0.75f);
				distanceStyle.alignment = TextAnchor.UpperLeft;

				lockStyle = new GUIStyle();
				lockStyle.normal.textColor = new Color(0,1,0,0.75f);
				lockStyle.alignment = TextAnchor.LowerCenter;
				lockStyle.fontSize = 16;

				radarTopStyle = new GUIStyle();
				radarTopStyle.normal.textColor = new Color(0, 1, 0, 0.65f);
				radarTopStyle.alignment = TextAnchor.UpperCenter;
				radarTopStyle.fontSize = 12;

				rIncrements = Misc.ParseToFloatArray(rangeIncrements);
				rangeIndex = Mathf.Clamp(rangeIndex, 0, rIncrements.Length-1);
				maxRange = rIncrements[rIncrements.Length-1];
				signalPersistTime = omnidirectional ? 360/(scanRotationSpeed+5) : directionalFieldOfView/(scanRotationSpeed+5);

				if(rotationTransformName!=string.Empty)
				{
					rotationTransform = part.FindModelTransform(rotationTransformName);
				}

				if(!radarRectInitialized)
				{
					float width = radarScreenSize + (2*windowBorder);
					float height = radarScreenSize + (2*windowBorder) + headerHeight + controlsHeight;
					radarWindowRect = new Rect(Screen.width - width, Screen.height - height, width, height);
				}

				lockedTarget = TargetSignatureData.noTarget;


				contacts = new TargetSignatureData[10];
				TargetSignatureData.ResetTSDArray(ref contacts);

				attemptedLocks = new TargetSignatureData[3];
				TargetSignatureData.ResetTSDArray(ref attemptedLocks);


				referenceTransform = (new GameObject()).transform;
				referenceTransform.parent = transform;
				referenceTransform.localPosition = Vector3.zero;

				lockingTurret = part.FindModuleImplementing<ModuleTurret> ();

				rwrType = (RadarWarningReceiver.RWRThreatTypes) rwrThreatType;

				unlinkNullRadar = false;

				foreach(var wm in vessel.FindPartModulesImplementing<MissileFire>())
				{
					wm.radars.Add(this);
				}

				StartCoroutine(StartUpRoutine());

			}
		}

		IEnumerator StartUpRoutine()
		{
			while(!FlightGlobals.ready || vessel.packed)
			{
				yield return null;
			}

			if (radarEnabled)
			{
				EnableRadar();
			}

			if(linked)
			{
				foreach(var v in FlightGlobals.Vessels)
				{
					if(v.id.ToString() == linkedVesselID)
					{
						foreach(var mr in v.FindPartModulesImplementing<ModuleRadar>())
						{
							if(mr.radarEnabled)
							{
								LinkToRadar(mr);
								break;
							}
						}
						break;
					}
				}
				if(!linkedRadar)
				{
					Debug.Log("Radar was linked, but linked radar doesn't exist.");
					UnlinkRadar();
				}

			}

			unlinkNullRadar = true;
		}
		
		// Update is called once per frame
		void Update ()
		{
			if(HighLogic.LoadedSceneIsFlight && FlightGlobals.ready && !vessel.packed && radarEnabled)
			{
				if(omnidirectional)
				{
					referenceTransform.position = vessel.transform.position;
					referenceTransform.rotation = Quaternion.LookRotation(VectorUtils.GetNorthVector(transform.position, vessel.mainBody), VectorUtils.GetUpDirection(transform.position));
				}
				else
				{
					referenceTransform.position = vessel.transform.position;
					referenceTransform.rotation = Quaternion.LookRotation(part.transform.up, VectorUtils.GetUpDirection(referenceTransform.position));
				}
				UpdateInputs();
			}

			drawGUI = (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready && !vessel.packed && radarEnabled && vessel.isActiveVessel && BDArmorySettings.GAME_UI_ENABLED);
		}

		void FixedUpdate()
		{
			if(HighLogic.LoadedSceneIsFlight && FlightGlobals.ready)
			{
				if(!vessel.IsControllable && radarEnabled)
				{
					DisableRadar();
				}

				if(radarEnabled)
				{
					if(locked)
					{
						UpdateLock();
						if (canTrackWhileScan)
						{
							Scan ();
						}
					}
					else if(boresightScan)
					{
						BoresightScan();
					}
					else if(canScan)
					{
						Scan ();
					}


					if(unlinkNullRadar && linked && !linkedRadar)
					{
						UnlinkRadar();
					}
				}
			}
		}

		void LateUpdate()
		{
			if (HighLogic.LoadedSceneIsFlight && canScan)
			{
				UpdateModel ();
			}
		}

		void UpdateInputs()
		{
			if(!vessel.isActiveVessel)
			{
				return;
			}


			if(BDInputUtils.GetKey(BDInputSettingsFields.RADAR_SLEW_RIGHT))
			{
				ShowSelector();
				SlewSelector(Vector2.right);
			}
			else if(BDInputUtils.GetKey(BDInputSettingsFields.RADAR_SLEW_LEFT))
			{
				ShowSelector();
				SlewSelector(-Vector2.right);
			}

			if(BDInputUtils.GetKey(BDInputSettingsFields.RADAR_SLEW_UP))
			{
				ShowSelector();
				SlewSelector(-Vector2.up);
			}
			else if(BDInputUtils.GetKey(BDInputSettingsFields.RADAR_SLEW_DOWN))
			{
				ShowSelector();
				SlewSelector(Vector2.up);
			}

			if(BDInputUtils.GetKeyDown(BDInputSettingsFields.RADAR_LOCK))
			{
				if(locked)
				{
					UnlockTarget();
				}
				else
				{
					if(showSelector)
					{
						TryLockViaSelector();
					}
					ShowSelector();
				}
			}

			if(BDInputUtils.GetKeyDown(BDInputSettingsFields.RADAR_TURRETS))
			{
				if(slaveTurrets)
				{
					UnslaveTurrets();
				}
				else
				{
					SlaveTurrets();
				}
			}

			if(BDInputUtils.GetKeyDown(BDInputSettingsFields.RADAR_RANGE_UP))
			{
				IncreaseRange(); 
			}
			else if(BDInputUtils.GetKeyDown(BDInputSettingsFields.RADAR_RANGE_DN))
			{
				DecreaseRange();
			}
		}

		void TryLockViaSelector()
		{
			bool found = false;
			Vector3 closestPos = Vector3.zero;
			float closestSqrMag = float.MaxValue;
			for(int i = 0; i < contacts.Length; i++)
			{
				float sqrMag = (contacts[i].pingPosition - selectorPos).sqrMagnitude;
				if(sqrMag < Mathf.Pow(20, 2) && sqrMag < closestSqrMag)
				{
					closestPos = contacts[i].predictedPosition;
					found = true;
				}
			}

			if(found)
			{
				TryLockTarget(closestPos);
			}
		}

		void SlewSelector(Vector2 direction)
		{
			float rate = 150;
			selectorPos += direction * rate * Time.deltaTime; 

			if(!omnidirectional)
			{
				if(selectorPos.y > radarScreenSize * 0.975f)
				{
					if(rangeIndex > 0)
					{
						DecreaseRange();
						selectorPos.y = radarScreenSize * 0.75f;
					}
				}
				else if(selectorPos.y < radarScreenSize * 0.025f)
				{
					if(rangeIndex < rIncrements.Length - 1)
					{
						IncreaseRange();
						selectorPos.y = radarScreenSize * 0.25f;
					}
				}
			}

			selectorPos.y = Mathf.Clamp(selectorPos.y, 10, radarScreenSize - 10);
			selectorPos.x = Mathf.Clamp(selectorPos.x, 10, radarScreenSize - 10);
		}

		void ShowSelector()
		{
			if(!showSelector)
			{
				showSelector = true;
				selectorPos = new Vector2(radarScreenSize / 2, radarScreenSize / 2);
			}
		}

		void UpdateModel()
		{
			//model rotation
			if(rotationTransform)
			{
				if(radarEnabled)
				{
					//rotationTransform
					if(!linked)
					{
						Vector3 direction;
						if(locked)
						{
							direction = Quaternion.AngleAxis(canTrackWhileScan ? currentAngle : lockScanAngle, referenceTransform.up) * referenceTransform.forward;
						}
						else
						{
							direction = Quaternion.AngleAxis(currentAngle, referenceTransform.up) * referenceTransform.forward;
						}

						Vector3 localDirection = Vector3.ProjectOnPlane(rotationTransform.parent.InverseTransformDirection(direction), Vector3.up);
						rotationTransform.localRotation = Quaternion.Lerp(rotationTransform.localRotation, Quaternion.LookRotation(localDirection, Vector3.up), 10 * TimeWarp.fixedDeltaTime);
					}
					else
					{
						rotationTransform.localRotation = Quaternion.Lerp(rotationTransform.localRotation, Quaternion.identity, 5 * TimeWarp.fixedDeltaTime);
					}

					//lock turret
					if(lockingTurret)
					{
						if(locked)
						{
							lockingTurret.AimToTarget(lockedTarget.predictedPosition);
						}
						else
						{
							lockingTurret.ReturnTurret();
						}
					}
				}
				else
				{
					rotationTransform.localRotation = Quaternion.Lerp(rotationTransform.localRotation, Quaternion.identity, 5 * TimeWarp.fixedDeltaTime);
					if(lockingTurret)
					{
						lockingTurret.ReturnTurret();
					}
				}
			}
		}


		void Scan()
		{
			if(linked)
			{
				return;
			}

			float angleDelta = scanRotationSpeed*Time.fixedDeltaTime;
			RadarUtils.ScanInDirection(weaponManager, currentAngle, referenceTransform, angleDelta, vessel.transform.position, minSignalThreshold, ref contacts, signalPersistTime, true, rwrType, true);

			if(omnidirectional)
			{
				currentAngle = Mathf.Repeat(currentAngle+angleDelta, 360);
			}
			else
			{
				currentAngle += radialScanDirection*angleDelta;
				if(Mathf.Abs(currentAngle) > directionalFieldOfView/2)
				{
					currentAngle = Mathf.Sign(currentAngle) * directionalFieldOfView/2;
					radialScanDirection = -radialScanDirection;
				}
			}
		}

		public void TryLockTarget(Vector3 position)
		{
			if(!canLock)
			{
				if(linkedRadar && linkedRadar.radarEnabled)
				{
					linkedRadar.TryLockTarget(position);
					lockedTarget = linkedRadar.lockedTarget;
					locked = true;
				}
				return;
			}
			Debug.Log ("Trying to radar lock target");

			Vector3 targetPlanarDirection = Vector3.ProjectOnPlane(position-referenceTransform.position, referenceTransform.up);
			float angle = Vector3.Angle(targetPlanarDirection, referenceTransform.forward);
			if(referenceTransform.InverseTransformPoint(position).x < 0)
			{
				angle = -angle;
			}
			//TargetSignatureData.ResetTSDArray(ref attemptedLocks);
			RadarUtils.ScanInDirection(weaponManager, angle, referenceTransform, lockAttemptFOV, referenceTransform.position, minLockedSignalThreshold, ref attemptedLocks, signalPersistTime, true, rwrType, true);

			for(int i = 0; i < attemptedLocks.Length; i++)
			{
				if(attemptedLocks[i].exists && (attemptedLocks[i].position-position).sqrMagnitude < Mathf.Pow(40,2))
				{
					locked = true;
					lockedTarget = attemptedLocks[i];
					Debug.Log ("- Acquired lock on target.");
					return;
				}
			}

			Debug.Log ("- Failed to lock on target.");
		}


		void BoresightScan()
		{
			currentAngle = Mathf.Lerp(currentAngle, 0, 0.08f);
			RadarUtils.ScanInDirection (new Ray (transform.position, transform.up), boresightFOV, minSignalThreshold * 5, ref attemptedLocks, Time.fixedDeltaTime, true, rwrType, true);

			for(int i = 0; i < attemptedLocks.Length; i++)
			{
				if(attemptedLocks[i].exists && attemptedLocks[i].age < 0.1f)
				{
					locked = true;
					lockedTarget = attemptedLocks[i];
					Debug.Log ("- Acquired lock on target.");
					boresightScan = false;
					return;
				}
			}
		}


		int snapshotTicker = 0;
		void UpdateLock()
		{
			if(!canLock && linked && linkedRadar && linkedRadar.locked)
			{
				lockedTarget = linkedRadar.lockedTarget;
				if(!lockedTarget.exists)
				{
					UnlockTarget();
				}
				return;
			}

			if(!canLock)
			{
				if(lockedTarget.exists)
				{
					UnlockTarget();
					return;
				}

				locked = false;
				return;
			}

			Vector3 targetPlanarDirection = Vector3.ProjectOnPlane(lockedTarget.predictedPosition-referenceTransform.position, referenceTransform.up);
			float lookAngle = Vector3.Angle(targetPlanarDirection, referenceTransform.forward);
			if(referenceTransform.InverseTransformPoint(lockedTarget.predictedPosition).x < 0)
			{
				lookAngle = -lookAngle;
			}

			if(omnidirectional)
			{
				if(lookAngle < 0) lookAngle += 360;
			}


			lockScanAngle = lookAngle + currentAngleLock;
			float angleDelta = lockRotationSpeed*Time.fixedDeltaTime;
			float lockedSignalPersist = lockRotationAngle/lockRotationSpeed;
			//RadarUtils.ScanInDirection(lockScanAngle, referenceTransform, angleDelta, referenceTransform.position, minLockedSignalThreshold, ref attemptedLocks, lockedSignalPersist);
			bool radarSnapshot = (snapshotTicker > 30);
			if(radarSnapshot)
			{
				snapshotTicker = 0;
			}
			else
			{
				snapshotTicker++;
			}
			RadarUtils.ScanInDirection (new Ray (referenceTransform.position, lockedTarget.predictedPosition - referenceTransform.position), lockRotationAngle * 2, minLockedSignalThreshold, ref attemptedLocks, lockedSignalPersist, true, rwrType, radarSnapshot);
			TargetSignatureData prevLock = lockedTarget;
			lockedTarget = TargetSignatureData.noTarget;
			for(int i = 0; i < attemptedLocks.Length; i++)
			{
				if(attemptedLocks[i].exists && (attemptedLocks[i].predictedPosition-prevLock.predictedPosition).sqrMagnitude < Mathf.Pow(20,2) && attemptedLocks[i].age < 2*lockedSignalPersist)
				{
					lockedTarget = attemptedLocks[i];
					break;
				}
			}

			if(!lockedTarget.exists) //if failed to maintain lock, get lock data from linked radar
			{
				if(linked && linkedRadar && linkedRadar.locked && (linkedRadar.lockedTarget.predictedPosition-prevLock.predictedPosition).sqrMagnitude < Mathf.Pow(20,2))
				{
					lockedTarget = linkedRadar.lockedTarget;
					//if(lockedTarget.exists) return;
				}
			}

			//if still failed or out of FOV, unlock.
			if(!lockedTarget.exists || (!omnidirectional && Vector3.Angle(lockedTarget.position-referenceTransform.position, transform.up) > directionalFieldOfView/2))
			{
				UnlockTarget();
				return;
			}

			//unlock if over-jammed
			if(lockedTarget.vesselJammer && lockedTarget.vesselJammer.lockBreakStrength > lockedTarget.signalStrength*lockedTarget.vesselJammer.rcsReductionFactor)
			{
				UnlockTarget();
				return;
			}


			//cycle scan direction
			currentAngleLock += radialScanDirection*angleDelta;
			if(Mathf.Abs(currentAngleLock) > lockRotationAngle/2)
			{
				currentAngleLock = Mathf.Sign(currentAngleLock) * lockRotationAngle/2;
				radialScanDirection = -radialScanDirection;
			}
		}

		void UnlockTarget()
		{
			lockedTarget = TargetSignatureData.noTarget;
			locked = false;
			if (!canTrackWhileScan)
			{
				currentAngle = lockScanAngle;
			}
		}

		void IncreaseRange()
		{
			int origIndex = rangeIndex;
			rangeIndex = Mathf.Clamp(rangeIndex+1, 0, rIncrements.Length-1);
			if(origIndex != rangeIndex)
			{
				pingPositionsDirty = true;
			}
		}

		void DecreaseRange()
		{
			int origIndex = rangeIndex;
			rangeIndex = Mathf.Clamp(rangeIndex-1, 0, rIncrements.Length-1);
			if(origIndex != rangeIndex)
			{
				pingPositionsDirty = true;
			}
		}

		void SlaveTurrets()
		{
			foreach (var mtc in vessel.FindPartModulesImplementing<ModuleTargetingCamera>())
			{
				mtc.slaveTurrets = false;
			}

			foreach (var rad in vessel.FindPartModulesImplementing<ModuleRadar>())
			{
				rad.slaveTurrets = false;
			}

			slaveTurrets = true;
		}

		void UnslaveTurrets()
		{
			foreach (var mtc in vessel.FindPartModulesImplementing<ModuleTargetingCamera>())
			{
				mtc.slaveTurrets = false;
			}

			foreach (var rad in vessel.FindPartModulesImplementing<ModuleRadar>())
			{
				rad.slaveTurrets = false;
			}
		}

		void OnGUI()
		{
			if(drawGUI)
			{
				radarWindowRect = GUI.Window(524314, radarWindowRect, RadarWindow, string.Empty, HighLogic.Skin.window);
				BDGUIUtils.UseMouseEventInRect(radarWindowRect);

				if(linkWindowOpen && canRecieveRadarData)
				{
					linkWindowRect = new Rect(radarWindowRect.x - linkRectWidth, radarWindowRect.y+16, linkRectWidth, 16 + (numberOfAvailableLinks * linkRectEntryHeight));
					LinkRadarWindow();

					BDGUIUtils.UseMouseEventInRect(linkWindowRect);
				}

				if(locked)
				{
					if(weaponManager && lockedTarget.team == BDATargetManager.BoolToTeam(weaponManager.team)) 
					{
						BDGUIUtils.DrawTextureOnWorldPos(lockedTarget.predictedPosition, BDArmorySettings.Instance.crossedGreenSquare, new Vector2(20, 20), 0);
					}
					else
					{
						BDGUIUtils.DrawTextureOnWorldPos(lockedTarget.predictedPosition, BDArmorySettings.Instance.openGreenSquare, new Vector2(20, 20), 0);
					}
				}
				else if(boresightScan)
				{
					BDGUIUtils.DrawTextureOnWorldPos(transform.position + (3500 * transform.up), BDArmorySettings.Instance.dottedLargeGreenCircle, new Vector2(156, 156), 0);
				}


			}
		}

		void RadarWindow(int windowID)
		{
			

			GUI.DragWindow(new Rect(0,0,radarScreenSize+(2*windowBorder), windowBorder+headerHeight));

			if(!referenceTransform) return;

			Rect displayRect = new Rect(windowBorder, 12+windowBorder, radarScreenSize, radarScreenSize);


			//==============================
			GUI.BeginGroup(displayRect);


			Rect radarRect = new Rect(0,0,radarScreenSize,radarScreenSize); //actual rect within group



			if(omnidirectional || linked)
			{
				GUI.DrawTexture(radarRect, omniBgTexture, ScaleMode.StretchToFill, true);
				if(omnidirectional)
				{
					GUI.Label(radarRect, "  N", radarTopStyle);
				}
				GUI.Label(new Rect(radarScreenSize*0.85f, radarScreenSize*0.1f, 60,24), (rIncrements[rangeIndex]/1000).ToString("0")+"km", distanceStyle);

				if(canScan)
				{
					if((!locked || canTrackWhileScan) && !linked)
					{
						GUIUtility.RotateAroundPivot(currentAngle, new Vector2(radarScreenSize / 2, radarScreenSize / 2));
						GUI.DrawTexture(radarRect, scanTexture, ScaleMode.StretchToFill, true);
						GUI.matrix = Matrix4x4.identity;
					}
				}

				//my ship direction icon
				float directionSize = 16;
				float dAngle = Vector3.Angle(Vector3.ProjectOnPlane(vessel.ReferenceTransform.up, referenceTransform.up), referenceTransform.forward);
				if(referenceTransform.InverseTransformVector(vessel.ReferenceTransform.up).x < 0)
				{
					dAngle = -dAngle;
				}
				GUIUtility.RotateAroundPivot(dAngle, radarRect.center);
				GUI.DrawTexture(new Rect(radarRect.center.x - (directionSize / 2), radarRect.center.y - (directionSize / 2), directionSize, directionSize), BDArmorySettings.Instance.directionTriangleIcon, ScaleMode.StretchToFill, true);
				GUI.matrix = Matrix4x4.identity;

				//if linked and directional, draw FOV lines
				if(!omnidirectional)
				{
					float fovAngle = directionalFieldOfView / 2;
					float lineWidth = 2;
					Rect verticalLineRect = new Rect(radarRect.center.x - (lineWidth / 2), 0, lineWidth, radarRect.center.y);
					GUIUtility.RotateAroundPivot(dAngle + fovAngle, radarRect.center);
					BDGUIUtils.DrawRectangle(verticalLineRect, new Color(0, 1, 0, 0.6f));
					GUI.matrix = Matrix4x4.identity;
					GUIUtility.RotateAroundPivot(dAngle - fovAngle, radarRect.center);
					BDGUIUtils.DrawRectangle(verticalLineRect, new Color(0, 1, 0, 0.4f));
					GUI.matrix = Matrix4x4.identity;
				}
			}
			else
			{
				GUI.DrawTexture(radarRect, radialBgTexture, ScaleMode.StretchToFill, true);
				GUI.Label(new Rect(5, 5, 60,24), (rIncrements[rangeIndex]/1000).ToString("0")+"km", distanceStyle);

				if(canScan)
				{
					float indicatorAngle = locked ? lockScanAngle : currentAngle;
					Vector2 scanIndicatorPos = RadarUtils.WorldToRadarRadial(referenceTransform.position + (Quaternion.AngleAxis(indicatorAngle, referenceTransform.up) * referenceTransform.forward), referenceTransform, radarRect, 5000, directionalFieldOfView / 2);
					GUI.DrawTexture(new Rect(scanIndicatorPos.x - 7, scanIndicatorPos.y - 10, 14, 20), BDArmorySettings.Instance.greenDiamondTexture, ScaleMode.StretchToFill, true);
				}
			}

			//selector
			if(showSelector)
			{
				float selectorSize = 18;
				Rect selectorRect = new Rect(selectorPos.x - (selectorSize / 2), selectorPos.y - (selectorSize / 2), selectorSize, selectorSize);
				Rect sLeftRect = new Rect(selectorRect.x, selectorRect.y, selectorSize / 6, selectorRect.height);
				Rect sRightRect = new Rect(selectorRect.x + selectorRect.width - (selectorSize / 6), selectorRect.y, selectorSize / 6, selectorRect.height);
				BDGUIUtils.DrawRectangle(sLeftRect, Color.green);
				BDGUIUtils.DrawRectangle(sRightRect, Color.green);
			}

			//missile data
			if(lastMissile && lastMissile.targetAcquired)
			{
				Rect missileDataRect = new Rect (5, radarRect.height - 65, radarRect.width - 5, 60);
				string missileDataString = lastMissile.GetShortName(); 
				missileDataString += "\nT-"+lastMissile.timeToImpact.ToString("0");

				if (lastMissile.activeRadar && Mathf.Round(Time.time*3)%2==0)
				{
					missileDataString += "\nACTIVE";
				}
				GUI.Label (missileDataRect, missileDataString, distanceStyle);
			}

			//roll indicator
			if(!omnidirectional)
			{
				Vector3 localUp = vessel.ReferenceTransform.InverseTransformDirection(referenceTransform.up);
				localUp = Vector3.ProjectOnPlane(localUp, Vector3.up).normalized;
				float rollAngle = -Misc.SignedAngle(-Vector3.forward, localUp, Vector3.right);
				GUIUtility.RotateAroundPivot(rollAngle, radarRect.center);
				GUI.DrawTexture(radarRect, rollIndicatorTexture, ScaleMode.StretchToFill, true);
				GUI.matrix = Matrix4x4.identity;
			}

			if(!canScan)
			{
				if(linked)
				{
					GUI.Label(radarRect, "\nLINKED: " + linkedRadar.part.partInfo.title, radarTopStyle);
				}
				else
				{
					GUI.Label(radarRect, "NO DATA\n", lockStyle);
				}
			}

			if(locked)
			{
				//LOCKED GUI
				Vector2 pingPosition;
				if(omnidirectional || linked)
				{
					pingPosition = RadarUtils.WorldToRadar(lockedTarget.position, referenceTransform, radarRect, rIncrements[rangeIndex]);
				}
				else
				{
					pingPosition = RadarUtils.WorldToRadarRadial(lockedTarget.position, referenceTransform, radarRect, rIncrements[rangeIndex], directionalFieldOfView/2);
				}

				//BDGUIUtils.DrawRectangle(new Rect(pingPosition.x-(4),pingPosition.y-(4),8, 8), Color.green);
				float vAngle = Vector3.Angle(Vector3.ProjectOnPlane(lockedTarget.velocity, referenceTransform.up), referenceTransform.forward);
				if(referenceTransform.InverseTransformVector(lockedTarget.velocity).x < 0)
				{
					vAngle = -vAngle;
				}
				GUIUtility.RotateAroundPivot(vAngle, pingPosition);
				GUI.DrawTexture(new Rect(pingPosition.x-(lockIconSize/2), pingPosition.y-(lockIconSize/2), lockIconSize, lockIconSize), lockIcon, ScaleMode.StretchToFill, true);
				GUI.matrix = Matrix4x4.identity;
				GUI.Label(new Rect(pingPosition.x+(lockIconSize*0.35f)+2,pingPosition.y,100,24), (lockedTarget.altitude/1000).ToString("0"), distanceStyle);

				GUI.Label(radarRect, "-LOCK-\n", lockStyle);

				if(BDArmorySettings.DRAW_DEBUG_LABELS)
				{
					GUI.Label(new Rect(pingPosition.x+(pingSize.x/2),pingPosition.y,100,24), lockedTarget.signalStrength.ToString("0.0"));
				}

				if (slaveTurrets)
				{
					GUI.Label (radarRect, "TURRETS\n\n", lockStyle);
				}
			}


			if(!locked || canTrackWhileScan || (linked&&linkedRadar&&linkedRadar.canTrackWhileScan))
			{
				//SCANNING GUI
				if (boresightScan)
				{
					GUI.DrawTexture (new Rect (30, 30, radarRect.width - 60, radarRect.height - 60), BDArmorySettings.Instance.dottedLargeGreenCircle, ScaleMode.StretchToFill, true);
					GUI.DrawTexture (new Rect (100, 100, radarRect.width - 200, radarRect.height - 200), BDArmorySettings.Instance.largeGreenCircleTexture, ScaleMode.StretchToFill, true);
					GUI.Label(radarRect, "BORESIGHT\n", lockStyle);
				} 
				else
				{
					if(!linked)
					{
						DrawScannedContacts(ref contacts, radarRect);
					}

					if(linked && linkedRadar)
					{
						DrawScannedContacts(ref linkedRadar.contacts, radarRect);
					}
				}
			}
			GUI.EndGroup();
			//=========================================


			float buttonWidth = 70;
			float gap = 2;
			float buttonHeight = (controlsHeight / 2) - (2*gap);
			float controlsStartY = headerHeight + radarScreenSize + windowBorder + windowBorder;
			float controlsStartY2 = controlsStartY + buttonHeight + gap;

			Rect rangeUpRect = new Rect(windowBorder, controlsStartY, buttonWidth, buttonHeight);
			if(GUI.Button(rangeUpRect, "Range +", HighLogic.Skin.button))
			{
				IncreaseRange();
			}
			Rect rangeDnRect = new Rect(rangeUpRect.x, controlsStartY2, rangeUpRect.width, rangeUpRect.height);
			if(GUI.Button(rangeDnRect, "Range -", HighLogic.Skin.button))
			{
				DecreaseRange();
			}

			Rect dataLinkRect = new Rect(rangeUpRect.x + gap + rangeUpRect.width, rangeUpRect.y, buttonWidth, buttonHeight);
			if(canRecieveRadarData)
			{
				if(GUI.Button(dataLinkRect, linked ? "Unlink" : "Data Link", linkWindowOpen ? HighLogic.Skin.box : HighLogic.Skin.button))
				{
					if(linkWindowOpen)
					{
						CloseLinkRadarWindow();
					}
					else
					{
						if(linked)
						{
							UnlinkRadar();
						}
						else
						{
							OpenLinkRadarWindow();
						}
					}
				}
			}
			else
			{
				Color oCol = GUI.color;
				GUI.color = new Color(1, 1, 1, 0.35f);
				GUI.Box(dataLinkRect, "Link N/A", HighLogic.Skin.button);
				GUI.color = oCol;
			}

			Rect unlockRect = new Rect(windowBorder + gap + buttonWidth + gap + buttonWidth, controlsStartY, buttonWidth, buttonHeight);
			if (locked)
			{
				if (GUI.Button (unlockRect, "Unlock", HighLogic.Skin.button))
				{
					UnlockTarget ();
				}
			}
			else if (!omnidirectional) //SCAN MODE SELECTOR
			{
				string boresightToggle = boresightScan ? "Scan" : "Boresight";
				if (GUI.Button (unlockRect, boresightToggle, HighLogic.Skin.button))
				{
					boresightScan = !boresightScan;
				}
			}
			Rect slaveRect = new Rect(unlockRect.x + gap + unlockRect.width, unlockRect.y, buttonWidth * 1.5f, buttonHeight);
			//slave button
			if (GUI.Button (slaveRect, slaveTurrets ? "Unslave Turrets" : "Slave Turrets", slaveTurrets ? HighLogic.Skin.box : HighLogic.Skin.button))
			{
				if (slaveTurrets)
				{
					UnslaveTurrets ();
				} else
				{
					SlaveTurrets ();
				}
			}




			Rect offRect = new Rect(slaveRect.x + gap + slaveRect.width, controlsStartY, 0, (2*buttonHeight)+gap);
			offRect.width = radarWindowRect.width - offRect.x - windowBorder;
			if(GUI.Button(offRect, "O\nF\nF", HighLogic.Skin.button))
			{
				DisableRadar();
			}
		}

		void LinkRadarWindow()
		{
			
			GUI.Box(linkWindowRect, string.Empty, HighLogic.Skin.window);

			numberOfAvailableLinks = 0;

			GUI.BeginGroup(linkWindowRect);

			if(GUI.Button(new Rect(8, 8, 100, linkRectEntryHeight), "Refresh", HighLogic.Skin.button))
			{
				RefreshAvailableLinks();
			}
			numberOfAvailableLinks += 1.25f;

			foreach(var mr in availableRadarLinks)
			{
				if(mr && mr.vessel.loaded)
				{
					if(GUI.Button(new Rect(8, 8+(linkRectEntryHeight*numberOfAvailableLinks), linkRectWidth-16, linkRectEntryHeight), mr.vessel.vesselName, HighLogic.Skin.button))
					{
						LinkToRadar(mr);
					}
					numberOfAvailableLinks++;
				}
			}


			GUI.EndGroup();
		}

		void UnlinkRadar()
		{
			linked = false;
			linkedRadar = null;
			linkedVesselID = string.Empty;
			if(locked)
			{
				UnlockTarget();
			}
		}

		void OpenLinkRadarWindow()
		{
			RefreshAvailableLinks();
			linkWindowOpen = true;
		}

		void CloseLinkRadarWindow()
		{
			linkWindowOpen = false;
		}

		void RefreshAvailableLinks()
		{
			if(!weaponManager) return;

			availableRadarLinks = new List<ModuleRadar>();
			foreach(var v in FlightGlobals.Vessels)
			{
				if(v.loaded && v!=vessel)
				{
					BDArmorySettings.BDATeams team = BDArmorySettings.BDATeams.None;
					foreach(var mf in v.FindPartModulesImplementing<MissileFire>())
					{
						team = BDATargetManager.BoolToTeam(mf.team);
						break;
					}
					if(team == BDATargetManager.BoolToTeam(weaponManager.team))
					{
						foreach(var mr in v.FindPartModulesImplementing<ModuleRadar>())
						{
							if(mr.radarEnabled && mr.canScan)
							{
								availableRadarLinks.Add(mr);
								break;
							}
						}
					}
				}
			}
		}

		void LinkToRadar(ModuleRadar mr)
		{
			if(!mr)
			{
				return;
			}

			linkedRadar = mr;
			linkedVesselID = mr.vessel.id.ToString();
			linked = true;
			if(mr.locked)
			{
				locked = true;
				lockedTarget = mr.lockedTarget;
			}
			CloseLinkRadarWindow();
		}
		bool pingPositionsDirty = true;
		void DrawScannedContacts(ref TargetSignatureData[] scannedContacts, Rect radarRect)
		{
			float myAlt = (float)vessel.altitude;
			for (int i = 0; i < scannedContacts.Length; i++)
			{
				if(scannedContacts[i].exists)// && scannedContacts[i].signalStrength > minSignalThreshold)
				{
					//ignore old target data
					if(scannedContacts[i].age > signalPersistTime)
					{
						scannedContacts[i].exists = false;
						continue;
					}

					//ignore targets outside of directional field of view
					if(!omnidirectional && !linked && Vector3.Angle(scannedContacts[i].position - transform.position, transform.up) > directionalFieldOfView)
					{
						scannedContacts[i].exists = false;
						continue;
					}

					//ignore locked target if tracking while scanning
					if((canTrackWhileScan||(linked&&linkedRadar&&linkedRadar.canTrackWhileScan)) && locked && (scannedContacts[i].predictedPosition - lockedTarget.predictedPosition).sqrMagnitude < 100)
					{
						scannedContacts[i].exists = false;
						continue;
					}

					float minusAlpha = (Mathf.Clamp01((Time.time - scannedContacts[i].timeAcquired) / signalPersistTime) * 2) - 1;

					//dont draw self if reading remote data
					if(linked && (scannedContacts[i].predictedPosition - transform.position).sqrMagnitude < Mathf.Pow(50, 2))
					{
						minusAlpha = 1;
						continue;
					}

					//jamming
					bool jammed = false;
					if(scannedContacts[i].vesselJammer && scannedContacts[i].vesselJammer.jammerStrength > scannedContacts[i].signalStrength)
					{
						jammed = true;
					}

					Vector2 pingPosition;
					if(pingPositionsDirty || scannedContacts[i].pingPosition == Vector2.zero)
					{
						if(omnidirectional || linked)
						{
							pingPosition = RadarUtils.WorldToRadar(scannedContacts[i].position, referenceTransform, radarRect, rIncrements[rangeIndex]);
						}
						else
						{
							pingPosition = RadarUtils.WorldToRadarRadial(scannedContacts[i].position, referenceTransform, radarRect, rIncrements[rangeIndex], directionalFieldOfView / 2);
						}

						scannedContacts[i].pingPosition = pingPosition;
					}
					else
					{
						pingPosition = scannedContacts[i].pingPosition;
					}

					Rect pingRect;
					//draw missiles and debris as dots
					if((scannedContacts[i].targetInfo && scannedContacts[i].targetInfo.isMissile) || scannedContacts[i].team == BDArmorySettings.BDATeams.None)
					{
						float mDotSize = 6;
						pingRect = new Rect(pingPosition.x - (mDotSize / 2), pingPosition.y - (mDotSize / 2), mDotSize, mDotSize);
						Color origGUIColor = GUI.color;
						GUI.color = Color.white - new Color(0, 0, 0, minusAlpha);
						GUI.DrawTexture(pingRect, BDArmorySettings.Instance.greenDotTexture, ScaleMode.StretchToFill, true);
						GUI.color = origGUIColor;
					}
					//draw contacts with direction indicator
					else if(!jammed && (showDirectionWhileScan || (linked&&linkedRadar&&linkedRadar.showDirectionWhileScan)) && scannedContacts[i].velocity.sqrMagnitude > 100)
					{
						pingRect = new Rect(pingPosition.x - (lockIconSize / 2), pingPosition.y - (lockIconSize / 2), lockIconSize, lockIconSize);
						float vAngle = Vector3.Angle(Vector3.ProjectOnPlane(scannedContacts[i].velocity, referenceTransform.up), referenceTransform.forward);
						if(referenceTransform.InverseTransformVector(scannedContacts[i].velocity).x < 0)
						{
							vAngle = -vAngle;
						}
						GUIUtility.RotateAroundPivot(vAngle, pingPosition);
						Color origGUIColor = GUI.color;
						GUI.color = Color.white - new Color(0, 0, 0, minusAlpha);
						if(weaponManager && scannedContacts[i].team == BDATargetManager.BoolToTeam(weaponManager.team))
						{
							GUI.DrawTexture(pingRect, friendlyContactIcon, ScaleMode.StretchToFill, true);
						}
						else
						{
							GUI.DrawTexture(pingRect, radarContactIcon, ScaleMode.StretchToFill, true);
						}

						GUI.matrix = Matrix4x4.identity;
						GUI.Label(new Rect(pingPosition.x + (lockIconSize * 0.35f) + 2, pingPosition.y, 100, 24), (scannedContacts[i].altitude / 1000).ToString("0"), distanceStyle);
						GUI.color = origGUIColor;
					}
					else //draw contacts as rectangles
					{
						int drawCount = jammed ? 4 : 1;
						pingRect = new Rect(pingPosition.x - (pingSize.x / 2), pingPosition.y - (pingSize.y / 2), pingSize.x, pingSize.y);
						for(int d = 0; d < drawCount; d++)
						{
							Rect jammedRect = new Rect(pingRect);
							Vector3 contactPosition = scannedContacts[i].position;
							if(jammed)
							{
								//jamming
								Vector3 jammedPosition = transform.position + ((scannedContacts[i].position - transform.position).normalized * UnityEngine.Random.Range(100, rIncrements[rangeIndex]));
								float bearingVariation = Mathf.Clamp(Mathf.Pow(32000, 2) / (scannedContacts[i].position - transform.position).sqrMagnitude, 0, 80);
								jammedPosition = transform.position + (Quaternion.AngleAxis(UnityEngine.Random.Range(-bearingVariation, bearingVariation), referenceTransform.up) * (jammedPosition - transform.position));
								if(omnidirectional || linked)
								{
									pingPosition = RadarUtils.WorldToRadar(jammedPosition, referenceTransform, radarRect, rIncrements[rangeIndex]);
								}
								else
								{
									pingPosition = RadarUtils.WorldToRadarRadial(jammedPosition, referenceTransform, radarRect, rIncrements[rangeIndex], directionalFieldOfView / 2);
								}

								jammedRect = new Rect(pingPosition.x - (pingSize.x / 2), pingPosition.y - (pingSize.y / 2) - (pingSize.y / 3), pingSize.x, pingSize.y / 3);
								contactPosition = jammedPosition;
							}

							Color iconColor = Color.green;
							float contactAlt = scannedContacts[i].altitude;
							if(!omnidirectional && !jammed)
							{
								if(contactAlt - myAlt > 1000)
								{
									iconColor = new Color(0, 0.6f, 1f, 1);
								}
								else if(contactAlt - myAlt < -1000)
								{
									iconColor = new Color(1f, 0.68f, 0, 1);
								}
							}

							if(omnidirectional)
							{
								Vector3 localPos = referenceTransform.InverseTransformPoint(contactPosition);
								localPos.y = 0;
								float angleToContact = Vector3.Angle(localPos, Vector3.forward);
								if(localPos.x < 0) angleToContact = -angleToContact;
								GUIUtility.RotateAroundPivot(angleToContact, pingPosition);
							}

							BDGUIUtils.DrawRectangle(jammedRect, iconColor - new Color(0, 0, 0, minusAlpha));

							GUI.matrix = Matrix4x4.identity;
						}
					}


					if(GUI.RepeatButton(pingRect, GUIContent.none, GUIStyle.none))
					{
						TryLockTarget(scannedContacts[i].predictedPosition);
						if(Event.current.isMouse && Event.current.type == EventType.mouseDown)
						{
							Event.current.Use();
						}
					}

					if(BDArmorySettings.DRAW_DEBUG_LABELS)
					{
						GUI.Label(new Rect(pingPosition.x + (pingSize.x / 2), pingPosition.y, 100, 24), scannedContacts[i].signalStrength.ToString("0.0"));
					}
				}
			}

			pingPositionsDirty = false;
		}




	}
}

