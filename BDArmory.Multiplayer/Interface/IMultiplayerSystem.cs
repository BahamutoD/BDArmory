using System;

namespace BDArmory.Multiplayer.Interface
{
    public interface IMultiplayerSystem
    {
        void RegisterSystem();

        void SendMessage(EventArgs message);
    }
}