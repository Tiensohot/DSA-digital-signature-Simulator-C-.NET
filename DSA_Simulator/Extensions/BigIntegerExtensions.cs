using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;



namespace DSA_DigitalSignature.Extensions
{
    public static class BigIntegerExtensions
    {
        public static bool IsProbablePrime(this BigInteger value, int rounds = 10)
        {
            if (value < 2) return false;
            if (value == 2 || value == 3) return true;
            if (value % 2 == 0) return false;

            BigInteger d = value - 1;
            int s = 0;
            while (d % 2 == 0)
            {
                d /= 2;
                s++;
            }

            var rng = new Random();
            for (int i = 0; i < rounds; i++)
            {
                BigInteger a = RandomBigInteger(2, value - 2, rng);
                BigInteger x = BigInteger.ModPow(a, d, value);
                if (x == 1 || x == value - 1) continue;

                bool composite = true;
                for (int r = 1; r < s; r++)
                {
                    x = BigInteger.ModPow(x, 2, value);
                    if (x == value - 1)
                    {
                        composite = false;
                        break;
                    }
                }
                if (composite) return false;
            }
            return true;
        }

        private static BigInteger RandomBigInteger(BigInteger min, BigInteger max, Random rng)
        {
            byte[] bytes = max.ToByteArray();
            BigInteger r;
            do
            {
                rng.NextBytes(bytes);
                bytes[^1] &= 0x7F;
                r = new BigInteger(bytes);
            } while (r < min || r >= max);
            return r;
        }
    }
}
