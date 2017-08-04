using BDArmory.CounterMeasure;
using BDArmory.Misc;
using BDArmory.Parts;
using BDArmory.Shaders;
using BDArmory.UI;
using System;
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

        internal static float rcsFrontal;             // internal so that editor analysis window has access to the details
        internal static float rcsLateral;             // dito
        internal static float rcsVentral;             // dito
        internal static float rcsTotal;               // dito

        private const float RCS_NORMALIZATION_FACTOR = 16.0f;
        private const float RCS_MISSILES = 999f;
        private const float RWR_PING_RANGE_FACTOR = 2.0f;
        private const float RADAR_IGNORE_DISTANCE_SQR = 100f;


        /// <summary>
        /// Get a vessel radar siganture, including all modifiers (ECM, stealth, ...)
        /// </summary>
        public static float GetVesselRadarSignature(Vessel v)
        {
            //1. baseSig = GetVesselRadarCrossSection
            float baseSig = GetVesselRadarCrossSection(v);

            //2. modifiedSig = GetVesselModifiedSignature(baseSig)    //ECM-jammers with rcs reduction effect; other rcs reductions (stealth)
            float modifiedSig = GetVesselModifiedSignature(v, baseSig);

            return modifiedSig;
        }


        /// <summary>
        /// Internal method: get a vessel base radar signature
        /// </summary>
        private static float GetVesselRadarCrossSection(Vessel v)
        {
            //read vesseltargetinfo, or render against radar cameras    
            TargetInfo ti = v.GetComponent<TargetInfo>();

            if (ti == null)
            {
                // add targetinfo to vessel
                ti = v.gameObject.AddComponent<TargetInfo>();
            }

            if (ti.isMissile)
            {
                // LEGACY special handling missile: should always be detected, hence signature is set to maximum
                // TODO: create field "missile_rcs_signature" on MIssileBase and return it here, allowing for different missiles
                //       to have different sizes for detection purpose.
                return RCS_MISSILES;
            }

            if (ti.radarBaseSignature == 0)
            {
                // perform radar rendering to obtain base cross section
                ti.radarBaseSignature = RenderVesselRadarSnapshot(v, v.transform);
            }

            return ti.radarBaseSignature; ;
        }


        /// <summary>
        /// Internal method: get a vessels siganture modifiers (ecm, stealth, ...)
        /// </summary>
        private static float GetVesselModifiedSignature(Vessel v, float baseSig)
        {
            float modifiedSig = baseSig;

            //TODO: 1) get vessel stealth modifier (NOT IMPLEMENTED YET)
            // if we ever introduce special stealth parts that increase low-observability, implement evaluation here!

            // 2) read vessel ecminfo for jammers with RCS reduction effect and multiply factor
            VesselECMJInfo vesseljammer = v.GetComponent<VesselECMJInfo>();
            if (vesseljammer)
            {
                modifiedSig *= vesseljammer.rcsReductionFactor;
            }

            // 3) read vessel ecminfo for active jammers and increase detectability accordingly
            if (vesseljammer)
            {
                // increase in detectability relative to jammerstrength and vessel rcs signature:
                // rcs_factor = jammerStrength / modifiedSig / 100 + 1.0f
                modifiedSig *= (((vesseljammer.jammerStrength / modifiedSig) / 100) + 1.0f);
            }

            /*
            // CHAFF SHOULD AFFECT LOCKING ONLY, NOT DETECTION!
            VesselChaffInfo vesselchaff = v.GetComponent<VesselChaffInfo>();
            if (vesselchaff)
            {
                modifiedSig *= vesselchaff.GetChaffMultiplier();
            }
            */

            return modifiedSig;
        }


        /// <summary>
        /// Internal method: do the actual radar snapshot rendering from 3 sides and store it in a vesseltargetinfo attached to the vessel
        /// 
        /// Note: Transform t is passed separatedly (instead of using v.transform), as the method need to be called from the editor
        ///         and there we dont have a VESSEL, only a SHIPCONSTRUCT, so the EditorRcSWindow passes the transform separately.
        /// </summary>
        /// <param name="inEditorZoom">when true, we try to make the rendered vessel fill the rendertexture completely, for a better detailed view. This does skew the computed cross section, so it is only for a good visual in editor!</param>
        public static float RenderVesselRadarSnapshot(Vessel v, Transform t, bool inEditorZoom = false)
        {
            const float radarDistance = 1000f;
            const float radarFOV = 2.0f;
            float distanceToShip;

            SetupResources();

            Bounds vesselbounds = CalcVesselBounds(v, t);
            if (BDArmorySettings.DRAW_DEBUG_LABELS)
            {
                Debug.Log("[BDArmory]: Rendering radar snapshot of vessel: " + v?.name is null ? "(null)" : v.name);
                Debug.Log("[BDArmory]: - bounds: " + vesselbounds.ToString());
                Debug.Log("[BDArmory]: - size: " + vesselbounds.size + ", magnitude: " + vesselbounds.size.magnitude);
            }


            // pass1: frontal
            radarCam.transform.position = vesselbounds.center + t.up * radarDistance;
            radarCam.transform.LookAt(vesselbounds.center);
            // setup camera FOV
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
            // setup camera FOV
            distanceToShip = Vector3.Distance(radarCam.transform.position, vesselbounds.center);
            radarCam.nearClipPlane = distanceToShip - 200;
            radarCam.farClipPlane = distanceToShip + 200;
            if (inEditorZoom)
                radarCam.fieldOfView = Mathf.Atan(vesselbounds.size.magnitude / distanceToShip) * 180 / Mathf.PI;
            else
                radarCam.fieldOfView = radarFOV;
            radarCam.targetTexture = rcsRenderingLateral;
            RenderTexture.active = rcsRenderingLateral;
            Shader.SetGlobalVector("_LIGHTDIR", -t.right);
            radarCam.RenderWithShader(BDAShaderLoader.RCSShader, string.Empty);
            drawTextureLateral.ReadPixels(new Rect(0, 0, radarResolution, radarResolution), 0, 0);
            drawTextureLateral.Apply();

            // pass3: Ventral
            radarCam.transform.position = vesselbounds.center + t.forward * radarDistance;
            radarCam.transform.LookAt(vesselbounds.center);
            // setup camera FOV
            distanceToShip = Vector3.Distance(radarCam.transform.position, vesselbounds.center);
            radarCam.nearClipPlane = distanceToShip - 200;
            radarCam.farClipPlane = distanceToShip + 200;
            if (inEditorZoom)
                radarCam.fieldOfView = Mathf.Atan(vesselbounds.size.magnitude / distanceToShip) * 180 / Mathf.PI;
            else
                radarCam.fieldOfView = radarFOV;
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
                if (BDArmorySettings.DRAW_DEBUG_LABELS)
                {
                    Debug.Log("[BDArmory]: - Vessel rcs is (frontal/lateral/ventral): " + rcsFrontal + "/" + rcsLateral + "/" + rcsVentral + " = total: " + rcsTotal);
                }
            }

            return rcsTotal;
        }


        /// <summary>
        /// Internal method: get a vessel's bounds
        /// Method implemention adapted from kronal vessel viewer
        /// </summary>
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


        /// <summary>
        /// Internal method: get a vessel's size (based on it's bounds)
        /// Method implemention adapted from kronal vessel viewer
        /// </summary>
        private static Vector3 GetVesselSize(Vessel v, Transform t)
        {
            return CalcVesselBounds(v, t).size;
        }


        /// <summary>
        /// Initialization of required resources. Necessary once per scene.
        /// </summary>
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

                rcsSetupCompleted = true;
            }

            if (radarCam == null)
            {
                //set up camera
                radarCam = (new GameObject("RadarCamera")).AddComponent<Camera>();
                radarCam.enabled = false;
                radarCam.clearFlags = CameraClearFlags.SolidColor;
                radarCam.backgroundColor = Color.black;
                radarCam.cullingMask = 1 << 0;   // only layer 0 active, see: http://wiki.kerbalspaceprogram.com/wiki/API:Layers
            }
        }


        /// <summary>
        /// Release of acquired resources. Necessary once at end of scene.
        /// </summary>
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


        /// <summary>
        /// Determine for a vesselposition relative to the radar position how much effect the ground clutter factor will have.
        /// </summary>
        public static float GetRadarGroundClutterModifier(ModuleRadar radar, Transform referenceTransform, Vector3 position, Vector3 vesselposition)
        {
            Vector3 upVector = referenceTransform.up;

            //ground clutter factor when looking down:
            Vector3 targetDirection = (vesselposition - position);
            float angleFromUp = Vector3.Angle(targetDirection, upVector);
            float lookDownAngle = angleFromUp - 90; // result range: -90 .. +90
            Mathf.Clamp(lookDownAngle, 0, 90);      // result range:   0 .. +90

            float groundClutterMutiplier = Mathf.Lerp(1, radar.radarGroundClutterFactor, (lookDownAngle / 90));
            return groundClutterMutiplier;
        }


        /// <summary>
        /// Special scanning method that needs to be set manually on the radar: perform fixed boresight scan with locked fov.
        /// Called from ModuleRadar, which will then attempt to immediately lock onto the detected targets.
        /// Uses detectionCurve for rcs evaluation.
        /// </summary>
        //was: public static void UpdateRadarLock(Ray ray, float fov, float minSignature, ref TargetSignatureData[] dataArray, float dataPersistTime, bool pingRWR, RadarWarningReceiver.RWRThreatTypes rwrType, bool radarSnapshot)
        public static bool RadarUpdateScanBoresight(Ray ray, float fov, ref TargetSignatureData[] dataArray, float dataPersistTime, ModuleRadar radar)
        {
            int dataIndex = 0;
            bool hasLocked = false;

            List<Vessel>.Enumerator loadedvessels = BDATargetManager.LoadedVessels.GetEnumerator();
            while (loadedvessels.MoveNext())
            {
                // ignore null, unloaded
                if (loadedvessels.Current == null ||
                    !loadedvessels.Current.loaded)
                    continue;

                // ignore self, ignore behind ray
                Vector3 vectorToTarget = (loadedvessels.Current.transform.position - ray.origin);
                if (((vectorToTarget).sqrMagnitude < RADAR_IGNORE_DISTANCE_SQR) ||
                     (Vector3.Dot(vectorToTarget, ray.direction) < 0))
                    continue;

                // ignore when blocked by terrain
                if (TerrainCheck(ray.origin, loadedvessels.Current.transform.position))
                    continue;

                if (Vector3.Angle(loadedvessels.Current.CoM - ray.origin, ray.direction) < fov / 2f)
                {
                    // get vessel's radar signature
                    float signature = GetVesselRadarSignature(loadedvessels.Current);
                    signature *= GetRadarGroundClutterModifier(radar, radar.referenceTransform, ray.origin, loadedvessels.Current.CoM);

                    // evaluate range
                    float distance = (loadedvessels.Current.CoM - ray.origin).magnitude;                                      //TODO: Performance! better if we could switch to sqrMagnitude...
                    if (distance > radar.radarMinDistanceDetect && distance < radar.radarMaxDistanceDetect)
                    {
                        //evaluate if we can detect such a signature at that range
                        float minDetectSig = radar.radarDetectionCurve.Evaluate(distance);

                        if (signature > minDetectSig)
                        {
                            // detected by radar
                            // fill attempted locks array for locking later:
                            while (dataIndex < dataArray.Length - 1)
                            {
                                if (!dataArray[dataIndex].exists || (dataArray[dataIndex].exists && (Time.time - dataArray[dataIndex].timeAcquired) > dataPersistTime))
                                {
                                    break;
                                }
                                dataIndex++;
                            }

                            if (dataIndex < dataArray.Length)
                            {
                                dataArray[dataIndex] = new TargetSignatureData(loadedvessels.Current, signature);
                                dataIndex++;
                                hasLocked = true;
                            }
                        }
                    }

                    //  our radar ping can be received at a higher range than we can detect, according to RWR range ping factor:
                    if (distance < radar.radarMaxDistanceDetect * RWR_PING_RANGE_FACTOR)
                        RadarWarningReceiver.PingRWR(loadedvessels.Current, ray.origin, radar.rwrType, radar.signalPersistTime);
                }

            }
            loadedvessels.Dispose();

            return hasLocked;
        }


        /// <summary>
        /// Special scanning method for missiles with active radar homing.
        /// Called from MissileBase / MissileLauncher, which will then attempt to immediately lock onto the detected targets.
        /// Uses the missiles locktrackCurve for rcs evaluation.
        /// </summary>
        //was: UpdateRadarLock(ray, maxOffBoresight, activeRadarMinThresh, ref scannedTargets, 0.4f, true, RadarWarningReceiver.RWRThreatTypes.MissileLock, true);
        public static bool RadarUpdateMissileLock(Ray ray, float fov, ref TargetSignatureData[] dataArray, float dataPersistTime, MissileBase missile)
        {
            int dataIndex = 0;
            bool hasLocked = false;

            List<Vessel>.Enumerator loadedvessels = BDATargetManager.LoadedVessels.GetEnumerator();
            while (loadedvessels.MoveNext())
            {
                // ignore null, unloaded
                if (loadedvessels.Current == null ||
                    !loadedvessels.Current.loaded)
                    continue;

                // ignore self, ignore behind ray
                Vector3 vectorToTarget = (loadedvessels.Current.transform.position - ray.origin);
                if (((vectorToTarget).sqrMagnitude < RADAR_IGNORE_DISTANCE_SQR) ||
                     (Vector3.Dot(vectorToTarget, ray.direction) < 0))
                    continue;

                // ignore when blocked by terrain
                if (TerrainCheck(ray.origin, loadedvessels.Current.transform.position))
                    continue;

                if (Vector3.Angle(loadedvessels.Current.CoM - ray.origin, ray.direction) < fov / 2f)
                {
                    // get vessel's radar signature
                    float signature = GetVesselRadarSignature(loadedvessels.Current);
                    // no ground clutter modifier for missiles

                    // evaluate range
                    float distance = (loadedvessels.Current.CoM - ray.origin).magnitude;                                      //TODO: Performance! better if we could switch to sqrMagnitude...
                    if (distance < missile.activeRadarRange)
                    {
                        //evaluate if we can detect such a signature at that range
                        float minDetectSig = missile.activeRadarLockTrackCurve.Evaluate(distance);

                        if (signature > minDetectSig)
                        {
                            // detected by radar
                            // fill attempted locks array for locking later:
                            while (dataIndex < dataArray.Length - 1)
                            {
                                if (!dataArray[dataIndex].exists || (dataArray[dataIndex].exists && (Time.time - dataArray[dataIndex].timeAcquired) > dataPersistTime))
                                {
                                    break;
                                }
                                dataIndex++;
                            }

                            if (dataIndex < dataArray.Length)
                            {
                                dataArray[dataIndex] = new TargetSignatureData(loadedvessels.Current, signature);
                                dataIndex++;
                                hasLocked = true;
                            }
                        }
                    }

                    //  our radar ping can be received at a higher range than we can detect, according to RWR range ping factor:
                    if (distance < missile.activeRadarRange * RWR_PING_RANGE_FACTOR)
                        RadarWarningReceiver.PingRWR(loadedvessels.Current, ray.origin, RadarWarningReceiver.RWRThreatTypes.MissileLock, dataPersistTime);
                }

            }
            loadedvessels.Dispose();

            return hasLocked;
        }


        /// <summary>
        /// Main scanning and locking method called from ModuleRadar.
        /// scanning both for omnidirectional and boresight scans.
        /// Uses detectionCurve OR locktrackCurve for rcs evaluation, depending on wether modeTryLock is true or false.
        /// </summary>
        /// <param name="modeTryLock">true: track/lock target; false: scan only</param>
        /// <param name="dataArray">relevant only for modeTryLock=true</param>
        /// <param name="dataPersistTime">optional, relevant only for modeTryLock=true</param>
        /// <returns></returns>
        //was: public static void UpdateRadarLock(MissileFire myWpnManager, float directionAngle, Transform referenceTransform, float fov, Vector3 position, float minSignature, ref TargetSignatureData[] dataArray, float dataPersistTime, bool pingRWR, RadarWarningReceiver.RWRThreatTypes rwrType, bool radarSnapshot)
        public static bool RadarUpdateScanLock(MissileFire myWpnManager, float directionAngle, Transform referenceTransform, float fov, Vector3 position, ModuleRadar radar, bool modeTryLock, ref TargetSignatureData[] dataArray, float dataPersistTime = 0f)
        {
            Vector3 forwardVector = referenceTransform.forward;
            Vector3 upVector = referenceTransform.up;
            Vector3 lookDirection = Quaternion.AngleAxis(directionAngle, upVector) * forwardVector;
            int dataIndex = 0;
            bool hasLocked = false;

            List<Vessel>.Enumerator loadedvessels = BDATargetManager.LoadedVessels.GetEnumerator();
            while (loadedvessels.MoveNext())
            {
                // ignore null, unloaded and self
                if (loadedvessels.Current == null ||
                    !loadedvessels.Current.loaded ||
                    loadedvessels.Current == myWpnManager.vessel)
                    continue;

                // ignore too close ones
                if ((loadedvessels.Current.transform.position - position).sqrMagnitude < RADAR_IGNORE_DISTANCE_SQR)
                    continue;

                // ignore when blocked by terrain
                if (TerrainCheck(referenceTransform.position, loadedvessels.Current.transform.position))
                    continue;

                Vector3 vesselDirection = Vector3.ProjectOnPlane(loadedvessels.Current.CoM - position, upVector);
                if (Vector3.Angle(vesselDirection, lookDirection) < fov / 2f)
                {
                    // get vessel's radar signature
                    float signature = GetVesselRadarSignature(loadedvessels.Current);
                    signature *= GetRadarGroundClutterModifier(radar, referenceTransform, position, loadedvessels.Current.CoM);

                    // evaluate range
                    float distance = (loadedvessels.Current.CoM - position).magnitude;                                      //TODO: Performance! better if we could switch to sqrMagnitude...

                    if (modeTryLock)    // LOCK/TRACK TARGET:
                    {
                        //evaluate if we can lock/track such a signature at that range
                        if (distance > radar.radarMinDistanceLockTrack && distance < radar.radarMaxDistanceLockTrack)
                        {
                            //evaluate if we can lock/track such a signature at that range
                            float minLockSig = radar.radarLockTrackCurve.Evaluate(distance);

                            if (signature > minLockSig)
                            {
                                // detected by radar
                                if (myWpnManager != null)
                                {
                                    BDATargetManager.ReportVessel(loadedvessels.Current, myWpnManager);
                                }

                                // fill attempted locks array for locking later:
                                while (dataIndex < dataArray.Length - 1)
                                {
                                    if (!dataArray[dataIndex].exists || (dataArray[dataIndex].exists && (Time.time - dataArray[dataIndex].timeAcquired) > dataPersistTime))
                                    {
                                        break;
                                    }
                                    dataIndex++;
                                }

                                if (dataIndex < dataArray.Length)
                                {
                                    dataArray[dataIndex] = new TargetSignatureData(loadedvessels.Current, signature);
                                    dataIndex++;
                                    hasLocked = true;
                                }

                            }
                        }

                        //  our radar ping can be received at a higher range than we can lock/track, according to RWR range ping factor:
                        if (distance < radar.radarMaxDistanceLockTrack * RWR_PING_RANGE_FACTOR)
                            RadarWarningReceiver.PingRWR(loadedvessels.Current, position, radar.rwrType, radar.signalPersistTime);

                    }
                    else   // SCAN/DETECT TARGETS:
                    {
                        //evaluate if we can detect such a signature at that range
                        if (distance > radar.radarMinDistanceDetect && distance < radar.radarMaxDistanceDetect)
                        {
                            //evaluate if we can detect or lock such a signature at that range
                            float minDetectSig = radar.radarDetectionCurve.Evaluate(distance);

                            if (signature > minDetectSig)
                            {
                                // detected by radar
                                if (myWpnManager != null)
                                {
                                    BDATargetManager.ReportVessel(loadedvessels.Current, myWpnManager);
                                }

                                // report scanned targets only
                                radar.ReceiveContactData(new TargetSignatureData(loadedvessels.Current, signature), false);
                            }
                        }

                        //  our radar ping can be received at a higher range than we can detect, according to RWR range ping factor:
                        if (distance < radar.radarMaxDistanceDetect * RWR_PING_RANGE_FACTOR)
                            RadarWarningReceiver.PingRWR(loadedvessels.Current, position, radar.rwrType, radar.signalPersistTime);
                    }
                    
                }


            }
            loadedvessels.Dispose();

            return hasLocked;
        }


        /// <summary>
        /// Update a lock on a tracked target.
        /// Uses locktrackCurve for rcs evaluation.
        /// </summary>
        //was: public static void UpdateRadarLock(Ray ray, Vector3 predictedPos, float fov, float minSignature, ModuleRadar radar, bool pingRWR, bool radarSnapshot, float dataPersistTime, bool locked, int lockIndex, Vessel lockedVessel)
        public static bool RadarUpdateLockTrack(Ray ray, Vector3 predictedPos, float fov, ModuleRadar radar, float dataPersistTime, bool locked, int lockIndex, Vessel lockedVessel)
        {
            float closestSqrDist = 1000f;

            // first: re-acquire lock if temporarily lost
            if (!lockedVessel)
            {
                List<Vessel>.Enumerator loadedvessels = BDATargetManager.LoadedVessels.GetEnumerator();
                while (loadedvessels.MoveNext())
                {
                    // ignore null, unloaded
                    if (loadedvessels.Current == null ||
                        !loadedvessels.Current.loaded)
                        continue;

                    // ignore self, ignore behind ray
                    Vector3 vectorToTarget = (loadedvessels.Current.transform.position - ray.origin);
                    if (((vectorToTarget).sqrMagnitude < RADAR_IGNORE_DISTANCE_SQR) ||
                         (Vector3.Dot(vectorToTarget, ray.direction) < 0))
                        continue;

                    if (Vector3.Angle(loadedvessels.Current.CoM - ray.origin, ray.direction) < fov/2)
                    {
                        float sqrDist = Vector3.SqrMagnitude(loadedvessels.Current.CoM - predictedPos);
                        if (sqrDist < closestSqrDist)
                        {
                            // best candidate so far, take it
                            closestSqrDist = sqrDist;
                            lockedVessel = loadedvessels.Current;
                        }
                    }

                }
                loadedvessels.Dispose();
            }

            // second: track that lock
            if (lockedVessel)
            {
                // blocked by terrain?
                if (TerrainCheck(ray.origin, lockedVessel.transform.position))
                {
                    radar.UnlockTargetAt(lockIndex, true);
                    return false;
                }

                // get vessel's radar signature
                float signature = GetVesselRadarSignature(lockedVessel);
                signature *= GetRadarGroundClutterModifier(radar, radar.referenceTransform, ray.origin, lockedVessel.CoM);

                // evaluate range
                float distance = (lockedVessel.CoM - ray.origin).magnitude;                                      //TODO: Performance! better if we could switch to sqrMagnitude...
                if (distance > radar.radarMinDistanceLockTrack && distance < radar.radarMaxDistanceLockTrack)
                {
                    //evaluate if we can detect such a signature at that range
                    float minTrackSig = radar.radarLockTrackCurve.Evaluate(distance);

                    if (signature > minTrackSig)
                    {
                        // can be tracked
                        radar.ReceiveContactData(new TargetSignatureData(lockedVessel, signature), locked);
                    }
                    else
                    {
                        // cannot track, so unlock it
                        radar.UnlockTargetAt(lockIndex, true);
                        return false;
                    }
                }

                //  our radar ping can be received at a higher range than we can detect, according to RWR range ping factor:
                if (distance < radar.radarMaxDistanceLockTrack * RWR_PING_RANGE_FACTOR)
                    RadarWarningReceiver.PingRWR(lockedVessel, ray.origin, radar.rwrType, radar.signalPersistTime);

                return true;
            }
            else
            {
                // nothing tracked/locked at this index
                radar.UnlockTargetAt(lockIndex, true);
            }

            return false;
        }


        /// <summary>
        /// Scans for targets in direction with field of view.
        /// Returns the direction scanned for debug .
        /// ONLY FOR LEGACY TARGETING, REMOVE IN FUTURE VERSION!
        /// </summary>
        [Obsolete("ONLY FOR LEGACY TARGETING, REMOVE IN FUTURE VERSION")]
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


        /// <summary>
        /// Helper method: check if line intersects terrain
        /// </summary>
		public static bool TerrainCheck(Vector3 start, Vector3 end)
		{
			return Physics.Linecast(start, end, 1<<15); // only layer 15 active, see: http://wiki.kerbalspaceprogram.com/wiki/API:Layers
        }


        /// <summary>
        /// Helper method: map a position onto the radar display
        /// </summary>
        public static Vector2 WorldToRadar(Vector3 worldPosition, Transform referenceTransform, Rect radarRect, float maxDistance)
		{
			float scale = maxDistance/(radarRect.height/2);
			Vector3 localPosition = referenceTransform.InverseTransformPoint(worldPosition);
			localPosition.y = 0;
			Vector2 radarPos = new Vector2((radarRect.width/2)+(localPosition.x/scale), (radarRect.height/2)-(localPosition.z/scale));
			return radarPos;
		}


        /// <summary>
        /// Helper method: map a position onto the radar display (for non-onmi radars)
        /// </summary>
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

