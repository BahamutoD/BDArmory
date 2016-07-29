using System;
using System.Reflection;

namespace BahaTurret
{
	[AttributeUsage(AttributeTargets.Field)]
	public class BDAPersistantSettingsField : Attribute
	{
		public BDAPersistantSettingsField ()
		{
		}

		public static void Save()
		{
			ConfigNode fileNode = ConfigNode.Load(BDArmorySettings.settingsConfigURL);

			if(!fileNode.HasNode("BDASettings"))
			{
				fileNode.AddNode("BDASettings");
			}

			ConfigNode settings = fileNode.GetNode("BDASettings");

			foreach(var field in typeof(BDArmorySettings).GetFields())
			{
				if(!field.IsDefined(typeof(BDAPersistantSettingsField), false)) continue;

				settings.SetValue(field.Name, field.GetValue(null).ToString(), true);
			}

			fileNode.Save(BDArmorySettings.settingsConfigURL);
		}

		public static void Load()
		{
			ConfigNode fileNode = ConfigNode.Load(BDArmorySettings.settingsConfigURL);
			if(!fileNode.HasNode("BDASettings")) return;

			ConfigNode settings = fileNode.GetNode("BDASettings");

			foreach(var field in typeof(BDArmorySettings).GetFields())
			{
				if(!field.IsDefined(typeof(BDAPersistantSettingsField), false)) continue;

				if(settings.HasValue(field.Name))
				{
					object parsedValue = ParseValue(field.FieldType, settings.GetValue(field.Name));
					if(parsedValue != null)
					{
						field.SetValue(null, parsedValue);
					}
				}
			}
		}

		public static object ParseValue(Type type, string value)
		{
			if(type == typeof(string))
			{
				return value;
			}

			if(type == typeof(bool))
			{
				return bool.Parse(value);
			}
			else if(type.IsEnum)
			{
				return Enum.Parse(type, value);
			}
			else if(type == typeof(float))
			{
				return float.Parse(value);
			}
			else if(type == typeof(Single))
			{
				return Single.Parse(value);
			}
			UnityEngine.Debug.LogError("BDAPersistantSettingsField to parse settings field of type "+type.ToString()+" and value "+value);

			return null;
		}

	}
}

