using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace DSA_DigitalSignature.Services
{
    public class HashService
    {
        public static BigInteger ComputeSHA256(byte[] data)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(data);
            return new BigInteger(hash, true, true);
        }

        public static BigInteger ComputeSHA256(string text)
        {
            return ComputeSHA256(Encoding.UTF8.GetBytes(text));
        }
    }
}
