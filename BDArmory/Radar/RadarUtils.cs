using BDArmory.CounterMeasure;
using BDArmory.Misc;
using BDArmory.Parts;
using BDArmory.Shaders;
using BDArmory.UI;
using System.Collections.Generic;
using UnityEngine;

namespace BDArmory.Radar
{
	public static class RadarUtils
	{
        private static bool rcsSetupCompleted = false;
        private static int radarResolution = 128;

        private static RenderTexture rcsRenderingFrontal;
        private static RenderTexture rcsRenderingLateral;
        private static RenderTexture rcsRenderingVentral;
        private static Camera radarCam;

        private static Texture2D drawTextureFrontal;
        public static Texture2D GetTextureFrontal { get { return drawTextureFrontal; } }
        private static Texture2D drawTextureLateral;
        public static Texture2D GetTextureLateral { get { return drawTextureLateral; } }
        private static Texture2D drawTextureVentral;
        public static Texture2D GetTextureVentral { get { return drawTextureVentral; } }
        public static float rcsFrontal;             // public so that editor analysis window has access to the details
        public static float rcsLateral;             // dito
        public static float rcsVentral;             // dito
        public static float rcsTotal;               // dito

        private const float RCS_NORMALIZATION_FACTOR = 16.0f;


        /**
         * Get a vessel radar siganture, including all modifiers (ECM, stealth, ...)
         */
        public static float GetVesselRadarSignature(Vessel v)
        {
            //1. baseSig = GetVesselRadarCrossSection
            //2. modifiedSig = GetVesselModifiedSignature(baseSig)    //ECM-jammers with rcs reduction effect; other rcs reductions (stealth?)

            return 0;
        }


        /**
         * Internal method: get a vessel base radar signature
         */
        private static float GetVesselRadarCrossSection(Vessel v)
        {
            //read vesseltargetinfo, or render against radar cameras    

            return 0;
        }


        /**
         * Internal method: get a vessels siganture modifiers (ecm, stealth, ...)
         */
        private static float GetVesselModifiedSignature(Vessel v, float baseSig)
        {
            //read vessel ecminfo and multiply
            //get vessel stealth modifier (NOT IMPLEMENTED YET)

            return 0;
        }


