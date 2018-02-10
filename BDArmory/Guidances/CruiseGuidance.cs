using System;
using BDArmory.Core.Extension;
using BDArmory.Misc;
using BDArmory.Parts;
using UnityEngine;

namespace BDArmory.Guidances
{
    public enum GuidanceState
    {
        Ascending,
        Cruising,
        Terminal
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
        private readonly MissileBase _missile;
        private double _originalDistance;

        private float _pitchAngle;
        private Vector3 _startPoint;
        private double _futureAltitude;
        private double _futureSpeed;
        private double _horizontalAcceleration;

        private float _lastDataRead;
        private double _lastHorizontalSpeed;
        private double _lastPitchTimeDecision;
        private double _lastSpeedDelta;
        private double _lastThrottleTimeDecision;
        private float _lastTimeDecision = 0;
        private double _lastVerticalSpeed;

        private double _verticalAcceleration;

        public CruiseGuidance(MissileBase missile)
        {
            _missile = missile;
        }

        public ThrottleDecision ThrottleDecision { get; set; }
        public PitchDecision PitchDecision { get; set; }

        public GuidanceState GuidanceState { get; set; }

        public Vector3 CalculateCruiseGuidance(Vector3 targetPosition)
        {
            //set up
            if (_originalDistance == 0)
            {
                _startPoint = _missile.vessel.CoM;
                _originalDistance = Vector3.Distance(targetPosition, _missile.vessel.CoM);

                //calculating expected apogee bases on isosceles triangle
            }
            if (_missile.TimeIndex < 1)
                return _missile.vessel.CoM + _missile.vessel.Velocity() * 10;

            var upDirection = VectorUtils.GetUpDirection(_missile.vessel.CoM);


            var planarDirectionToTarget =
                Vector3.ProjectOnPlane(targetPosition - _missile.vessel.CoM, upDirection).normalized;

            // Ascending
            _missile.debugString.Append("State=" + GuidanceState);
            _missile.debugString.Append(Environment.NewLine);

            var missileAltitude = GetCurrentAltitude(_missile.vessel, planarDirectionToTarget, upDirection);
            _missile.debugString.Append("Altitude=" + missileAltitude);
            _missile.debugString.Append(Environment.NewLine);

            _missile.debugString.Append("Apoapsis=" + _missile.vessel.orbit.ApA);
            _missile.debugString.Append(Environment.NewLine);

            _missile.debugString.Append("Future Altitude=" + _futureAltitude);
            _missile.debugString.Append(Environment.NewLine);

            _missile.debugString.Append("Pitch angle=" + _pitchAngle);
            _missile.debugString.Append(Environment.NewLine);

            _missile.debugString.Append("Pitch decision=" + PitchDecision);
            _missile.debugString.Append(Environment.NewLine);

            _missile.debugString.Append("lastVerticalSpeed=" + _lastVerticalSpeed);
            _missile.debugString.Append(Environment.NewLine);

            _missile.debugString.Append("verticalAcceleration=" + _verticalAcceleration);
            _missile.debugString.Append(Environment.NewLine);

            var surfaceDistanceVector = Vector3
                .Project(_missile.vessel.CoM - _startPoint, (targetPosition - _startPoint).normalized);

            var pendingDistance = _originalDistance - surfaceDistanceVector.magnitude;

            CalculatePerSecondData();


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

                    return _missile.vessel.CoM + (planarDirectionToTarget.normalized + upDirection.normalized) * 10f;

                case GuidanceState.Cruising:

                    CheckIfTerminal(pendingDistance);
                    //Altitude control
                    UpdatePitch(missileAltitude);
                    UpdateThrottle();

                    return _missile.vessel.CoM + 10 * planarDirectionToTarget.normalized + _pitchAngle * upDirection;

                case GuidanceState.Terminal:

                    _missile.debugString.Append($"Descending");
                    _missile.debugString.Append(Environment.NewLine);

                    _missile.Throttle = Mathf.Clamp((float) (_missile.vessel.atmDensity * 10f), 0.01f, 1f);

                    if (_missile is BDModularGuidance)
                        if (_missile.vessel.InVacuum())
                            return _missile.vessel.CoM + _missile.vessel.Velocity() * 10;

                    return MissileGuidance.GetAirToGroundTarget(targetPosition, _missile.vessel, 1.85f);
            }

            return _missile.vessel.CoM + _missile.vessel.Velocity() * 10;
        }

