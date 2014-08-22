using System;
using UnityEngine;

namespace BahaTurret
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class BDACameraTools : MonoBehaviour
	{
		public static Part lastProjectileFired = null;
		
		Vessel vessel;
		public static FlightCamera flightCamera;
		Vector3 targetPosition;
		Vector3 cameraUp;
		Transform origParent;
		Quaternion origRotation;
		Vector3 origPosition;
		float origFOV;
		float origClip;
		
		
		
		bool targetCameraEnabled = false;
		bool targetCloseCameraEnabled = false;
		bool targetFreeCameraEnabled = false;
		bool projectileCameraEnabled = false;
		bool flybyCameraEnabled = false;
		
		public static bool projectileCameraFinished = false;
		float pjcTimeCheck;
		
		void Start()
		{
			vessel = FlightGlobals.ActiveVessel;
			flightCamera = FlightCamera.fetch;
			
			
			origParent = flightCamera.transform.parent;
			origRotation = flightCamera.transform.rotation;
			origPosition = flightCamera.transform.position;
			origFOV = Camera.main.fieldOfView;
			origClip = Camera.main.nearClipPlane;
			
		}
		
		
		void Update()
		{
			if(CheckForManager() && BDArmorySettings.CAMERA_TOOLS && true==false)
			{
					
				#region targetCamera
				if(Input.GetKeyDown(KeyCode.Keypad0))
				{
					TargetCamera();
				}
				if(targetCameraEnabled)
				{
					//targetPosition = vessel.targetObject.GetTransform().position;
					float moveSpeed = Vector3.Distance(targetPosition, vessel.targetObject.GetTransform().position);
					targetPosition = Vector3.MoveTowards(targetPosition, vessel.targetObject.GetTransform().position, moveSpeed*Time.deltaTime);
					if(Vector3.Distance(targetPosition, vessel.targetObject.GetTransform().position) > 1200)
					{
						targetPosition = vessel.targetObject.GetTransform().position;	
					}
					flightCamera.transform.rotation = Quaternion.LookRotation(targetPosition - flightCamera.transform.position, FlightGlobals.getUpAxis());
					flightCamera.transform.position = vessel.transform.position;
					
					//flightCamera.SetFoV(25 * 60/(vessel.targetObject.GetTransform().position - vessel.transform.position).magnitude);
					flightCamera.SetFoV(1);
				}
				#endregion
				
				
				#region targetCloseCamera
				if(Input.GetKeyDown(KeyCode.Keypad1))
				{
					if(!targetCloseCameraEnabled && vessel.targetObject != null)
					{
						RevertCamera();
						SaveCamera();
						Camera.main.nearClipPlane = 25;
						flightCamera.transform.parent = vessel.transform;
						flightCamera.setTarget(null);
						flightCamera.transform.rotation = Quaternion.LookRotation(targetPosition - flightCamera.transform.position, FlightGlobals.getUpAxis());
						targetCloseCameraEnabled = true;
						
						targetPosition = vessel.targetObject.GetTransform().position;
					}
					else
					{
						RevertCamera();
					}
				}
				if(targetCloseCameraEnabled)
				{
					if(Vector3.Distance(targetPosition, vessel.targetObject.GetTransform().position) > 1200)
					{
						targetPosition = vessel.targetObject.GetTransform().position;	
					}
					flightCamera.transform.rotation = Quaternion.LookRotation(targetPosition - flightCamera.transform.position, FlightGlobals.getUpAxis());
					flightCamera.transform.position = vessel.transform.position;
					
					//flightCamera.SetFoV(25 * 60/(vessel.targetObject.GetTransform().position - vessel.transform.position).magnitude);
					flightCamera.SetFoV(0.2f);
				}
				#endregion
				
				
				#region targetFreeCamera
				if(Input.GetKeyDown(KeyCode.Keypad2))
				{
					TargetFreeCamera();
				}
				#endregion
				
				
				#region projectileCamera
				if(Input.GetKeyDown(KeyCode.Keypad3) && lastProjectileFired != null)
				{
					if(!projectileCameraEnabled)
					{
						RevertCamera();
						SaveCamera();
						Camera.main.nearClipPlane = 2;
						flightCamera.transform.parent = null;
						flightCamera.setTarget(null);
						flightCamera.transform.rotation = Quaternion.LookRotation(lastProjectileFired.transform.forward, FlightGlobals.getUpAxis());
						projectileCameraEnabled = true;
						lastProjectileFired.OnJustAboutToBeDestroyed += new Callback(PostProjectileCamera);
						
						//targetPosition = vessel.targetObject.GetTransform().position;
					}
					else
					{
						lastProjectileFired.OnJustAboutToBeDestroyed -= new Callback(PostProjectileCamera);
						RevertCamera();
					}
				}
				if(projectileCameraEnabled)
				{
					if(lastProjectileFired != null)
					{
						float rotateRate = Time.deltaTime * Quaternion.Angle(flightCamera.transform.rotation, Quaternion.LookRotation(lastProjectileFired.transform.forward, FlightGlobals.getUpAxis()));
						flightCamera.transform.rotation = Quaternion.RotateTowards(flightCamera.transform.rotation, Quaternion.LookRotation(lastProjectileFired.transform.forward, FlightGlobals.getUpAxis()), rotateRate);
						//flightCamera.transform.rotation = Quaternion.LookRotation(lastProjectileFired.transform.forward, FlightGlobals.getUpAxis());
						flightCamera.transform.position = lastProjectileFired.transform.position;
						
						lastProjectileFired.OnJustAboutToBeDestroyed -= new Callback(PostProjectileCamera);
						
						lastProjectileFired.OnJustAboutToBeDestroyed += new Callback(PostProjectileCamera);
						projectileCameraFinished = false;
						flightCamera.SetFoV(100f);
					}
					else
					{
						//TargetCamera();
					}
				}
				
				if(projectileCameraFinished)
				{
					if(Time.time-pjcTimeCheck > 2)
					{
						RevertCamera();
						projectileCameraFinished = false;
					}
				}
				else
				{
					pjcTimeCheck = Time.time;	
				}
				
				#endregion
				
				#region flyby camera
				if(Input.GetKeyDown(KeyCode.Keypad4))
				{
					FlyByCamera();	
				}
				if(flybyCameraEnabled)
				{
					flightCamera.transform.rotation = Quaternion.LookRotation(vessel.transform.position - flightCamera.transform.position, cameraUp);
					flightCamera.transform.position -= Time.deltaTime * Mathf.Clamp((float)vessel.srf_velocity.magnitude, 0, 3500)*vessel.srf_velocity.normalized;
					flightCamera.transform.position += Time.deltaTime * Mathf.Clamp(vessel.rigidbody.velocity.magnitude, 0, 3500) * vessel.rigidbody.velocity.normalized;
					float lowerLimit = Mathf.Clamp (0.0002f * (float)(vessel.srfSpeed * vessel.srfSpeed) - 4, 5, 60);
					
					float cameraDistance = Vector3.Distance(vessel.transform.position, flightCamera.transform.position);
					float targetFoV = Mathf.Clamp((7000/(cameraDistance+100)) - 4, lowerLimit, 60);
					flightCamera.SetFoV(targetFoV);
					
				}
				#endregion
			}
		}
		
		void TargetFreeCamera()
		{
			if(!targetFreeCameraEnabled && vessel.targetObject != null)
			{
				RevertCamera();
				SaveCamera();
				
				flightCamera.setTarget(vessel.targetObject.GetTransform());
				targetFreeCameraEnabled = true;
				
				targetPosition = vessel.targetObject.GetTransform().position;
			}
			else
			{
				RevertCamera();
			}
		}	
		
		void TargetCamera()
		{
			if(!targetCameraEnabled && vessel.targetObject!=null)
			{
				RevertCamera();
				SaveCamera();
				Camera.main.nearClipPlane = 25;
				
				flightCamera.transform.parent = vessel.transform;
				flightCamera.setTarget(null);
				flightCamera.transform.rotation = Quaternion.LookRotation(targetPosition - flightCamera.transform.position, FlightGlobals.getUpAxis());
				targetCameraEnabled = true;
				
				targetPosition = vessel.targetObject.GetTransform().position;
			}
			else
			{
				RevertCamera();
			}
		}
		
		void FlyByCamera()
		{
			if(!flybyCameraEnabled && FlightGlobals.ActiveVessel!=null)
			{
				RevertCamera();
				SaveCamera();
				cameraUp = FlightGlobals.getUpAxis();
				if(FlightCamera.fetch.mode == FlightCamera.Modes.ORBITAL || (FlightCamera.fetch.mode == FlightCamera.Modes.AUTO && FlightCamera.GetAutoModeForVessel(vessel) == FlightCamera.Modes.ORBITAL))
				{
					cameraUp = -vessel.ReferenceTransform.forward;
				}
				
				//GameObject emptyGO = new GameObject("empty");
				//emptyGO = (GameObject) Instantiate(emptyGO, spawnPos, Quaternion.identity);
				
				float sideDistance = Mathf.Clamp(20+(float)vessel.srfSpeed/10, 15, 150);
				float distanceAhead = Mathf.Clamp(4*(float)vessel.srf_velocity.magnitude, 30, 2750);

				flightCamera.transform.parent = null;
				flightCamera.setTarget(null);
				flightCamera.transform.rotation = Quaternion.LookRotation(vessel.transform.position - flightCamera.transform.position, cameraUp);
				flightCamera.transform.position = vessel.transform.position + distanceAhead*vessel.srf_velocity.normalized + sideDistance*vessel.ReferenceTransform.right + 15*FlightGlobals.getUpAxis();

				
				flybyCameraEnabled = true;
			}
			else
			{
				RevertCamera();	
			}
			
		}
		
		void RevertCamera()
		{
			vessel = FlightGlobals.ActiveVessel;
			flightCamera.transform.position = origPosition;
			flightCamera.transform.localRotation = origRotation;
			flightCamera.transform.parent = origParent;
			FlightCamera.fetch.setTarget(vessel.transform);
			flightCamera.ResetFoV();
			Camera.main.nearClipPlane = origClip;
			
			targetCameraEnabled = false;
			targetCloseCameraEnabled = false;
			projectileCameraEnabled = false;
			targetFreeCameraEnabled = false;
			flybyCameraEnabled = false;
		}
		
		void SaveCamera()
		{
			vessel = FlightGlobals.ActiveVessel;
			origPosition = flightCamera.transform.position;
			origRotation = flightCamera.transform.localRotation;
			origParent = flightCamera.transform.parent;
			origClip = Camera.main.nearClipPlane;
		}
		
		bool CheckForManager()
		{
			foreach(MissileFire ml in FlightGlobals.ActiveVessel.FindPartModulesImplementing<MissileFire>())
			{
				return true;	
			}
			//RevertCamera();
			return false;
		}
		
		public static void PostProjectileCamera()
		{
			projectileCameraFinished = true;
			
			flightCamera.SetFoV(80);
			flightCamera.setTarget(null);
			flightCamera.transform.SetParent(null);
			
			flightCamera.transform.position -= 70*flightCamera.transform.forward;
			
		}
		
		
	}
}

