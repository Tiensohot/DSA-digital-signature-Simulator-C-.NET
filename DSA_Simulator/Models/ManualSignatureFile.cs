using System.Numerics;

namespace DSA_DigitalSignature.Models
{
    public class ManualSignatureFile
    {
        public string P { get; set; } = "";
        public string Q { get; set; } = "";
        public string G { get; set; } = "";
        public string Y { get; set; } = "";
        public string R { get; set; } = "";
        public string S { get; set; } = "";
    }
}
