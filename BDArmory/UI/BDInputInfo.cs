namespace BDArmory.UI
{
    public struct BDInputInfo
    {
        public string description;
        public string inputString;

        public BDInputInfo(string description)
        {
            this.description = description;
            inputString = string.Empty;
        }

        public BDInputInfo(string inputString, string description)
        {
            this.inputString = inputString;
            this.description = description;
        }
    }
}