using System;
using System.Collections;
using System.Reflection;
using System.IO;
using UnityEngine;
using KSPAssets.Loaders;
using KSPAssets;
namespace BahaTurret
{
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	public class BDAShaderLoader : MonoBehaviour
	{
		private static bool loaded = false;
		public static Shader GrayscaleEffectShader;
		public static Shader UnlitBlackShader;
		public static Shader BulletShader;

		void Start()
		{
			if(!loaded)
			{
				StartCoroutine(LoadRoutine());
				loaded = true;
			}
		}

		IEnumerator LoadRoutine()
		{
			while(!AssetLoader.Ready)
			{
				yield return null;
			}


			AssetDefinition bulletDef = AssetLoader.GetAssetDefinitionWithName("BDArmory/AssetBundles/bdabulletshader", "BDArmory/Bullet");
			AssetDefinition unlitBlackDef = AssetLoader.GetAssetDefinitionWithName("BDArmory/AssetBundles/bdaunlitblack", "BDArmory/Unlit Black");
			AssetDefinition grayscaleDef = AssetLoader.GetAssetDefinitionWithName("BDArmory/AssetBundles/bdagrayscaleshader", "BDArmory/Grayscale Effect");
			AssetDefinition[] assetDefs = new AssetDefinition[]{ bulletDef, unlitBlackDef, grayscaleDef };
			AssetLoader.LoadAssets(AssetLoaded, assetDefs);
		}


		void AssetLoaded(AssetLoader.Loader loader)
		{
			Debug.Log("BDArmory loaded shaders: ");
			for(int i = 0; i < loader.objects.Length; i++)
			{
				Shader s = (Shader)loader.objects[i];
				if(s == null) continue;

				Debug.Log("- " + s.name);

				if(s.name == "BDArmory/Bullet")
				{
					BulletShader = s;
				}
				else if(s.name == "BDArmory/Unlit Black")
				{
					UnlitBlackShader = s;
				}
				else if(s.name == "BDArmory/Grayscale Effect")
				{
					GrayscaleEffectShader = s;
				}
			}
		}

		/*
		public static Shader LoadManifestShader(string manifestResource)
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			Stream stream = assembly.GetManifestResourceStream(manifestResource);
			if(stream!=null)
			{
				StreamReader reader = new StreamReader(stream);
			
				Material mat = new Material(reader.ReadToEnd());
				return mat.shader;
			}
			else
			{
				Debug.Log ("ShaderLoader: Failed to acquire stream from manifest resource.");
			}
			return null;
		}
		*/
	}


}

