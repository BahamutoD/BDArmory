using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BahaTurret
{
	public class BDAdjustableRail : PartModule
	{
		
		[KSPField(isPersistant = true)]
		public float railHeight = 0;
		
		
		[KSPField(isPersistant = true)]
		public float railLength = 1;
		
		
		Transform railLengthTransform;
		Transform railHeightTransform;

		[KSPField]
		public string stackNodePosition;

		Dictionary<string,Vector3> originalStackNodePosition;
		
		public override void OnStart (PartModule.StartState state)
		{
			railLengthTransform = part.FindModelTransform("Rail");
			railHeightTransform = part.FindModelTransform("RailSleeve");
			
			railLengthTransform.localScale = new Vector3(1, railLength, 1);
			railHeightTransform.localPosition = new Vector3(0,railHeight,0);

			if(HighLogic.LoadedSceneIsEditor)
			{
				ParseStackNodePosition();
				StartCoroutine(DelayedUpdateStackNode());
			}
		}

		void ParseStackNodePosition()
		{
			originalStackNodePosition = new Dictionary<string, Vector3>();
			string[] nodes = stackNodePosition.Split(new char[]{ ';' });
			for(int i = 0; i < nodes.Length; i++)
			{
				string[] split = nodes[i].Split(new char[]{ ',' });
				string id = split[0];
				Vector3 position = new Vector3(float.Parse(split[1]), float.Parse(split[2]), float.Parse(split[3]));
				originalStackNodePosition.Add(id, position);
			}
		}


		IEnumerator DelayedUpdateStackNode()
		{
			yield return null;
			UpdateStackNode(false);
		}

		
		[KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Height ++", active = true)]
		public void IncreaseHeight()
		{
			/*
			UpdateStackNode(false);
			foreach(Part sym in part.symmetryCounterparts)
			{
				sym.FindModuleImplementing<BDAdjustableRail>().UpdateStackNode(false);
			}
			*/
			railHeight = Mathf.Clamp(railHeight-0.02f, -.16f, 0);
			railHeightTransform.localPosition = new Vector3(0,railHeight,0);

			UpdateStackNode(true);

			foreach(Part sym in part.symmetryCounterparts)
			{
				sym.FindModuleImplementing<BDAdjustableRail>().UpdateHeight(railHeight);
			}


		}


		
		[KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Height --", active = true)]
		public void DecreaseHeight()
		{
			/*
			UpdateStackNode(false);
			foreach(Part sym in part.symmetryCounterparts)
			{
				sym.FindModuleImplementing<BDAdjustableRail>().UpdateStackNode(false);
			}
			*/
			railHeight = Mathf.Clamp(railHeight+0.02f, -.16f, 0);
			railHeightTransform.localPosition = new Vector3(0,railHeight,0);

			UpdateStackNode(true);

			foreach(Part sym in part.symmetryCounterparts)
			{
				sym.FindModuleImplementing<BDAdjustableRail>().UpdateHeight(railHeight);
			}
		}
		
		[KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Length ++", active = true)]
		public void IncreaseLength()
		{
			railLength = Mathf.Clamp(railLength+0.2f, 0.4f, 2f);
			railLengthTransform.localScale = new Vector3(1, railLength, 1);
			foreach(Part sym in part.symmetryCounterparts)
			{
				sym.FindModuleImplementing<BDAdjustableRail>().UpdateLength(railLength);
			}
		}
		
		[KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Length --", active = true)]
		public void DecreaseLength()
		{
			railLength = Mathf.Clamp(railLength-0.2f, 0.4f, 2f);
			railLengthTransform.localScale = new Vector3(1, railLength, 1);
			foreach(Part sym in part.symmetryCounterparts)
			{
				sym.FindModuleImplementing<BDAdjustableRail>().UpdateLength(railLength);
			}
		}

		public void UpdateHeight(float height)
		{
			railHeight = height;
			railHeightTransform.localPosition = new Vector3(0,railHeight,0);	

			UpdateStackNode(true);
		}
		
		public void UpdateLength(float length)
		{
			railLength = length;
			railLengthTransform.localScale = new Vector3(1, railLength, 1);
		}

		public void UpdateStackNode(bool updateChild)
		{
			foreach(var stackNode in part.attachNodes)
			{
				if(stackNode.nodeType == AttachNode.NodeType.Stack && originalStackNodePosition.ContainsKey(stackNode.id))
				{
					Vector3 prevPos = stackNode.position;

					stackNode.position.y = originalStackNodePosition[stackNode.id].y + railHeight;

					if(updateChild)
					{
						Vector3 delta = stackNode.position - prevPos;
						Vector3 worldDelta = part.transform.TransformVector(delta);
						foreach(var p in part.children)
						{
							p.transform.position += worldDelta;
						}
					}
				}
			}
		}
	}
}

