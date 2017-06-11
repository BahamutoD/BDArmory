
using System;
using BDArmory.Multiplayer.Enum;

namespace BDArmory.Multiplayer.Message
{
    [Serializable]
    public class BdaMessage
    {
        public MessageType MessageType { get; set; }
        public MessageContent MessageContent { get; set; }
    }

    [Serializable]
    public class MessageContent
    {    
        public string VesselId { get; set; }
        public string PartId { get; set; }
    }

    [Serializable]
    internal class DamageMessageContent : MessageContent
    {
        public double Damage { get; set; }
    }
}
       