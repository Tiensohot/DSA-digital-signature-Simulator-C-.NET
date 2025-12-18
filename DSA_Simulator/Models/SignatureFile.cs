using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSA_DigitalSignature.Models
{
    public class SignatureFile
    {
        public string Algorithm { get; set; } = "";
        public string Hash { get; set; } = "";
        public string R { get; set; } = "";
        public string S { get; set; } = "";
        public string PublicKeyY { get; set; } = "";
        public string P { get; set; } = "";
        public string Q { get; set; } = "";
        public string G { get; set; } = "";
    }
}