        /**
         * Internal method: do the actual radar snapshot rendering from 3 sides
         * and store it in a vesseltargetinfo attached to the vessel
         * 
         * Note: Transform t is passed separatedly (instead of using v.transform), as the method need to be called from the editor
         *       and there we dont have a VESSEL, only a SHIPCONSTRUCT, so the editor passes the transform separately.
         *       
         * inEditorZoom: when true, we try to make the rendered vessel fill the rendertexture completely, for a better view.
         *               This does skew the computed cross section, so it is only for a good visual in editor!
         */
        public static float RenderVesselRadarSnapshot(Vessel v, Transform t, bool inEditorZoom = false)
        {
            const float radarDistance = 1000f;
            const float radarFOV = 2.0f;
            float distanceToShip;


            Bounds vesselbounds = CalcVesselBounds(v, t);
            if (BDArmorySettings.DRAW_DEBUG_LABELS)
            {
                Debug.Log("[BDArmory]: Rendering radar snapshot of vessel");
                Debug.Log("[BDArmory]: - SHIPBOUNDS: " + vesselbounds.ToString());
                Debug.Log("[BDArmory]: - SHIPSIZE: " + vesselbounds.size + ", MAGNITUDE: " + vesselbounds.size.magnitude);
            }


            // pass1: frontal
            radarCam.transform.position = vesselbounds.center + t.up * radarDistance;
            radarCam.transform.LookAt(vesselbounds.center);

                // setup camera FOV (once only needed)
                distanceToShip = Vector3.Distance(radarCam.transform.position, vesselbounds.center);
                radarCam.nearClipPlane = distanceToShip - 200;
                radarCam.farClipPlane = distanceToShip + 200;
                if (inEditorZoom)
                    radarCam.fieldOfView = Mathf.Atan(vesselbounds.size.magnitude / distanceToShip) * 180 / Mathf.PI;
                else
                    radarCam.fieldOfView = radarFOV;

            radarCam.targetTexture = rcsRenderingFrontal;
            RenderTexture.active = rcsRenderingFrontal;
            Shader.SetGlobalVector("_LIGHTDIR", -t.up);
            radarCam.RenderWithShader(BDAShaderLoader.RCSShader, string.Empty);
            drawTextureFrontal.ReadPixels(new Rect(0, 0, radarResolution, radarResolution), 0, 0);
            drawTextureFrontal.Apply();

            // pass2: lateral
            radarCam.transform.position = vesselbounds.center + t.right * radarDistance;
            radarCam.transform.LookAt(vesselbounds.center);
                //camera FOV already setup
            radarCam.targetTexture = rcsRenderingLateral;
            RenderTexture.active = rcsRenderingLateral;
            Shader.SetGlobalVector("_LIGHTDIR", -t.right);
            radarCam.RenderWithShader(BDAShaderLoader.RCSShader, string.Empty);
            drawTextureLateral.ReadPixels(new Rect(0, 0, radarResolution, radarResolution), 0, 0);
            drawTextureLateral.Apply();

            // pass3: Ventral
            radarCam.transform.position = vesselbounds.center + t.forward * radarDistance;
            radarCam.transform.LookAt(vesselbounds.center);
                //camera FOV already setup
            radarCam.targetTexture = rcsRenderingVentral;
            RenderTexture.active = rcsRenderingVentral;
            Shader.SetGlobalVector("_LIGHTDIR", -t.forward);
            radarCam.RenderWithShader(BDAShaderLoader.RCSShader, string.Empty);
            drawTextureVentral.ReadPixels(new Rect(0, 0, radarResolution, radarResolution), 0, 0);
            drawTextureVentral.Apply();

            // Count pixel colors to determine radar returns (only for normal non-zoomed rendering!)
            if (!inEditorZoom)
            {
                rcsFrontal = 0;
                rcsLateral = 0;
                rcsVentral = 0;
                for (int x = 0; x < radarResolution; x++)
                {
                    for (int y = 0; y < radarResolution; y++)
                    {
                        rcsFrontal += drawTextureFrontal.GetPixel(x, y).maxColorComponent;
                        rcsLateral += drawTextureLateral.GetPixel(x, y).maxColorComponent;
                        rcsVentral += drawTextureVentral.GetPixel(x, y).maxColorComponent;
                    }
                }

                // normalize rcs value, so that the structural 1x1 panel facing the radar exactly gives a return of 1 m^2:
                rcsFrontal /= RCS_NORMALIZATION_FACTOR;
                rcsLateral /= RCS_NORMALIZATION_FACTOR;
                rcsVentral /= RCS_NORMALIZATION_FACTOR;
                rcsTotal = (rcsFrontal + rcsLateral + rcsVentral) / 3f;
            }

            return rcsTotal;
        }


        /**
         * Internal method: get a vessel's bounds
         * Method implemention adapted from kronal vessel viewer
         */
        private static Bounds CalcVesselBounds(Vessel v, Transform t)
        {
            Bounds result = new Bounds(t.position, Vector3.zero);

            List<Part>.Enumerator vp = v.Parts.GetEnumerator();
            while (vp.MoveNext())
            {
                if (vp.Current.collider && !vp.Current.Modules.Contains("LaunchClamp"))
                {
                    result.Encapsulate(vp.Current.collider.bounds);
                }
            }
            vp.Dispose();

            return result;
        }


        /**
         * Internal method: get a vessel's size (based on it's bounds)
         * Method implemention adapted from kronal vessel viewer
         */
        private static Vector3 GetVesselSize(Vessel v, Transform t)
        {
            return CalcVesselBounds(v, t).size;
        }



