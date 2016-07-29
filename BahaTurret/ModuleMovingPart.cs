using System;
using System.Collections;
using UnityEngine;

namespace BahaTurret
{
	public class ModuleMovingPart : PartModule
	{
		Transform parentTransform;
		[KSPField]
		public string parentTransformName = string.Empty;



		bool setupComplete = false;

		Part[] children;
		Vector3[] localAnchors;

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			if(HighLogic.LoadedSceneIsFlight)
			{
				if(parentTransformName == string.Empty)
				{
					enabled = false;
					return;
				}

				parentTransform = part.FindModelTransform(parentTransformName);

				StartCoroutine(SetupRoutine());
			}
		}

		void FixedUpdate()
		{
			if(setupComplete)
			{
				UpdateJoints();

			}
		}

		IEnumerator SetupRoutine()
		{
			while(vessel.packed)
			{
				yield return null;
			}
			SetupJoints();
		}

		void SetupJoints()
		{
			children = part.children.ToArray();
			localAnchors = new Vector3[children.Length];

			for(int i = 0; i < children.Length; i++)
			{
				children[i].attachJoint.Joint.autoConfigureConnectedAnchor = false;
				Vector3 connectedAnchor = children[i].attachJoint.Joint.connectedAnchor;
				Vector3 worldAnchor = children[i].attachJoint.Joint.connectedBody.transform.TransformPoint(connectedAnchor);
				Vector3 localAnchor = parentTransform.InverseTransformPoint(worldAnchor);
				localAnchors[i] = localAnchor;
			}

			setupComplete = true;
		}

		void UpdateJoints()
		{
			for(int i = 0; i < children.Length; i++)
			{
				if(!children[i]) continue;

				Vector3 newWorldAnchor = parentTransform.TransformPoint(localAnchors[i]);
				Vector3 newConnectedAnchor = children[i].attachJoint.Joint.connectedBody.transform.InverseTransformPoint(newWorldAnchor);
				children[i].attachJoint.Joint.connectedAnchor = newConnectedAnchor;
			}
		}

		void OnGUI()
		{
			if(setupComplete)
			{
				for(int i = 0; i < localAnchors.Length; i++)
				{
					BDGUIUtils.DrawTextureOnWorldPos(parentTransform.TransformPoint(localAnchors[i]), BDArmorySettings.Instance.greenDotTexture, new Vector2(6, 6), 0);
				}
			}
		}
	}
}

