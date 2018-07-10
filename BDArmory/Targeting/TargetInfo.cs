using System.Collections;
using System.Collections.Generic;
using BDArmory.Core.Extension;
using BDArmory.Modules;
using BDArmory.UI;
using UnityEngine;

namespace BDArmory.Targeting
{
	public class TargetInfo : MonoBehaviour
	{
		public BDArmorySetup.BDATeams team;
		public bool isMissile;
		public MissileBase MissileBaseModule;
		public MissileFire weaponManager;
        List<MissileFire> friendliesEngaging;
        public float detectedTime;

        public float radarBaseSignature = -1;
        public bool radarBaseSignatureNeedsUpdate = true;
        public float radarModifiedSignature;
        public float radarLockbreakFactor;
        public float radarJammingDistance;
        public bool alreadyScheduledRCSUpdate = false;


        public bool isLanded
		{
			get
			{
                if (!vessel) return false;
                if (
                    (vessel.situation == Vessel.Situations.LANDED ||
                    vessel.situation == Vessel.Situations.SPLASHED) && // Boats should be included 
                    !isUnderwater //refrain from shooting subs with missiles
                    )
                {
                    
                    return true;
                }
                
                else
                    return false;
            }
		}

        public bool isFlying
        {
            get
            {
                if (!vessel) return false;
                if (vessel.situation == Vessel.Situations.FLYING || vessel.InOrbit()) return true;
                else
                    return false;
            }

        }