        public static void SetupResources()
		{
            if (!rcsSetupCompleted)
            {
                //set up rendertargets and textures
                rcsRenderingFrontal = new RenderTexture(radarResolution, radarResolution, 16);
                rcsRenderingLateral = new RenderTexture(radarResolution, radarResolution, 16);
                rcsRenderingVentral = new RenderTexture(radarResolution, radarResolution, 16);
                drawTextureFrontal = new Texture2D(radarResolution, radarResolution, TextureFormat.RGB24, false);
                drawTextureLateral = new Texture2D(radarResolution, radarResolution, TextureFormat.RGB24, false);
                drawTextureVentral = new Texture2D(radarResolution, radarResolution, TextureFormat.RGB24, false);

                //set up camera
                radarCam = (new GameObject("RadarCamera")).AddComponent<Camera>();
                radarCam.enabled = false;
                radarCam.clearFlags = CameraClearFlags.SolidColor;
                radarCam.backgroundColor = Color.black;
                radarCam.cullingMask = 1 << 0;   // only layer 0 active, see: http://wiki.kerbalspaceprogram.com/wiki/API:Layers

                rcsSetupCompleted = true;
            }
        }


        public static void CleanupResources()
        {
            if (rcsSetupCompleted)
            {
                RenderTexture.Destroy(rcsRenderingFrontal);
                RenderTexture.Destroy(rcsRenderingLateral);
                RenderTexture.Destroy(rcsRenderingVentral);
                Texture2D.Destroy(drawTextureFrontal);
                Texture2D.Destroy(drawTextureLateral);
                Texture2D.Destroy(drawTextureVentral);
                GameObject.Destroy(radarCam);
                rcsSetupCompleted = false;
            }
        }


		public static float GetRadarSnapshot(Vessel v, Vector3 origin, float camFoV)
		{/*
			
			TargetInfo ti = v.GetComponent<TargetInfo>();
			if(ti && ti.isMissile)
			{
				return 600;
			}

			float distance = (v.transform.position - origin).magnitude;

			radarCam.nearClipPlane = Mathf.Clamp(distance - 200, 20, 40000);
			radarCam.farClipPlane = Mathf.Clamp(distance + 200, 20, 40000);

			radarCam.fieldOfView = camFoV;

			radarCam.transform.position = origin;
			radarCam.transform.LookAt(v.CoM+(v.srf_velocity*Time.fixedDeltaTime));

			float pixels = 0;
			RenderTexture.active = radarRT;

			radarCam.Render();
			
			radarTex2D.ReadPixels(new Rect(0,0,radarResolution,radarResolution), 0,0);

			for(int x = 0; x < radarResolution; x++)
			{
				for(int y = 0; y < radarResolution; y++)	
				{
					if(radarTex2D.GetPixel(x,y).r<1)
					{
						pixels++;	
					}
				}
			}


			return pixels*4;
            */
            return 0;
		}

		public static void UpdateRadarLock(MissileFire myWpnManager, float directionAngle, Transform referenceTransform, float fov, Vector3 position, float minSignature, ref TargetSignatureData[] dataArray, float dataPersistTime, bool pingRWR, RadarWarningReceiver.RWRThreatTypes rwrType, bool radarSnapshot)
		{
			Vector3d geoPos = VectorUtils.WorldPositionToGeoCoords(position, FlightGlobals.currentMainBody);
			Vector3 forwardVector = referenceTransform.forward;
			Vector3 upVector = referenceTransform.up;//VectorUtils.GetUpDirection(position);
			Vector3 lookDirection = Quaternion.AngleAxis(directionAngle, upVector) * forwardVector;

			int dataIndex = 0;
			foreach(Vessel vessel in BDATargetManager.LoadedVessels)
			{
				if(vessel == null) continue;
				if(!vessel.loaded) continue;

				if(myWpnManager)
				{
					if(vessel == myWpnManager.vessel) continue; //ignore self
				}
				else if((vessel.transform.position - position).sqrMagnitude < 3600) continue;

				Vector3 vesselDirection = Vector3.ProjectOnPlane(vessel.CoM - position, upVector);

				if(Vector3.Angle(vesselDirection, lookDirection) < fov / 2)
				{
					if(TerrainCheck(referenceTransform.position, vessel.transform.position)) continue; //blocked by terrain

					float sig = float.MaxValue;
					if(radarSnapshot && minSignature > 0) sig = GetModifiedSignature(vessel, position);

					RadarWarningReceiver.PingRWR(vessel, position, rwrType, dataPersistTime);

					float detectSig = sig;

					VesselECMJInfo vesselJammer = vessel.GetComponent<VesselECMJInfo>();
					if(vesselJammer)
					{
						sig *= vesselJammer.rcsReductionFactor;
						detectSig += vesselJammer.jammerStrength;
					}

					if(detectSig > minSignature)
					{
						if(vessel.vesselType == VesselType.Debris)
						{
							vessel.gameObject.AddComponent<TargetInfo>();
						}
						else if(myWpnManager != null)
						{
							BDATargetManager.ReportVessel(vessel, myWpnManager);
						}

						while(dataIndex < dataArray.Length - 1)
						{
							if((dataArray[dataIndex].exists && Time.time - dataArray[dataIndex].timeAcquired > dataPersistTime) || !dataArray[dataIndex].exists)
							{
								break;
							}
							dataIndex++;
						}
						if(dataIndex >= dataArray.Length) break;
						dataArray[dataIndex] = new TargetSignatureData(vessel, sig);
						dataIndex++;
						if(dataIndex >= dataArray.Length) break;
					}
				}
			}

		}

