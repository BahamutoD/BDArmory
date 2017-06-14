using System;
using BDArmory.Core;
using BDArmory.Core.Services;
using BDArmory.Multiplayer.Interface;
using BDArmory.Multiplayer.Utils;
using LunaClient.Systems;
using LunaClient.Systems.ModApi;
using UnityEngine;

namespace BDArmory.Multiplayer
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class LunaMultiplayerSytem : MonoBehaviour, IMultiplayerSystem
    {
        private const string ModName = "BDArmory";
        public const bool Relay = true;

        private void Awake()
        {
          RegisterSystem();
        }

        public void RegisterSystem()
        {

            try
            {
                SystemsContainer.Get<ModApiSystem>().RegisterFixedUpdateModHandler(ModName, HandlerFunction);
                Dependencies.Register<IMultiplayerSystem>(this);
                SuscribeToCoreEvents();

                Debug.Log("[BDArmory]: LMP Multiplayer found");     
            }
            catch (Exception ex)
            {
                Debug.Log("[BDArmory]: LMP Multiplayer is not installed");
                Debug.LogError(ex);
            }
        }

        public void HandlerFunction(byte[] messageData)
        {
            Debug.Log("[BDArmory]: Message received"+ messageData.Length);

        }

        private void SuscribeToCoreEvents()
        {
           Dependencies.Get<DamageService>().OnActionExecuted += OnActionExecuted;
        }

        private void OnActionExecuted(object sender, EventArgs eventArgs)
        {
            SendMessage(eventArgs);
        }

       
        public void SendMessage(EventArgs message)
        {
            Debug.Log("[BDArmory]: Sending message");
            SystemsContainer.Get<ModApiSystem>().SendModMessage(ModName, BinaryUtils.ObjectToByteArray(message), true);
        }

        void OnDestroy()
        {
            
        }
    }
}