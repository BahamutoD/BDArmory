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


        public CruiseGuidance(MissileBase missile)
        {
            this._missile = missile;
        }

        public  Vector3 CalculateCruiseGuidance(Vector3 targetPosition)
        {
            if (_missile.TimeIndex < 1)
            {
                return _missile.vessel.CoM + _missile.vessel.Velocity() * 10;
            }

            Vector3 agmTarget = _missile.vessel.CoM + _missile.vessel.Velocity() * 10, dToTarget, direction;
            // Ascending


            _missile.debugString.Append($"State="+GuidanceState);
            _missile.debugString.Append(Environment.NewLine);
            switch (GuidanceState)
            {
                case GuidanceState.Ascending:
                    _missile.Throttle = 1;
                    dToTarget = targetPosition - _missile.vessel.CoM;
                    direction = Quaternion.AngleAxis(Mathf.Clamp(_missile.maxOffBoresight * 0.9f, 0, 45f), Vector3.Cross(dToTarget, VectorUtils.GetUpDirection(_missile.vessel.CoM))) * dToTarget;
                    agmTarget = _missile.vessel.CoM + direction;

                    if (MissileWillReachAltitude())
                    {
                        if (MissileGuidance.GetRadarAltitude(this._missile.vessel) >= this._missile.CruiseAltitude)
                        {
                            _pitchAngle = 0;
                            GuidanceState = GuidanceState.Cruising;
                        }
                        _missile.Throttle = 0;
                    }
                    else if (Vector3.Angle(this._missile.vessel.Velocity(), targetPosition - this._missile.vessel.CoM) >
                            45f && Vector3.Distance(this._missile.vessel.CoM, targetPosition) < 4f * this._missile.CruiseAltitude)
                    {
                            GuidanceState = GuidanceState.Terminal;
                    }
                    else
                    {
                        _missile.Throttle = 1;
                        dToTarget = targetPosition - _missile.vessel.CoM;
                        direction = Quaternion.AngleAxis(Mathf.Clamp(_missile.maxOffBoresight * 0.9f, 0, 45f), Vector3.Cross(dToTarget, VectorUtils.GetUpDirection(_missile.vessel.CoM))) * dToTarget;
                        agmTarget = _missile.vessel.CoM + direction;
                    }

                    break;
                case GuidanceState.Cruising:

                    if (Vector3.Angle(this._missile.vessel.Velocity(), targetPosition - this._missile.vessel.CoM) >
                        30f && Vector3.Distance(this._missile.vessel.CoM, targetPosition) < 4f * this._missile.CruiseAltitude)
                    {
                        GuidanceState = GuidanceState.Terminal;
                        break;
                    }

                    MakeDecisionAboutPitch(_missile);

                    dToTarget = targetPosition - _missile.vessel.CoM;
                    direction = Quaternion.AngleAxis(_pitchAngle, Vector3.Cross(dToTarget, VectorUtils.GetUpDirection(_missile.vessel.CoM))) * dToTarget;
                    agmTarget = _missile.vessel.CoM + direction;

                    //Altitude control
                    if (Time.time - lastTimeDecision > 0.5f)
                    {
                        MakeDecisionAboutThrottle(_missile);

             
                        lastTimeDecision = Time.time;
                    }

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

            return agmTarget;
        }

        private void CalculateNewPitchAngle(MissileBase missile)
        {
            switch (PitchDecision)
            {
                case PitchDecision.Ascent:
                    _pitchAngle += 0.1f;
                    break;
                case PitchDecision.Descent:
                    _pitchAngle -= 0.1f;
                    break;
                case PitchDecision.Hold:
                    break;
            }
        }

        private void MakeDecisionAboutThrottle(MissileBase missile)
        {
            if (missile.vessel.srfSpeed > missile.CruiseSpeed)
            {
                ThrottleDecision = ThrottleDecision.Decrease;
            }
            else if (missile.vessel.srfSpeed < missile.CruiseSpeed)
            {
                ThrottleDecision = ThrottleDecision.Increase;
            }
            else
            {
                ThrottleDecision = ThrottleDecision.Hold;
            }
  
            switch (ThrottleDecision)
            {
                case ThrottleDecision.Increase:
                    missile.Throttle = Mathf.Clamp(missile.Throttle * 2f, 0.01f, 1f);
                    break;
                case ThrottleDecision.Decrease:
                    missile.Throttle = Mathf.Clamp(missile.Throttle * 0.5f, 0.01f, 1f);
                    break;
                case ThrottleDecision.Hold:
                    break;
            }
        }

        private void MakeDecisionAboutPitch(MissileBase missile)
        {
            if (MissileGuidance.GetRadarAltitude(this._missile.vessel) > missile.CruiseAltitude && missile.vessel.verticalSpeed > 0)
            {
                PitchDecision = PitchDecision.Descent;

            }
            else if (MissileGuidance.GetRadarAltitude(this._missile.vessel) > missile.CruiseAltitude &&
                     missile.vessel.verticalSpeed < 0)
            {
                PitchDecision = PitchDecision.Hold;
            }
            else if (MissileGuidance.GetRadarAltitude(this._missile.vessel) < missile.CruiseAltitude &&
                     missile.vessel.verticalSpeed > 0)
            {
                PitchDecision = PitchDecision.Hold;
            }
            else if (MissileGuidance.GetRadarAltitude(this._missile.vessel) < missile.CruiseAltitude &&
                     missile.vessel.verticalSpeed < 0)
            {
                PitchDecision = PitchDecision.Ascent;
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

        private bool MissileWillReachAltitude()
        {
            double timeToV0 = this._missile.vessel.verticalSpeed / Gravity.reference;

            double finalAltitude = MissileGuidance.GetRadarAltitude(this._missile.vessel) + this._missile.vessel.verticalSpeed * timeToV0 +
                                   (0.5f) * (-Gravity.reference) * Math.Pow(timeToV0, 2);

            return finalAltitude >= this._missile.CruiseAltitude;          
        }
    }
}