        private void CalculatePerSecondData()
        {
            if (Time.time - _lastDataRead < 1f)
                return;
            _lastDataRead = Time.time;

            _verticalAcceleration = _missile.vessel.verticalSpeed - _lastVerticalSpeed;
            _lastVerticalSpeed = _missile.vessel.verticalSpeed;

            _horizontalAcceleration = _missile.vessel.horizontalSrfSpeed - _lastHorizontalSpeed;
            _lastHorizontalSpeed = _missile.vessel.horizontalSrfSpeed;
        }

        private bool CheckIfTerminal(double pendingDistance)
        {
            if (pendingDistance / _missile.vessel.horizontalSrfSpeed < 20f)
            {
                GuidanceState = GuidanceState.Terminal;
                return true;
            }
            return false;
        }

        private void UpdateThrottle()
        {
            if (Time.time - _lastThrottleTimeDecision > 0.1f)
            {
                MakeDecisionAboutThrottle(_missile);
                _lastThrottleTimeDecision = Time.time;
            }
        }

        private void UpdatePitch(double missileAltitude)
        {
            if (Time.time - _lastPitchTimeDecision > 1f)
            {
                MakeDecisionAboutPitch(_missile, missileAltitude);
                _lastPitchTimeDecision = Time.time;
            }
        }

        private double GetCurrentAltitude(Vessel missileVessel, Vector3 planarDirectionToTarget, Vector3 upDirection)
        {
            var currentRadarAlt = MissileGuidance.GetRadarAltitude(missileVessel);
            var tRayDirection = planarDirectionToTarget * 10 - 10 * upDirection;
            var terrainRay = new Ray(missileVessel.transform.position, tRayDirection);
            RaycastHit rayHit;

            if (Physics.Raycast(terrainRay, out rayHit, 30000, (1 << 15) | (1 << 17)))
            {
                var detectedAlt =
                    Vector3.Project(rayHit.point - missileVessel.transform.position, upDirection).magnitude;

                return Mathf.Min(detectedAlt, currentRadarAlt);
            }
            return currentRadarAlt;
        }


        private void MakeDecisionAboutThrottle(MissileBase missile)
        {
            const double maxError = 10;
            _futureSpeed = CalculateFutureSpeed();


            var currentSpeedDelta = missile.vessel.horizontalSrfSpeed - _missile.CruiseSpeed;

            if (_futureSpeed > missile.CruiseSpeed)
                ThrottleDecision = ThrottleDecision.Decrease;
            else if (Math.Abs(_futureSpeed - _missile.CruiseSpeed) < maxError)
                ThrottleDecision = ThrottleDecision.Hold;
            else
                ThrottleDecision = ThrottleDecision.Increase;

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

            _lastSpeedDelta = currentSpeedDelta;
        }

        private void MakeDecisionAboutPitch(MissileBase missile, double missileAltitude)
        {
            const double maxVerticalSpeed = 20d;

            _futureAltitude = CalculateFutureAltitude(missileAltitude);

            PitchDecision futureDecision;

            if (_futureAltitude < missile.CruiseAltitude)
            {
                futureDecision = PitchDecision.Ascent;
            }
            else if (_futureAltitude > missile.CruiseAltitude)
            {
                futureDecision = PitchDecision.Descent;
            }
            else
            {
                futureDecision = PitchDecision.Hold;
            }
        

            if (PitchDecision == PitchDecision.Ascent && PitchDecision == futureDecision)
            {
                if (_missile.vessel.verticalSpeed > maxVerticalSpeed)
                {
                    futureDecision = PitchDecision.Hold;
                }
            }
            else if (PitchDecision == PitchDecision.Descent && PitchDecision == futureDecision)
            {
                if (_missile.vessel.verticalSpeed < -maxVerticalSpeed)
                {
                    futureDecision = PitchDecision.Hold;
                }
            }

            PitchDecision = futureDecision;

            switch (PitchDecision)
            {
                case PitchDecision.Ascent:
                    _pitchAngle = Mathf.Clamp(_pitchAngle + 0.2f, -2.5f, 2.5f);
                    break;
                case PitchDecision.Descent:
                    _pitchAngle = Mathf.Clamp(_pitchAngle - 0.2f, -2.5f, 2.5f);
                    break;
                case PitchDecision.Hold:
                    break;
            }
        }


        private double CalculateFutureAltitude(double currentAltitude, float futureTime = 5f)
        {
            return currentAltitude + _missile.vessel.verticalSpeed * futureTime +
                   0.5f * _verticalAcceleration * Math.Pow(futureTime, 2);
        }


        private double CalculateFutureSpeed(float futureTime = 5f)
        {
            return _missile.vessel.horizontalSrfSpeed + _horizontalAcceleration * futureTime;
        }

        private bool MissileWillReachAltitude(double currentAltitude)
        {
            return _missile.vessel.orbit.ApA > _missile.CruiseAltitude;
        }
    }
}