using Newtonsoft.Json;
using System.Collections.Generic;

namespace BDArmory.Misc
{
    [JsonObject(MemberSerialization.OptIn)]
    public class BDTeam
    {
        [JsonProperty]
        public readonly string Name;

        [JsonProperty]
        public bool Neutral;

        [JsonProperty]
        public readonly List<string> Friends;

        public BDTeam(string name, List<string> friends = null, bool neutral = false)
        {
            Name = name;
            Friends = friends ?? new List<string>();
            Neutral = neutral;
        }

        public bool IsEnemy(BDTeam other)
        {
            if (Neutral || other is null || other.Neutral || other.Name == Name || Friends.Contains(other.Name))
                return false;
            return true;
        }

        public override string ToString() => JsonConvert.SerializeObject(this);

        public static BDTeam FromString(string teamString)
        {
            // Backward compatibility
            if (string.IsNullOrEmpty(teamString) || teamString == "False")
                return new BDTeam("A");
            else if (teamString == "True")
                return new BDTeam("B");
            try
            {
                return JsonConvert.DeserializeObject<BDTeam>(teamString);
            }
            catch
            {
                return new BDTeam("A");
            }
        }

        public override int GetHashCode() => Name.GetHashCode();

        public bool Equals(BDTeam other) => Name == other?.Name;

        public override bool Equals(object obj) => Equals(obj as BDTeam);

        public static bool operator ==(BDTeam left, BDTeam right) => object.Equals(left, right);

        public static bool operator !=(BDTeam left, BDTeam right) => !object.Equals(left, right);
    }
}
