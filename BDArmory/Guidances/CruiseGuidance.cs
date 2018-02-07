using System;
using System.Net;
using System.Text;
using BDArmory.Core.Extension;
using BDArmory.Misc;
using BDArmory.Parts;
using UnityEngine;
using VehiclePhysics;

namespace BDArmory.Guidances
{
    public enum GuidanceState
    {
        Ascending,
        Cruising,
        Terminal,
    }

    public enum PitchDecision
    {
        Ascent,
        Descent,
        Hold
    }

    public enum ThrottleDecision
    {
        Increase,
        Decrease,
        Hold
    }


    public class CruiseGuidance
    {
        public ThrottleDecision ThrottleDecision { get; set; }
        public PitchDecision PitchDecision { get; set; }

        public GuidanceState GuidanceState { get; set; }

        private float _pitchAngle = 0;
        private float lastTimeDecision = 0;
        private MissileBase _missile;
        private double lastSpeedDelta = 0;
        private double lastPitchTimeDecision;
        private double lastThrottleTimeDecision;

        private double verticalAcceleration = 0;
        private double lastVerticalSpeed = 0;

        private float lastVerticalRead = 0;
        private double _originalDistance = 0;
        private Vector3 _startPoint;

        public CruiseGuidance(MissileBase missile)
        {
            this._missile = missile;
        }

        public  Vector3 CalculateCruiseGuidance(Vector3 targetPosition)
        {
            //set up
            if (_originalDistance == 0)
            {
                _startPoint = _missile.vessel.CoM;
                _originalDistance = Vector3.Distance(targetPosition, _missile.vessel.CoM);

                //calculating expected apogee bases on isosceles triangle

            }
            if (_missile.TimeIndex < 1)
            {
                return _missile.vessel.CoM + _missile.vessel.Velocity() * 10;
            }

            Vector3 upDirection = VectorUtils.GetUpDirection(_missile.vessel.CoM);
            Vector3 agmTarget = _missile.vessel.CoM + _missile.vessel.Velocity() * 10;

            Vector3 planarDirectionToTarget =
                Vector3.ProjectOnPlane(targetPosition - _missile.vessel.CoM, upDirection).normalized;
            
            // Ascending
            _missile.debugString.Append($"State="+GuidanceState);
            _missile.debugString.Append(Environment.NewLine);

            double missileAltitude = GetCurrentAltitude(_missile.vessel, planarDirectionToTarget, upDirection);
            _missile.debugString.Append($"Altitude=" + missileAltitude);
            _missile.debugString.Append(Environment.NewLine);

            _missile.debugString.Append($"Apoapsis=" + _missile.vessel.orbit.ApA);
            _missile.debugString.Append(Environment.NewLine);

            var surfaceDistanceVector = Vector3
                .Project((_missile.vessel.CoM - _startPoint), (targetPosition - _startPoint).normalized);

            var pendingDistance = _originalDistance - surfaceDistanceVector.magnitude;

            CalculateVerticalData();



            switch (GuidanceState)
            {
                case GuidanceState.Ascending:
                    UpdateThrottle();
                   
                    if (MissileWillReachAltitude(missileAltitude))
                    {
                        _pitchAngle = 0;
                        GuidanceState = GuidanceState.Cruising;
                       
                        break;
                    }

                    CheckIfTerminal(pendingDistance);
                   
                    agmTarget = _missile.vessel.CoM + (planarDirectionToTarget.normalized + upDirection.normalized) * 10f;
                    
                    break;
                case GuidanceState.Cruising:

                    CheckIfTerminal(pendingDistance);

                    MakeDecisionAboutPitch(_missile, missileAltitude);

                    agmTarget = _missile.vessel.CoM + (10 * planarDirectionToTarget) + (Mathf.Clamp(_pitchAngle,-2f,2f) * upDirection);
                    _missile.DrawDebugLine(_missile.vessel.CoM, (agmTarget - _missile.vessel.CoM).normalized * 20f);

                    //Altitude control
                    UpdatePitch(missileAltitude);
                    UpdateThrottle();

                    break;
                case GuidanceState.Terminal:

                    _missile.debugString.Append($"Descending");
                    _missile.debugString.Append(Environment.NewLine);
                    agmTarget = MissileGuidance.GetAirToGroundTarget(targetPosition, _missile.vessel, 1.85f);

                    if (_missile is BDModularGuidance)
                    {
                        if (_missile.vessel.InVacuum())
                        {
                            agmTarget = _missile.vessel.CoM + _missile.vessel.Velocity() * 10;
                        }
                    }
                    _missile.Throttle = Mathf.Clamp((float)(_missile.vessel.atmDensity * 10f), 0.01f, 1f);
                    break;
               
            }
            lastSpeedDelta = _missile.vessel.srfSpeed - _missile.CruiseSpeed;

            return agmTarget;
        }

