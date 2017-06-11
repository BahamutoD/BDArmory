using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace BDArmory.Multiplayer.Utils
{
    public static class BinaryUtils
    {
        public static byte[] ObjectToByteArray(object obj)
        {
            if (obj == null)
                return null;
            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }
    }
}