		public static void UpdateRadarLock(MissileFire myWpnManager, float directionAngle, Transform referenceTransform, float fov, Vector3 position, float minSignature, ModuleRadar radar, bool pingRWR, RadarWarningReceiver.RWRThreatTypes rwrType, bool radarSnapshot)
		{
			Vector3d geoPos = VectorUtils.WorldPositionToGeoCoords(position, FlightGlobals.currentMainBody);
			Vector3 forwardVector = referenceTransform.forward;
			Vector3 upVector = referenceTransform.up;//VectorUtils.GetUpDirection(position);
			Vector3 lookDirection = Quaternion.AngleAxis(directionAngle, upVector) * forwardVector;

			foreach(Vessel vessel in BDATargetManager.LoadedVessels)
			{
				if(vessel == null) continue;
				if(!vessel.loaded) continue;

				if(myWpnManager)
				{
					if(vessel == myWpnManager.vessel) continue; //ignore self
				}
				else if((vessel.transform.position - position).sqrMagnitude < 3600) continue;

				Vector3 vesselDirection = Vector3.ProjectOnPlane(vessel.CoM - position, upVector);

				if(Vector3.Angle(vesselDirection, lookDirection) < fov / 2)
				{
					if(TerrainCheck(referenceTransform.position, vessel.transform.position)) continue; //blocked by terrain

					float sig = float.MaxValue;
					if(radarSnapshot && minSignature > 0) sig = GetModifiedSignature(vessel, position);

					RadarWarningReceiver.PingRWR(vessel, position, rwrType, radar.signalPersistTime);

					float detectSig = sig;

					VesselECMJInfo vesselJammer = vessel.GetComponent<VesselECMJInfo>();
					if(vesselJammer)
					{
						sig *= vesselJammer.rcsReductionFactor;
						detectSig += vesselJammer.jammerStrength;
					}

					if(detectSig > minSignature)
					{
						if(vessel.vesselType == VesselType.Debris)
						{
							vessel.gameObject.AddComponent<TargetInfo>();
						}
						else if(myWpnManager != null)
						{
							BDATargetManager.ReportVessel(vessel, myWpnManager);
						}

						//radar.vesselRadarData.AddRadarContact(radar, new TargetSignatureData(vessel, detectSig), false);
						radar.ReceiveContactData(new TargetSignatureData(vessel, detectSig), false);
					}
				}
			}

		}

