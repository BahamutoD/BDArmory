
namespace BDArmory.AmmoSwitch
{
    public class BDAcAmmo
    {
        //public PartResource resource;
        public string name;
        public int ID;
        public float ratio;
        public double currentSupply = 0f;
        public double amount = 0f;
        public double maxAmount = 0f;

        public BDAcAmmo(string _name, float _ratio)
        {
            name = _name;
            ID = _name.GetHashCode();
            ratio = _ratio;
        }

        public BDAcAmmo(string _name)
        {
            name = _name;
            ID = _name.GetHashCode();
            ratio = 1f;
        }
    }
}
