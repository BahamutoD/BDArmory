using UnityEngine;
using System.Collections;

namespace BahaTurret
{
	public class ModuleRadar : PartModule
	{
		[KSPField]
		public bool canLock = true;
		bool locked = false;
		
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

		//contacts
		TargetSignatureData[] contacts;
		TargetSignatureData[] attemptedLocks;
		public TargetSignatureData lockedTarget;

		//GUI
		public static Rect radarWindowRect;
		public static bool radarRectInitialized = false;
		float radarScreenSize = 360;
		float windowBorder = 10;
		float headerHeight = 12;
		float controlsHeight = 32;
		Vector2 pingSize = new Vector2(16,8);
		float signalPersistTime;
		Texture2D omniBgTexture = GameDatabase.Instance.GetTexture(BDArmorySettings.textureDir + "omniRadarTexture", false);
		Texture2D radialBgTexture = GameDatabase.Instance.GetTexture(BDArmorySettings.textureDir + "radialRadarTexture", false);
		Texture2D scanTexture = GameDatabase.Instance.GetTexture(BDArmorySettings.textureDir + "omniRadarScanTexture", false);
		Texture2D lockIcon = GameDatabase.Instance.GetTexture(BDArmorySettings.textureDir + "lockedRadarIcon", false);
		float lockIconSize = 24;

		//scanning
		[KSPField(isPersistant = true)]
		public float currentAngle = 0;
		Transform referenceTransform;
		float radialScanDirection = 1;

		//locking
		[KSPField]
		public float lockAttemptFOV = 2;
		float lockScanAngle = 0;


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

				attemptedLocks = new TargetSignatureData[5];
				TargetSignatureData.ResetTSDArray(ref attemptedLocks);