        public bool isUnderwater
        {
            get
            {
                if (!vessel) return false;
                if (vessel.altitude < -20) //some boats sit slightly underwater, this is only for submersibles
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        
        public bool isSplashed
        {
            get
            {
                if (!vessel) return false;
                if (vessel.situation == Vessel.Situations.SPLASHED) return true;
                else
                    return false;
            }
        }

        public Vector3 velocity
		{
			get
			{
				if(!vessel) return Vector3.zero;
				return vessel.Velocity();
			}
		}

		public Vector3 position
        {
            get
            {
                return vessel.vesselTransform.position;
            }
        }

		private Vessel vessel;

		public Vessel Vessel
		{
			get
			{
				return vessel;
			}
			set
			{
				vessel = value;
			}
		}

		public bool isThreat
		{
			get
			{
				if(!Vessel)
				{
					return false;
				}

				if(isMissile && MissileBaseModule && !MissileBaseModule.HasMissed)
				{
					return true;
				}
				else if(weaponManager && weaponManager.vessel.IsControllable)
				{
					return true;
				}

				return false;
			}
		}		
        
		void Awake()
		{
			if(!vessel)
			{
				vessel = GetComponent<Vessel>();
			}

			if(!vessel)
			{
				//Debug.Log ("[BDArmory]: TargetInfo was added to a non-vessel");
				Destroy (this);
				return;
			}

            //destroy this if a target info is already attached to the vessel
            IEnumerator otherInfo = vessel.gameObject.GetComponents<TargetInfo>().GetEnumerator();
            while (otherInfo.MoveNext())
            {
                if ((object)otherInfo.Current != this)
                {
                    Destroy(this);
                    return;
                }
            }

			team = BDArmorySetup.BDATeams.None;
			bool foundMf = false;
            List<MissileFire>.Enumerator mf = vessel.FindPartModulesImplementing<MissileFire>().GetEnumerator();
            while (mf.MoveNext())
            {
                foundMf = true;
                team = BDATargetManager.BoolToTeam(mf.Current.team);
                weaponManager = mf.Current;
                break;
            }
            mf.Dispose();

			if(!foundMf)
			{
                List<MissileBase>.Enumerator ml = vessel.FindPartModulesImplementing<MissileBase>().GetEnumerator();
                while (ml.MoveNext())
                {
                    isMissile = true;
                    MissileBaseModule = ml.Current;
                    team = BDATargetManager.BoolToTeam(ml.Current.Team);
                    break;
                }
                ml.Dispose();
			}
				
			if(team != BDArmorySetup.BDATeams.None)
			{
                BDATargetManager.AddTarget(this);
			}

			friendliesEngaging = new List<MissileFire>();

			vessel.OnJustAboutToBeDestroyed += AboutToBeDestroyed;            

            //add delegate to peace enable event
            BDArmorySetup.OnPeaceEnabled += OnPeaceEnabled;

            //lifeRoutine = StartCoroutine(LifetimeRoutine());              // TODO: CHECK BEHAVIOUR AND SIDE EFFECTS!

            if (!isMissile && team != BDArmorySetup.BDATeams.None)
			{
                GameEvents.onVesselPartCountChanged.Add(VesselModified);
                //massRoutine = StartCoroutine(MassRoutine());              // TODO: CHECK BEHAVIOUR AND SIDE EFFECTS!
            }
        }

		void OnPeaceEnabled()
		{
			//Destroy(this);
		}

		void OnDestroy()
		{
			//remove delegate from peace enable event
			BDArmorySetup.OnPeaceEnabled -= OnPeaceEnabled;
            vessel.OnJustAboutToBeDestroyed -= AboutToBeDestroyed;
            GameEvents.onVesselPartCountChanged.Remove(VesselModified);
        }
		
		IEnumerator UpdateRCSDelayed()
		{
            alreadyScheduledRCSUpdate = true;
            yield return new WaitForSeconds(1.0f);
            //radarBaseSignatureNeedsUpdate = true;     //TODO: currently disabled to reduce stuttering effects due to more demanding radar rendering!
        }

		void Update()
		{
			if(!vessel)
			{
				AboutToBeDestroyed();
			}
			else
			{
				if((vessel.vesselType == VesselType.Debris) && (weaponManager == null))
				{
					RemoveFromDatabases();
					team = BDArmorySetup.BDATeams.None;
				}
			}
		}

		public int numFriendliesEngaging
		{
			get
			{
				if(friendliesEngaging == null)
				{
					return 0;
				}
				friendliesEngaging.RemoveAll(item => item == null);
				return friendliesEngaging.Count;
			}
		}

		public void Engage(MissileFire mf)
		{
            if (mf == null || friendliesEngaging == null)
                return;

			if(!friendliesEngaging.Contains(mf))
			{
				friendliesEngaging?.Add(mf);
			}
		}

		public void Disengage(MissileFire mf)
		{
            if (mf == null)
                return;

            friendliesEngaging?.Remove(mf);
		}
		
		void AboutToBeDestroyed()
		{
			RemoveFromDatabases();
			Destroy(this);
		}

		public bool IsCloser(TargetInfo otherTarget, MissileFire myMf)
		{
			float thisSqrDist = (position-myMf.transform.position).sqrMagnitude;
			float otherSqrDist = (otherTarget.position-myMf.transform.position).sqrMagnitude;
			return thisSqrDist < otherSqrDist;
		}

		public void RemoveFromDatabases()
		{
			BDATargetManager.TargetDatabase[BDArmorySetup.BDATeams.A].Remove(this);
			BDATargetManager.TargetDatabase[BDArmorySetup.BDATeams.B].Remove(this);
		}

        public void VesselModified(Vessel v)
        {
            if (v && v == this.vessel)
            {
                if (!alreadyScheduledRCSUpdate)
                    StartCoroutine(UpdateRCSDelayed());
            }
        }

        public static Vector3 TargetCOMDispersion(Vessel v)
        {
            Vector3 TargetCOM_ = new Vector3(0,0);
            ShipConstruct sc = new ShipConstruct("ship", "temp ship", v.parts[0]);

            Vector3 size = ShipConstruction.CalculateCraftSize(sc);

            float dispersionMax = size.y;

            //float dispersionMax = 100f;

            float dispersion = Random.Range(0, dispersionMax);

            TargetCOM_ = v.CoM + new Vector3(0,dispersion);

            return TargetCOM_;
        }
	}
}

;