		public static void UpdateRadarLock(Ray ray, float fov, float minSignature, ref TargetSignatureData[] dataArray, float dataPersistTime, bool pingRWR, RadarWarningReceiver.RWRThreatTypes rwrType, bool radarSnapshot)
		{
			int dataIndex = 0;
			foreach(Vessel vessel in BDATargetManager.LoadedVessels)
			{
				if(vessel == null) continue;
				if(!vessel.loaded) continue;
				//if(vessel.Landed) continue;

				Vector3 vectorToTarget = vessel.transform.position - ray.origin;
				if((vectorToTarget).sqrMagnitude < 10) continue; //ignore self

				if(Vector3.Dot(vectorToTarget, ray.direction) < 0) continue; //ignore behind ray

				if(Vector3.Angle(vessel.CoM - ray.origin, ray.direction) < fov / 2)
				{
					if(TerrainCheck(ray.origin, vessel.transform.position)) continue; //blocked by terrain
					float sig = float.MaxValue;
					if(radarSnapshot) sig = GetModifiedSignature(vessel, ray.origin);

					if(pingRWR && sig > minSignature * 0.66f)
					{
						RadarWarningReceiver.PingRWR(vessel, ray.origin, rwrType, dataPersistTime);
					}

					if(sig > minSignature)
					{
						while(dataIndex < dataArray.Length - 1)
						{
							if((dataArray[dataIndex].exists && Time.time - dataArray[dataIndex].timeAcquired > dataPersistTime) || !dataArray[dataIndex].exists)
							{
								break;
							}
							dataIndex++;
						}
						if(dataIndex >= dataArray.Length) break;
						dataArray[dataIndex] = new TargetSignatureData(vessel, sig);
						dataIndex++;
						if(dataIndex >= dataArray.Length) break;
					}
				}

			}
		}

		public static void UpdateRadarLock(Ray ray, Vector3 predictedPos, float fov, float minSignature, ModuleRadar radar, bool pingRWR, bool radarSnapshot, float dataPersistTime, bool locked, int lockIndex, Vessel lockedVessel)
		{
			RadarWarningReceiver.RWRThreatTypes rwrType = radar.rwrType;
			//Vessel lockedVessel = null;
			float closestSqrDist = 100;

			if(lockedVessel == null)
			{
				foreach(Vessel vessel in BDATargetManager.LoadedVessels)
				{
					if(vessel == null) continue;
					if(!vessel.loaded) continue;
					//if(vessel.Landed) continue;

					Vector3 vectorToTarget = vessel.transform.position - ray.origin;
					if((vectorToTarget).sqrMagnitude < 10) continue; //ignore self

					if(Vector3.Dot(vectorToTarget, ray.direction) < 0) continue; //ignore behind ray

					if(Vector3.Angle(vessel.CoM - ray.origin, ray.direction) < fov / 2)
					{
						float sqrDist = Vector3.SqrMagnitude(vessel.CoM - predictedPos);
						if(sqrDist < closestSqrDist)
						{
							closestSqrDist = sqrDist;
							lockedVessel = vessel;
						}
					}
				}
			}

			if(lockedVessel != null)
			{
				if(TerrainCheck(ray.origin, lockedVessel.transform.position))
				{
					radar.UnlockTargetAt(lockIndex, true); //blocked by terrain
					return;
				}

				float sig = float.MaxValue;
				if(radarSnapshot) sig = GetModifiedSignature(lockedVessel, ray.origin);

				if(pingRWR && sig > minSignature * 0.66f)
				{
					RadarWarningReceiver.PingRWR(lockedVessel, ray.origin, rwrType, dataPersistTime);
				}

				if(sig > minSignature)
				{
					//radar.vesselRadarData.AddRadarContact(radar, new TargetSignatureData(lockedVessel, sig), locked);
					radar.ReceiveContactData(new TargetSignatureData(lockedVessel, sig), locked);
				}
				else
				{
					radar.UnlockTargetAt(lockIndex, true);
					return;
				}
			}
			else
			{
				radar.UnlockTargetAt(lockIndex, true);
			}
		}

