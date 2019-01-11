using BDArmory.UI;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace BDArmory.Misc
{
    [JsonObject(MemberSerialization.OptIn)]
    public class BDTeam
    {
        [JsonProperty]
        public string Name;

        [JsonProperty]
        public bool Neutral;

        [JsonProperty]
        public List<string> Allies;

        public BDTeam(string name, List<string> allies = null, bool neutral = false)
        {
            Name = name;
            Neutral = neutral;
            Allies = allies ?? new List<string>();
        }

        public static BDTeam Get(string name)
        {
            if (!BDArmorySetup.Instance.Teams.ContainsKey(name))
                BDArmorySetup.Instance.Teams.Add(name, new BDTeam(name));
            return BDArmorySetup.Instance.Teams[name];
        }

        public bool IsEnemy(BDTeam other)
        {
            if (other == null)
                return true;
            if (Neutral || other.Neutral || other.Name == Name || Allies.Contains(other.Name))
                return false;
            return true;
        }

        public override string ToString() => Name;

        public static BDTeam Deserialize(string teamString)
        {
            // Backward compatibility
            if (string.IsNullOrEmpty(teamString) || teamString == "False")
                return BDTeam.Get("A");
            else if (teamString == "True")
                return BDTeam.Get("B");
            try
            {
                BDTeam team = JsonConvert.DeserializeObject<BDTeam>(teamString);
                if (!BDArmorySetup.Instance.Teams.ContainsKey(team.Name))
                    BDArmorySetup.Instance.Teams.Add(team.Name, team);
                return BDArmorySetup.Instance.Teams[team.Name];
            }
            catch
            {
                return BDTeam.Get("A");
            }
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        public override int GetHashCode() => Name.GetHashCode();

        public bool Equals(BDTeam other) => Name == other?.Name;

        public override bool Equals(object obj) => Equals(obj as BDTeam);

        public static bool operator ==(BDTeam left, BDTeam right) => Equals(left, right);

        public static bool operator !=(BDTeam left, BDTeam right) => !Equals(left, right);
    }
}
