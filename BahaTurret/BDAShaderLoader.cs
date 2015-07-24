using System;
using UnityEngine;
using System.Reflection;
using System.IO;
namespace BahaTurret
{
	public static class BDAShaderLoader
	{
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
	}
}