		/// <summary>
		/// Scans for targets in direction with field of view.
		/// Returns the direction scanned for debug 
		/// </summary>
		/// <returns>The scan direction.</returns>
		/// <param name="myWpnManager">My wpn manager.</param>
		/// <param name="directionAngle">Direction angle.</param>
		/// <param name="referenceTransform">Reference transform.</param>
		/// <param name="fov">Fov.</param>
		/// <param name="results">Results.</param>
		/// <param name="maxDistance">Max distance.</param>
		public static Vector3 GuardScanInDirection(MissileFire myWpnManager, float directionAngle, Transform referenceTransform, float fov, out ViewScanResults results, float maxDistance)
		{
			fov *= 1.1f;
			results = new ViewScanResults();
			results.foundMissile = false;
			results.foundHeatMissile = false;
			results.foundRadarMissile = false;
			results.foundAGM = false;
			results.firingAtMe = false;
			results.missileThreatDistance = float.MaxValue;
            results.threatVessel = null;
            results.threatWeaponManager = null;

			if(!myWpnManager || !referenceTransform)
			{
				return Vector3.zero;
			}

			Vector3 position = referenceTransform.position;
			Vector3d geoPos = VectorUtils.WorldPositionToGeoCoords(position, FlightGlobals.currentMainBody);
			Vector3 forwardVector = referenceTransform.forward;
			Vector3 upVector = referenceTransform.up;
			Vector3 lookDirection = Quaternion.AngleAxis(directionAngle, upVector) * forwardVector;



			foreach(Vessel vessel in BDATargetManager.LoadedVessels)
			{
				if(vessel == null) continue;

				if(vessel.loaded)
				{
					if(vessel == myWpnManager.vessel) continue; //ignore self

					Vector3 vesselProjectedDirection = Vector3.ProjectOnPlane(vessel.transform.position-position, upVector);
					Vector3 vesselDirection = vessel.transform.position - position;


					if(Vector3.Dot(vesselDirection, lookDirection) < 0) continue;

					float vesselDistance = (vessel.transform.position - position).magnitude;

					if(vesselDistance < maxDistance && Vector3.Angle(vesselProjectedDirection, lookDirection) < fov / 2 && Vector3.Angle(vessel.transform.position-position, -myWpnManager.transform.forward) < myWpnManager.guardAngle/2)
					{
						//Debug.Log("Found vessel: " + vessel.vesselName);
						if(TerrainCheck(referenceTransform.position, vessel.transform.position)) continue; //blocked by terrain

						BDATargetManager.ReportVessel(vessel, myWpnManager);

						TargetInfo tInfo;
						if((tInfo = vessel.GetComponent<TargetInfo>()))
						{
							if(tInfo.isMissile)
							{
								MissileBase missileBase;
								if(missileBase = tInfo.MissileBaseModule)
								{
									results.foundMissile = true;
									results.threatVessel = missileBase.vessel;
									Vector3 vectorFromMissile = myWpnManager.vessel.CoM - missileBase.part.transform.position;
									Vector3 relV = missileBase.vessel.srf_velocity - myWpnManager.vessel.srf_velocity;
									bool approaching = Vector3.Dot(relV, vectorFromMissile) > 0;
									if(missileBase.HasFired && missileBase.TimeIndex > 1 && approaching && (missileBase.TargetPosition - (myWpnManager.vessel.CoM + (myWpnManager.vessel.srf_velocity * Time.fixedDeltaTime))).sqrMagnitude < 3600)
									{
										if(missileBase.TargetingMode == MissileBase.TargetingModes.Heat)
										{
											results.foundHeatMissile = true;
											results.missileThreatDistance = Mathf.Min(results.missileThreatDistance, Vector3.Distance(missileBase.part.transform.position, myWpnManager.part.transform.position));
											results.threatPosition = missileBase.transform.position;
											break;
										}
										else if(missileBase.TargetingMode == MissileBase.TargetingModes.Radar)
										{
											results.foundRadarMissile = true;
											results.missileThreatDistance = Mathf.Min(results.missileThreatDistance, Vector3.Distance(missileBase.part.transform.position, myWpnManager.part.transform.position));
											results.threatPosition = missileBase.transform.position;
										}
										else if(missileBase.TargetingMode == MissileBase.TargetingModes.Laser)
										{
											results.foundAGM = true;
											results.missileThreatDistance = Mathf.Min(results.missileThreatDistance, Vector3.Distance(missileBase.part.transform.position, myWpnManager.part.transform.position));
											break;
										}
									}
									else
									{
										break;
									}
								}
							}
							else
							{
								//check if its shooting guns at me
								//if(!results.firingAtMe)       //more work, but we can't afford to be incorrect picking the closest threat
								//{
									foreach(ModuleWeapon weapon in vessel.FindPartModulesImplementing<ModuleWeapon>())
									{
										if(!weapon.recentlyFiring) continue;
										if(Vector3.Dot(weapon.fireTransforms[0].forward, vesselDirection) > 0) continue;

										if(Vector3.Angle(weapon.fireTransforms[0].forward, -vesselDirection) < 6500 / vesselDistance && (!results.firingAtMe || (weapon.vessel.ReferenceTransform.position - position).magnitude < (results.threatPosition - position).magnitude))
										{
											results.firingAtMe = true;
											results.threatPosition = weapon.vessel.transform.position;
                                            results.threatVessel = weapon.vessel;
                                            results.threatWeaponManager = weapon.weaponManager;
                                            break;
										}
									}
								//}
							}
						}
					}
				}
			}

			return lookDirection;
		}