        private void CalculateVerticalData()
        {
            if (Time.time - lastVerticalRead < 1f)
            {
                return;
            }
            lastVerticalRead = Time.time;

            this.verticalAcceleration = _missile.vessel.verticalSpeed - lastVerticalSpeed;
            lastVerticalSpeed = _missile.vessel.verticalSpeed;



        }

        private bool CheckIfTerminal(double pendingDistance)
        {
            if ((pendingDistance / this._missile.vessel.srfSpeed) < 20f)
            {
                GuidanceState = GuidanceState.Terminal;
                return true;
            }
            return false;
        }

        private void UpdateThrottle()
        {
            if (Time.time - this.lastThrottleTimeDecision > 0.1f)
            {
                MakeDecisionAboutThrottle(_missile);
                this.lastThrottleTimeDecision = Time.time;
            }
        }

        private void UpdatePitch(double missileAltitude)
        {
            if (Time.time - this.lastPitchTimeDecision > 1f)
            {
                MakeDecisionAboutPitch(_missile, missileAltitude);
                this.lastPitchTimeDecision = Time.time;
            }
        }

        private double GetCurrentAltitude(Vessel missileVessel, Vector3 planarDirectionToTarget, Vector3 upDirection)
        {
            float currentRadarAlt = MissileGuidance.GetRadarAltitude(missileVessel);
            Vector3 tRayDirection = (planarDirectionToTarget * 10) - (10 * upDirection);
            Ray terrainRay = new Ray(missileVessel.transform.position, tRayDirection);
            RaycastHit rayHit;

            if (Physics.Raycast(terrainRay, out rayHit, 30000, (1 << 15) | (1 << 17)))
            {
                float detectedAlt =
                    Vector3.Project(rayHit.point - missileVessel.transform.position, upDirection).magnitude;

                return Mathf.Min(detectedAlt, currentRadarAlt);
            }
            return currentRadarAlt;
        }


        private void MakeDecisionAboutThrottle(MissileBase missile)
        {
            var currentSpeedDelta = _missile.vessel.srfSpeed - _missile.CruiseSpeed;

            if (missile.vessel.srfSpeed > missile.CruiseSpeed && currentSpeedDelta >= lastSpeedDelta)
            {
                ThrottleDecision = ThrottleDecision.Decrease;
            }
            else if (missile.vessel.srfSpeed > missile.CruiseSpeed && currentSpeedDelta < lastSpeedDelta)
            {
                ThrottleDecision = ThrottleDecision.Hold;
            }
            else if (missile.vessel.srfSpeed < missile.CruiseSpeed && currentSpeedDelta > lastSpeedDelta)
            {
                ThrottleDecision = ThrottleDecision.Hold;
            }
            else if (missile.vessel.srfSpeed < missile.CruiseSpeed && currentSpeedDelta <= lastSpeedDelta)
            {
                ThrottleDecision = ThrottleDecision.Increase;
            }
  
            switch (ThrottleDecision)
            {
                case ThrottleDecision.Increase:
                    missile.Throttle = Mathf.Clamp(missile.Throttle + 0.01f, 0.01f, 1f);
                    break;
                case ThrottleDecision.Decrease:
                    missile.Throttle = Mathf.Clamp(missile.Throttle - 0.01f, 0.01f, 1f);
                    break;
                case ThrottleDecision.Hold:
                    break;
            }
        }

        private void MakeDecisionAboutPitch(MissileBase missile, double missileAltitude)
        {
            const double maxError = 50d;
            var futureAltitude = CalculateFutureAltitude(missileAltitude);

            if (futureAltitude < missile.CruiseAltitude)
            {
                PitchDecision = PitchDecision.Ascent;

            }
            else if (Math.Abs(futureAltitude - missile.CruiseAltitude) < maxError)
            {
                PitchDecision = PitchDecision.Hold;
            }
            else if (futureAltitude > missile.CruiseAltitude)
            {
                PitchDecision = PitchDecision.Descent;
            }
            
            switch (PitchDecision)
            {
                case PitchDecision.Ascent:
                    _pitchAngle += 0.01f;
                    break;
                case PitchDecision.Descent:
                    _pitchAngle -= 0.01f;
                    break;
                case PitchDecision.Hold:
                    break;
            }
        }


        private double CalculateFutureAltitude(double currentAltitude, float futureTime = 5f)
        {
            return  currentAltitude + this._missile.vessel.verticalSpeed * futureTime +
                                   (0.5f) * (this.verticalAcceleration) * Math.Pow(futureTime, 2);
        }

        private bool MissileWillReachAltitude(double currentAltitude)
        {
            return _missile.vessel.orbit.ApA > _missile.CruiseAltitude;          
        }
    }
}