				referenceTransform = (new GameObject()).transform;
				referenceTransform.parent = transform;
				referenceTransform.localPosition = Vector3.zero;
			}
		}
		
		// Update is called once per frame
		void Update ()
		{
			if(HighLogic.LoadedSceneIsFlight && FlightGlobals.ready)
			{
				if(omnidirectional)
				{
					referenceTransform.position = vessel.transform.position;
					referenceTransform.rotation = Quaternion.LookRotation(VectorUtils.GetNorthVector(transform.position, vessel.mainBody), VectorUtils.GetUpDirection(transform.position));
				}
				else
				{
					referenceTransform.position = vessel.transform.position;
					referenceTransform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.up, VectorUtils.GetUpDirection(referenceTransform.position)), VectorUtils.GetUpDirection(referenceTransform.position));
				}

			}
		}

		void FixedUpdate()
		{
			if(HighLogic.LoadedSceneIsFlight && FlightGlobals.ready)
			{
				if(radarEnabled)
				{
					if(locked)
					{
						UpdateLock();
					}
					else
					{
						Scan ();
					}
				}
			}
		}


		void Scan()
		{
			//model rotation
			if(rotationTransform)
			{
				if(omnidirectional)
				{
					rotationTransform.localRotation *= Quaternion.AngleAxis(scanRotationSpeed*Time.deltaTime, Vector3.up);
				}
			}

			float angleDelta = scanRotationSpeed*Time.fixedDeltaTime;
			RadarUtils.ScanInDirection(currentAngle, referenceTransform, angleDelta, vessel.transform.position, 0.01f, ref contacts, signalPersistTime);

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

		void TryLockTarget(Vector3 position)
		{
			if(!canLock)
			{
				return;
			}
			Debug.Log ("Trying to radar lock target");

			Vector3 targetPlanarDirection = Vector3.ProjectOnPlane(position-referenceTransform.position, referenceTransform.up);
			float angle = Vector3.Angle(targetPlanarDirection, referenceTransform.forward);
			if(referenceTransform.InverseTransformPoint(position).x < 0)
			{
				angle = -angle;
			}
			TargetSignatureData.ResetTSDArray(ref attemptedLocks);
			RadarUtils.ScanInDirection(angle, referenceTransform, lockAttemptFOV, referenceTransform.position, minSignalThreshold, ref attemptedLocks, signalPersistTime);

			for(int i = 0; i < attemptedLocks.Length; i++)
			{
				if(attemptedLocks[i].exists && (attemptedLocks[i].position-position).sqrMagnitude < Mathf.Pow(40,2))
				{
					locked = true;
					lockedTarget = attemptedLocks[i];
					currentAngle = 0;
					Debug.Log ("- Acquired lock on target.");
					return;
				}
			}

			Debug.Log ("- Failed to lock on target.");
		}



		void UpdateLock()
		{
			Vector3 targetPlanarDirection = Vector3.ProjectOnPlane(lockedTarget.position-referenceTransform.position, referenceTransform.up);
			float lookAngle = Vector3.Angle(targetPlanarDirection, referenceTransform.forward);
			if(referenceTransform.InverseTransformPoint(lockedTarget.position).x < 0)
			{
				lookAngle = -lookAngle;
			}

			if(omnidirectional)
			{
				if(lookAngle < 0) lookAngle += 360;
			}
			else
			{
				//lookAngle = Mathf.Clamp(lookAngle, -directionalFieldOfView/2, directionalFieldOfView/2);
				if(Vector3.Angle(lockedTarget.position-referenceTransform.position, transform.up) > directionalFieldOfView/2)
				{
					UnlockTarget();
					return;
				}
			}

			lockScanAngle = lookAngle + currentAngle;
			float angleDelta = lockRotationSpeed*Time.fixedDeltaTime;
			float lockedSignalPersist = lockRotationAngle/lockRotationSpeed;
			RadarUtils.ScanInDirection(lockScanAngle, referenceTransform, angleDelta, referenceTransform.position, minSignalThreshold, ref attemptedLocks, lockedSignalPersist);
			TargetSignatureData prevLock = lockedTarget;
			lockedTarget = TargetSignatureData.noTarget;
			for(int i = 0; i < attemptedLocks.Length; i++)
			{
				if(attemptedLocks[i].exists && (attemptedLocks[i].position-prevLock.position).sqrMagnitude < Mathf.Pow(20,2))
				{
					lockedTarget = attemptedLocks[i];
					break;
				}
			}
			if(!lockedTarget.exists)
			{
				locked = false;
				return;
			}

			//cycle scan direction
			currentAngle += radialScanDirection*angleDelta;
			if(Mathf.Abs(currentAngle) > lockRotationAngle/2)
			{
				currentAngle = Mathf.Sign(currentAngle) * lockRotationAngle/2;
				radialScanDirection = -radialScanDirection;
			}

		}

		void UnlockTarget()
		{
			lockedTarget = TargetSignatureData.noTarget;
			locked = false;
		}

		void IncreaseRange()
		{
			rangeIndex = Mathf.Clamp(rangeIndex+1, 0, rIncrements.Length-1);
		}

		void DecreaseRange()
		{
			rangeIndex = Mathf.Clamp(rangeIndex-1, 0, rIncrements.Length-1);
		}


		void OnGUI()
		{
			if(radarEnabled && vessel.isActiveVessel && FlightGlobals.ready)
			{
				radarWindowRect = GUI.Window(524314, radarWindowRect, RadarWindow, string.Empty, HighLogic.Skin.window);

				if(locked)
				{
					BDGUIUtils.DrawTextureOnWorldPos(lockedTarget.predictedPosition, BDArmorySettings.Instance.openGreenSquare, new Vector2(20,20), 0);
				}
			}
		}

		void RadarWindow(int windowID)
		{
			GUI.DragWindow(new Rect(0,0,radarScreenSize+(2*windowBorder), windowBorder+headerHeight));

			Rect displayRect = new Rect(windowBorder, 12+windowBorder, radarScreenSize, radarScreenSize);

			GUI.BeginGroup(displayRect);

			GUIStyle distanceStyle = new GUIStyle();
			distanceStyle.normal.textColor = new Color(0,1,0,0.75f);
			distanceStyle.alignment = TextAnchor.UpperLeft;

			GUIStyle lockStyle = new GUIStyle();
			lockStyle.normal.textColor = new Color(0,1,0,1);
			lockStyle.alignment = TextAnchor.LowerCenter;
			lockStyle.fontSize = 16;

			Rect radarRect = new Rect(0,0,radarScreenSize,radarScreenSize); //actual rect within group

			if(omnidirectional)
			{
				GUI.DrawTexture(radarRect, omniBgTexture, ScaleMode.StretchToFill, true);

				GUI.Label(new Rect(radarScreenSize*0.85f, radarScreenSize*0.1f, 60,24), (rIncrements[rangeIndex]/1000).ToString("0")+"km", distanceStyle);

				if(!locked)
				{
					GUIUtility.RotateAroundPivot(currentAngle, new Vector2(radarScreenSize/2, radarScreenSize/2));
					GUI.DrawTexture(radarRect, scanTexture, ScaleMode.StretchToFill, true);
					GUI.matrix = Matrix4x4.identity;
				}
			}
			else
			{
				GUI.DrawTexture(radarRect, radialBgTexture, ScaleMode.StretchToFill, true);
				GUI.Label(new Rect(5, 5, 60,24), (rIncrements[rangeIndex]/1000).ToString("0")+"km", distanceStyle);

				float indicatorAngle = locked ? lockScanAngle : currentAngle;
				Vector2 scanIndicatorPos = RadarUtils.WorldToRadarRadial(referenceTransform.position+(Quaternion.AngleAxis(indicatorAngle,referenceTransform.up)*referenceTransform.forward), referenceTransform, radarRect, 5000, directionalFieldOfView/2);
				BDGUIUtils.DrawRectangle(new Rect(scanIndicatorPos.x-5, scanIndicatorPos.y-12, 10, 10), new Color(0,1,0,0.65f));
			}


			if(locked)
			{
				//LOCKED GUI
				Vector2 pingPosition;
				if(omnidirectional)
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
				GUI.Label(new Rect(pingPosition.x+(lockIconSize/2)+2,pingPosition.y,100,24), (lockedTarget.altitude/1000).ToString("0"), distanceStyle);

				GUI.Label(radarRect, "-LOCK-\n", lockStyle);

				if(BDArmorySettings.DRAW_DEBUG_LABELS)
				{
					GUI.Label(new Rect(pingPosition.x+(pingSize.x/2),pingPosition.y,100,24), lockedTarget.signalStrength.ToString("0.0"));
				}

			}
			else
			{
				//SCANNING GUI
				float myAlt = (float)vessel.altitude;
				for(int i = 0; i < contacts.Length; i++)
				{
					if(contacts[i].exists && contacts[i].signalStrength > minSignalThreshold)
					{
						if(Time.time-contacts[i].timeAcquired > signalPersistTime)
						{
							contacts[i].exists = false;
							continue;
						}

						if(!omnidirectional && Vector3.Angle(contacts[i].position-transform.position, transform.up) > directionalFieldOfView)
						{
							contacts[i].exists = false;
							continue;
						}

						float minusAlpha = (Mathf.Clamp01((Time.time-contacts[i].timeAcquired)/signalPersistTime)*2)-1;

						Vector2 pingPosition;
						if(omnidirectional)
						{
							pingPosition = RadarUtils.WorldToRadar(contacts[i].position, referenceTransform, radarRect, rIncrements[rangeIndex]);
						}
						else
						{
							pingPosition = RadarUtils.WorldToRadarRadial(contacts[i].position, referenceTransform, radarRect, rIncrements[rangeIndex], directionalFieldOfView/2);
						}
						Rect pingRect = new Rect(pingPosition.x-(pingSize.x/2),pingPosition.y-(pingSize.y/2),pingSize.x, pingSize.y);

						Color iconColor = Color.green;
						float contactAlt = contacts[i].altitude;
						if(contactAlt-myAlt > 1000)
						{
							iconColor = new Color(0,0.6f,1f,1);
						}
						else if(contactAlt-myAlt < -1000)
						{
							iconColor = new Color(1f,0.68f,0,1);
						}

						BDGUIUtils.DrawRectangle(pingRect, iconColor-new Color(0,0,0,minusAlpha));
						if(GUI.RepeatButton(pingRect, GUIContent.none, GUIStyle.none))
						{
							TryLockTarget(contacts[i].position + (contacts[i].velocity*(Time.time-contacts[i].timeAcquired)));
						}

						if(BDArmorySettings.DRAW_DEBUG_LABELS)
						{
							GUI.Label(new Rect(pingPosition.x+(pingSize.x/2),pingPosition.y,100,24), contacts[i].signalStrength.ToString("0.0"));
						}
					}
				}
			}
			GUI.EndGroup();



			float controlsStartY = headerHeight + radarScreenSize + windowBorder + windowBorder;
			float buttonWidth = 70;
			if(GUI.Button(new Rect(windowBorder, controlsStartY, buttonWidth, 24), "Range +", HighLogic.Skin.button))
			{
				IncreaseRange();
			}
			if(GUI.Button(new Rect(windowBorder + 2 + buttonWidth, controlsStartY, buttonWidth, 24), "Range -", HighLogic.Skin.button))
			{
				DecreaseRange();
			}
			if(locked)
			{
				if(GUI.Button(new Rect(windowBorder + 2 + buttonWidth + 2 + buttonWidth, controlsStartY, 100, 24), "Unlock", HighLogic.Skin.button))
				{
					UnlockTarget();
				}
			}
		}




	}
}