		public static float GetModifiedSignature(Vessel vessel, Vector3 origin)
		{
			//float sig = GetBaseRadarSignature(vessel);
			float sig = GetRadarSnapshot(vessel, origin, 0.1f);

			Vector3 upVector = VectorUtils.GetUpDirection(origin);
			
			//sig *= Mathf.Pow(15000,2)/(vessel.transform.position-origin).sqrMagnitude;
			
			if(vessel.Landed)
			{
				sig *= 0.25f;
			}
			if(vessel.Splashed)
			{
				sig *= 0.4f;
			}
			
			//notching and ground clutter
			Vector3 targetDirection = (vessel.transform.position-origin).normalized;
			Vector3 targetClosureV = Vector3.ProjectOnPlane(Vector3.Project(vessel.srf_velocity,targetDirection), upVector);
			float notchFactor = 1;
			float angleFromUp = Vector3.Angle(targetDirection,upVector);
			float lookDownAngle = angleFromUp-90;

			if(lookDownAngle > 5)
			{
				notchFactor = Mathf.Clamp(targetClosureV.sqrMagnitude / 3600f, 0.1f, 1f);
			}
			else
			{
				notchFactor = Mathf.Clamp(targetClosureV.sqrMagnitude / 3600f, 0.8f, 3f);
			}

			float groundClutterFactor = Mathf.Clamp((90/angleFromUp), 0.25f, 1.85f);
			sig *= groundClutterFactor;
			sig *= notchFactor;

			VesselChaffInfo vci = vessel.GetComponent<VesselChaffInfo>();
			if(vci) sig *= vci.GetChaffMultiplier();

			return sig;
		}

		public static bool TerrainCheck(Vector3 start, Vector3 end)
		{
			return Physics.Linecast(start, end, 1<<15);
		}

	    public static Vector2 WorldToRadar(Vector3 worldPosition, Transform referenceTransform, Rect radarRect, float maxDistance)
		{
			float scale = maxDistance/(radarRect.height/2);
			Vector3 localPosition = referenceTransform.InverseTransformPoint(worldPosition);
			localPosition.y = 0;
			Vector2 radarPos = new Vector2((radarRect.width/2)+(localPosition.x/scale), (radarRect.height/2)-(localPosition.z/scale));
			return radarPos;
		}
		
		public static Vector2 WorldToRadarRadial(Vector3 worldPosition, Transform referenceTransform, Rect radarRect, float maxDistance, float maxAngle)
		{
			float scale = maxDistance/(radarRect.height);
			Vector3 localPosition = referenceTransform.InverseTransformPoint(worldPosition);
			localPosition.y = 0;
			float angle = Vector3.Angle(localPosition, Vector3.forward);
			if(localPosition.x < 0) angle = -angle;
			float xPos = (radarRect.width/2) + ((angle/maxAngle)*radarRect.width/2);
			float yPos = radarRect.height - (new Vector2 (localPosition.x, localPosition.z)).magnitude / scale;
			Vector2 radarPos = new Vector2(xPos, yPos);
			return radarPos;
		}

        
	}
}

