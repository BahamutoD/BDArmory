using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace BahaTurret
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class BDAShaderLoader : MonoBehaviour
    {
        private static bool loaded;

        private static readonly string bundlePath = KSPUtil.ApplicationRootPath + "GameData" +
                                                    Path.DirectorySeparatorChar +
                                                    "BDArmory" + Path.DirectorySeparatorChar + "AssetBundles";


        public static Shader GrayscaleEffectShader;
        public static Shader UnlitBlackShader;
        public static Shader BulletShader;

        public string BundlePath
        {
            get
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.OSXPlayer:
                        return bundlePath + Path.DirectorySeparatorChar +
                               "bdarmoryshaders_macosx.bundle";
                    case RuntimePlatform.WindowsPlayer:
                        return bundlePath + Path.DirectorySeparatorChar +
                               "bdarmoryshaders_windows.bundle";
                    case RuntimePlatform.LinuxPlayer:
                        return bundlePath + Path.DirectorySeparatorChar +
                               "bdarmoryshaders_macosx.bundle";
                    default:
                        return bundlePath + Path.DirectorySeparatorChar +
                               "bdarmoryshaders_windows.bundle";
                }
            }
        }

        private void Start()
        {
            if (!loaded)
            {
                Debug.Log("[BDArmory] start bundle load process");
                StartCoroutine(LoadBundleAssets());
                loaded = true;
            }
        }

        private IEnumerator LoadBundleAssets()
        {
            Debug.Log("[BDArmory] Loading bundle data");

            var shaderBundle = AssetBundle.LoadFromFile(BundlePath);

            if (shaderBundle != null)
            {
                var shaders = shaderBundle.LoadAllAssets<Shader>();

                foreach (var shader in shaders)
                {
                    Debug.Log($"[BDArmory] Shader \"{shader.name}\" loaded. Shader supported? {shader.isSupported}");

                    switch (shader.name)
                    {
                        case "BDArmory/Particles/Bullet":
                            BulletShader = shader;
                            break;
                        case "Custom/Unlit Black":
                            UnlitBlackShader = shader;
                            break;
                        case "Hidden/Grayscale Effect":
                            GrayscaleEffectShader = shader;
                            break;
                        default:
                            Debug.Log($"[BDArmory] Not expected shader : {shader.name}");
                            break;
                    }
                }
                yield return null;
                Debug.Log("[BDArmory] unloading bundle");
                shaderBundle.Unload(false); // unload the raw asset bundle
            }
            else
            {
                Debug.Log("[BDArmory] Error: Found no asset bundle to load");
            }
        }
    }
}