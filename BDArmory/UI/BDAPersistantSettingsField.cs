using System;
using System.Reflection;
using UnityEngine;

namespace BahaTurret
{
    [AttributeUsage(AttributeTargets.Field)]
    public class BDAPersistantSettingsField : Attribute
    {
        public BDAPersistantSettingsField()
        {
        }

        public static void Save()
        {
            ConfigNode fileNode = ConfigNode.Load(BDArmorySettings.settingsConfigURL);

            if (!fileNode.HasNode("BDASettings"))
            {
                fileNode.AddNode("BDASettings");
            }

            ConfigNode settings = fileNode.GetNode("BDASettings");

            foreach (FieldInfo field in typeof(BDArmorySettings).GetFields())
            {
                if (!field.IsDefined(typeof(BDAPersistantSettingsField), false)) continue;

                settings.SetValue(field.Name, field.GetValue(null).ToString(), true);
            }

            fileNode.Save(BDArmorySettings.settingsConfigURL);
        }

        public static void Load()
        {
            ConfigNode fileNode = ConfigNode.Load(BDArmorySettings.settingsConfigURL);
            if (!fileNode.HasNode("BDASettings")) return;

            ConfigNode settings = fileNode.GetNode("BDASettings");

            foreach (FieldInfo field in typeof(BDArmorySettings).GetFields())
            {
                if (!field.IsDefined(typeof(BDAPersistantSettingsField), false)) continue;

                if (!settings.HasValue(field.Name)) continue;
                object parsedValue = BDArmorySettings.ParseValue(field.FieldType, settings.GetValue(field.Name));
                if (parsedValue != null)
                {
                    field.SetValue(null, parsedValue);
                }
            }
        }
    }
}