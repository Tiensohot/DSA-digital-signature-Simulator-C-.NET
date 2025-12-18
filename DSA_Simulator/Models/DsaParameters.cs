using System.Numerics;

namespace DSA_DigitalSignature.Models
{
    public class DsaParameters
    {
        public BigInteger P { get; set; }
        public BigInteger Q { get; set; }
        public BigInteger G { get; set; }

        public BigInteger X { get; set; } // Private key
        public BigInteger Y { get; set; } // Public key
    }
}
