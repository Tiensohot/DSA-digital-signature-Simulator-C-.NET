using System.Numerics;
using DSA_DigitalSignature.Models;

namespace DSA_DigitalSignature.Services
{
    public class DsaMathService
    {
        // nghịch đảo modulo
        public static BigInteger ModInverse(BigInteger a, BigInteger mod)
        {
            return BigInteger.ModPow(a, mod - 2, mod);
        }

        // Ký văn bản
        public static DsaSignature Sign(
            BigInteger hash,
            DsaParameters param,
            BigInteger k)
        {
            BigInteger r = BigInteger.ModPow(param.G, k, param.P) % param.Q;
            BigInteger s = (ModInverse(k, param.Q) * (hash + param.X * r)) % param.Q;

            return new DsaSignature { R = r, S = s };
        }

        // Xác thực chữ ký
        public static bool Verify(
            BigInteger hash,
            DsaSignature sig,
            DsaParameters param)
        {
            BigInteger w = ModInverse(sig.S, param.Q);
            BigInteger u1 = (hash * w) % param.Q;
            BigInteger u2 = (sig.R * w) % param.Q;

            BigInteger v =
                (BigInteger.ModPow(param.G, u1, param.P)
                * BigInteger.ModPow(param.Y, u2, param.P)
                % param.P) % param.Q;

            return v == sig.R;
        }
    }
}
