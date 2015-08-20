using System;
using System.Collections;
using UnityEngine;
namespace BahaTurret
{
	public class BDKeyBinder : MonoBehaviour
	{
		public static BDKeyBinder current = null;
		public int id = 0;
		public bool valid = false;
		string inputString = string.Empty;
		bool mouseUp = false;

		public void StartRecording()
		{
			StartCoroutine(RecordKeyRoutine());	
		}

		IEnumerator RecordKeyRoutine()
		{
			while(!valid)
			{
				if(mouseUp)
				{
					string iString = BDInputUtils.GetInputString();
					if(iString.Length > 0)
					{
						inputString = iString;
						valid = true;
					}
				}

				if(Input.GetKeyUp(KeyCode.Mouse0))
				{
					mouseUp = true;
				}
				yield return null;
			}
		}

		public bool AcquireInputString(out string _inputString)
		{
			if(valid)
			{
				_inputString = inputString;
				current = null;
				Destroy(gameObject);
				return true;
			}
			else
			{
				_inputString = string.Empty;
				return false;
			}
		}

		public static void BindKey(int id)
		{
			if(current != null)
			{
				Debug.Log("Tried to bind key but key binder is in use.");
				return;
			}

			current = new GameObject().AddComponent<BDKeyBinder>();
			current.id = id;
			current.StartRecording();
		}

		public static bool IsRecordingID(int id)
		{
			return (current && current.id == id);
		}
	}
}

