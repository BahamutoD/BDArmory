using BDArmory.Core.Services;
using UnityEngine;

namespace BDArmory.Core
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class Bootstrapper :MonoBehaviour
    {
        private void Awake()
        {
            Dependencies.Register<DamageService, TemperatureDamageService>();
        }
    }
}
